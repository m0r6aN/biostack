using System.Text;
using System.Threading.RateLimiting;
using BioStack.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Repositories;
using BioStack.Infrastructure.Knowledge;
using BioStack.Application.Services;
using BioStack.Api.Endpoints;
using BioStack.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim())
    .ToArray()
    ?? Array.Empty<string>();

if (allowedOrigins.Length == 0)
{
    allowedOrigins =
    [
        "http://localhost:3000",
        "http://localhost:3001",
        "http://localhost:3043"
    ];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth-start", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
            }));

    options.AddPolicy("auth-verify", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
            }));
});

// ── First-party cookie sessions + legacy bearer support ─────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be set in configuration or environment.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "BioStackAuth";
        options.DefaultChallengeScheme = "BioStackAuth";
    })
    .AddPolicyScheme("BioStackAuth", "BioStack auth", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? JwtBearerDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "biostack_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = async context =>
        {
            var sessionToken = context.Principal?.FindFirst("session_token")?.Value;
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                context.RejectPrincipal();
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<BioStackDbContext>();
            var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(sessionToken)));
            var session = await db.Sessions.FirstOrDefaultAsync(s =>
                s.TokenHash == tokenHash &&
                s.RevokedAtUtc == null &&
                s.ExpiresAtUtc > DateTime.UtcNow);

            if (session is null)
            {
                context.RejectPrincipal();
            }
        };
    })
    .AddJwtBearer(options =>
    {
        // .NET 8+ uses JsonWebTokenHandler which ignores DefaultInboundClaimTypeMap.
        // Setting MapInboundClaims = false keeps "role" as "role" (not remapped to
        // the long ClaimTypes.Role URI), so RequireClaim("role", "1") works correctly.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience          = true,
            ValidateLifetime          = true,
            ValidateIssuerSigningKey  = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"]   ?? "biostack",
            ValidAudience            = builder.Configuration["Jwt:Audience"] ?? "biostack-ui",
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("role", "1"));
});

// ── Database ────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=./data/biostack.db";

var configuredDatabaseProvider = builder.Configuration["Database:Provider"];
var usePostgres = DatabaseProviderResolver.IsPostgres(configuredDatabaseProvider, connectionString);

builder.Services.AddDbContext<BioStackDbContext>(options =>
{
    if (usePostgres)
    {
        options.UseNpgsql(connectionString);
        return;
    }

    options.UseSqlite(connectionString);
});

// ── Repositories ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPersonProfileRepository, PersonProfileRepository>();
builder.Services.AddScoped<ICompoundRecordRepository, CompoundRecordRepository>();
builder.Services.AddScoped<ICheckInRepository, CheckInRepository>();
builder.Services.AddScoped<IProtocolRepository, ProtocolRepository>();
builder.Services.AddScoped<IProtocolRunRepository, ProtocolRunRepository>();
builder.Services.AddScoped<IProtocolComputationRecordRepository, ProtocolComputationRecordRepository>();
builder.Services.AddScoped<IProtocolReviewCompletedEventRepository, ProtocolReviewCompletedEventRepository>();
builder.Services.AddScoped<IProtocolPhaseRepository, ProtocolPhaseRepository>();
builder.Services.AddScoped<ITimelineEventRepository, TimelineEventRepository>();
builder.Services.AddScoped<IInteractionFlagRepository, InteractionFlagRepository>();
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddSingleton<InMemoryMagicLinkDelivery>();
var hasAzureEmail = !string.IsNullOrWhiteSpace(builder.Configuration["AzureCommunicationEmail:ConnectionString"]);
var hasSmtp = !string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]);
var isLocalFrontend = Uri.TryCreate(builder.Configuration["FrontendUrl"], UriKind.Absolute, out var frontendUri) &&
    (frontendUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
     frontendUri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase));
var useInMemoryMagicLinks = builder.Environment.IsDevelopment() || (!hasAzureEmail && !hasSmtp && isLocalFrontend);

if (hasAzureEmail)
{
    builder.Services.AddSingleton<IMagicLinkDelivery, AzureCommunicationEmailMagicLinkDelivery>();
}
else if (hasSmtp)
{
    builder.Services.AddSingleton<IMagicLinkDelivery, SmtpMagicLinkDelivery>();
}
else if (useInMemoryMagicLinks)
{
    builder.Services.AddSingleton<IMagicLinkDelivery>(sp => sp.GetRequiredService<InMemoryMagicLinkDelivery>());
}
else
{
    builder.Services.AddSingleton<IMagicLinkDelivery, SmtpMagicLinkDelivery>();
}
builder.Services.AddSingleton<IDevMagicLinkInbox>(sp => sp.GetRequiredService<InMemoryMagicLinkDelivery>());

// ── Domain services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IKnowledgeSource, DatabaseKnowledgeSource>();

builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ICompoundService, CompoundService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<IProtocolService, ProtocolService>();
builder.Services.AddScoped<IProtocolPhaseService, ProtocolPhaseService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<ICalculatorService, CalculatorService>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
builder.Services.AddScoped<IOverlapService, OverlapService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "BioStack Mission Control API";
        doc.Info.Version = "v1";
        doc.Info.Description = "Local-first biometrics and protocol observability platform";
        return Task.CompletedTask;
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors("ConfiguredOrigins");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("BioStack Mission Control API");
    options.WithTheme(ScalarTheme.Moon);
});

app.MapHealthChecks("/health");

app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapCompoundEndpoints();
app.MapCheckInEndpoints();
app.MapProtocolEndpoints();
app.MapProtocolPhaseEndpoints();
app.MapTimelineEndpoints();
app.MapCalculatorEndpoints();
app.MapKnowledgeEndpoints();
app.MapLeadEndpoints();
app.MapAdminEndpoints();

if (useInMemoryMagicLinks)
    app.MapDevAuthEndpoints();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();

    db.Database.EnsureCreated();

    if (db.Database.IsSqlite())
    {
        var createScript = DatabaseSchemaBootstrapper.MakeSqliteCreateScriptIdempotent(
            db.Database.GenerateCreateScript());

        if (!string.IsNullOrWhiteSpace(createScript))
        {
            db.Database.ExecuteSqlRaw(createScript);
        }

        DatabaseSchemaBootstrapper.BackfillMissingSqliteColumns(db);
    }

    // Seed Knowledge if empty
    if (!db.KnowledgeEntries.Any())
    {
        var source = new LocalKnowledgeSource();
        var initialData = source.GetAllCompoundsAsync().Result;
        db.KnowledgeEntries.AddRange(initialData);
        db.SaveChanges();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[FATAL] Database initialization failed: {ex.Message}");
}

app.Run();

public partial class Program { }
