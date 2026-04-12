using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            .AllowAnyHeader();
    });
});

// ── JWT Authentication ──────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be set in configuration or environment.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
builder.Services.AddScoped<IProtocolPhaseRepository, ProtocolPhaseRepository>();
builder.Services.AddScoped<ITimelineEventRepository, TimelineEventRepository>();
builder.Services.AddScoped<IInteractionFlagRepository, InteractionFlagRepository>();
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();

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
app.UseAuthentication();
app.UseAuthorization();

// Callback-secret middleware — blocks /oauth-callback unless the correct shared secret is present.
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api/v1/auth/oauth-callback"))
    {
        var expectedSecret = ctx.RequestServices.GetRequiredService<IConfiguration>()["Auth:CallbackSecret"];
        if (!string.IsNullOrEmpty(expectedSecret))
        {
            var provided = ctx.Request.Headers["X-Callback-Secret"].FirstOrDefault();
            if (provided != expectedSecret)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsync("Unauthorized");
                return;
            }
        }
    }
    await next(ctx);
});

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

if (app.Environment.IsDevelopment())
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
