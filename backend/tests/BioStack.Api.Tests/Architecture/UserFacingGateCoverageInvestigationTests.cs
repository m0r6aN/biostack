namespace BioStack.Api.Tests.Architecture;

using System.Reflection;
using BioStack.Application.Governance;
using BioStack.Api.Tests.Integration;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "Architecture")]
public sealed class UserFacingGateCoverageInvestigationTests(ITestOutputHelper output)
{
    private static readonly ExpectedEndpoint[] Catalog =
    [
        new("AnalyzeProtocol", "POST", "/api/analyze/protocol", false),
        new("GetProtocolReview", "GET", "/api/v1/protocols/{id}/review", false),
        new("GetProtocolPatterns", "GET", "/api/v1/protocols/{id}/patterns", false),
        new("GetProtocolDrift", "GET", "/api/v1/protocols/{id}/drift", false),
        new("GetAllCompounds", "GET", "/api/v1/knowledge/compounds", false),
        new("GetCompound", "GET", "/api/v1/knowledge/compounds/{name}", false),
        new("CheckOverlap", "POST", "/api/v1/knowledge/overlap-check", false),
        new("CheckInteractions", "POST", "/api/v1/knowledge/interaction-check", false),
        new(
            "GetCompoundRelationships",
            "GET",
            "/api/v1/intelligence/compounds/{compound}/relationships",
            true),
        new("GetCompoundCompatibility", "GET", "/api/v1/intelligence/compatibility", true),
        new("GenerateStackDeliberationEnvelope", "POST", "/api/v1/stack-review/envelope", true),
    ];

    [Fact]
    public async Task Runtime_inventory_records_bounded_gate_and_auth_metadata_without_claiming_endpoint_wide_coverage()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"biostack-q02-endpoint-inventory-{Guid.NewGuid():N}.db");
        WebApplicationFactory<Program>? factory = null;
        Exception? inventoryFailure = null;
        Exception? cleanupFailure = null;
        var rows = new List<InventoryRow>();
        var drift = new List<string>();

        output.WriteLine($"TEMP_SQLITE path={databasePath}");

        try
        {
            factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("environment", "Development");
                    builder.ConfigureLogging(logging => logging.ClearProviders());
                    builder.ConfigureAppConfiguration((_, configuration) =>
                    {
                        configuration.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath}",
                            ["Database:Provider"] = "sqlite",
                            ["FrontendUrl"] = "http://localhost:3043",
                            ["PublicApiUrl"] = "http://localhost:5000",
                            ["Jwt:Secret"] = "q02-local-test-secret-at-least-32-characters",
                            ["Jwt:Issuer"] = "biostack",
                            ["Jwt:Audience"] = "biostack-ui",
                            ["KeonCollective:LiveMode"] = "false",
                            ["KeonCollective:ControlBaseUrl"] = string.Empty,
                            ["KeonRuntime:LiveMode"] = "false",
                            ["KeonRuntime:BaseUrl"] = string.Empty,
                        });
                    });
                    builder.ConfigureServices(services =>
                    {
                        services.UseTestKeonRuntimeClient();
                        services.RemoveBioStackDbContext();
                        services.AddDbContext<BioStackDbContext>(options =>
                            options.UseSqlite($"Data Source={databasePath}"));
                    });
                });

            // Resolving Services starts the in-memory application locally. No HttpClient is
            // created and no route request is issued by this inventory.
            var endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();
            var routeEndpoints = endpointDataSource.Endpoints.OfType<RouteEndpoint>().ToArray();

            foreach (var expected in Catalog)
            {
                var matches = routeEndpoints
                    .Where(endpoint => string.Equals(
                        endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName,
                        expected.Name,
                        StringComparison.Ordinal))
                    .ToArray();

                if (matches.Length != 1)
                {
                    drift.Add($"{expected.Name}: expected one runtime endpoint, found {matches.Length}");
                    continue;
                }

                var endpoint = matches[0];
                var methods = (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [])
                    .Select(method => method.ToUpperInvariant())
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(method => method, StringComparer.Ordinal)
                    .ToArray();
                var pattern = NormalizePattern(endpoint.RoutePattern.RawText);
                var tags = endpoint.Metadata
                    .GetOrderedMetadata<ITagsMetadata>()
                    .SelectMany(metadata => metadata.Tags)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(tag => tag, StringComparer.Ordinal)
                    .ToArray();
                var authorization = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().ToArray();
                var policies = authorization
                    .Select(metadata => metadata.Policy)
                    .Where(policy => !string.IsNullOrWhiteSpace(policy))
                    .Select(policy => policy!)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(policy => policy, StringComparer.Ordinal)
                    .ToArray();
                var handler = endpoint.Metadata.GetMetadata<MethodInfo>();
                var directlyDeclaresGate = handler?.GetParameters().Any(parameter =>
                    parameter.ParameterType == typeof(IUserFacingIntelligenceGate)) ?? false;

                var row = new InventoryRow(
                    expected.Name,
                    methods,
                    pattern,
                    tags,
                    authorization.Length > 0,
                    policies,
                    handler is not null,
                    handler?.DeclaringType?.FullName,
                    handler?.Name,
                    directlyDeclaresGate,
                    expected.PositiveControl);
                rows.Add(row);

                if (!methods.SequenceEqual([expected.Method], StringComparer.Ordinal))
                {
                    drift.Add(
                        $"{expected.Name}: expected method {expected.Method}, found [{string.Join(",", methods)}]");
                }

                if (!string.Equals(pattern, expected.Pattern, StringComparison.Ordinal))
                {
                    drift.Add($"{expected.Name}: expected pattern {expected.Pattern}, found {pattern}");
                }
            }

            foreach (var row in rows.OrderBy(row => row.Name, StringComparer.Ordinal))
            {
                output.WriteLine(
                    "ROW " +
                    $"name={row.Name} " +
                    $"method=[{string.Join(",", row.Methods)}] " +
                    $"pattern={row.Pattern} " +
                    $"tags=[{string.Join(",", row.Tags)}] " +
                    $"authorization={row.HasAuthorization} " +
                    $"policies=[{string.Join(",", row.Policies)}] " +
                    $"handler_metadata={row.HasHandlerMetadata} " +
                    $"handler={FormatHandler(row)} " +
                    $"direct_gate_parameter={row.DirectlyDeclaresGate}");
            }

            var positiveControls = rows.Where(row => row.PositiveControl).ToArray();
            foreach (var row in positiveControls.Where(row => !row.HasAuthorization))
            {
                drift.Add($"{row.Name}: positive control lacks authorization metadata");
            }

            var allHandlersAvailable = rows.Count == Catalog.Length && rows.All(row => row.HasHandlerMetadata);
            if (allHandlersAvailable)
            {
                foreach (var row in positiveControls.Where(row => !row.DirectlyDeclaresGate))
                {
                    drift.Add($"{row.Name}: positive control lacks the direct gate parameter");
                }
            }

            output.WriteLine(
                "RECOMMENDATION_SURFACE_MARKER " +
                "contract=none-on-pinned-base present=False " +
                "textual-or-semantic-guessing=forbidden");

            if (drift.Count > 0)
            {
                output.WriteLine("OUTCOME INCONCLUSIVE_INVENTORY_DRIFT");
                foreach (var item in drift.OrderBy(item => item, StringComparer.Ordinal))
                {
                    output.WriteLine($"DRIFT {item}");
                }
            }
            else if (!allHandlersAvailable)
            {
                var affectedRows = rows
                    .Where(row => !row.HasHandlerMetadata)
                    .Select(row => row.Name)
                    .OrderBy(name => name, StringComparer.Ordinal);
                output.WriteLine(
                    "HANDLER_METADATA_UNAVAILABLE " +
                    $"rows=[{string.Join(",", affectedRows)}]");
                output.WriteLine("OUTCOME INCONCLUSIVE_NO_HANDLER_METADATA_SEAM");
            }
            else
            {
                output.WriteLine("OUTCOME INCONCLUSIVE_NO_COMPLETE_RECOMMENDATION_SURFACE_MARKER");
            }
        }
        catch (Exception exception)
        {
            inventoryFailure = exception;
        }
        finally
        {
            if (factory is not null)
            {
                try
                {
                    await factory.DisposeAsync();
                }
                catch (Exception exception)
                {
                    cleanupFailure = exception;
                }
            }

            try
            {
                SqliteConnection.ClearAllPools();
            }
            catch (Exception exception)
            {
                cleanupFailure ??= exception;
            }

            try
            {
                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }

                if (File.Exists(databasePath))
                {
                    throw new IOException("The exact Q02 temporary SQLite file still exists after deletion.");
                }
            }
            catch (Exception exception)
            {
                cleanupFailure ??= exception;
            }
        }

        if (cleanupFailure is not null)
        {
            output.WriteLine("OUTCOME SCOPE_OR_CLEANUP_BLOCKED");
            Assert.Fail($"Q02 exact temporary-file cleanup failed: {cleanupFailure.Message}");
        }

        output.WriteLine($"TEMP_SQLITE deleted={!File.Exists(databasePath)}");

        if (inventoryFailure is not null)
        {
            output.WriteLine("OUTCOME ENVIRONMENT_BLOCKED");
            Assert.Fail($"Q02 local runtime inventory failed: {inventoryFailure}");
        }

        Assert.True(rows.Count == Catalog.Length, "The bounded catalog was not fully inventoried.");
        Assert.Empty(drift);
    }

    private static string NormalizePattern(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return "<missing>";
        }

        return "/" + rawText.TrimStart('/');
    }

    private static string FormatHandler(InventoryRow row)
        => row.HasHandlerMetadata
            ? $"{row.HandlerDeclaringType}.{row.HandlerName}"
            : "<unavailable>";

    private sealed record ExpectedEndpoint(
        string Name,
        string Method,
        string Pattern,
        bool PositiveControl);

    private sealed record InventoryRow(
        string Name,
        string[] Methods,
        string Pattern,
        string[] Tags,
        bool HasAuthorization,
        string[] Policies,
        bool HasHandlerMetadata,
        string? HandlerDeclaringType,
        string? HandlerName,
        bool DirectlyDeclaresGate,
        bool PositiveControl);
}
