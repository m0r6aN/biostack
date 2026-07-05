namespace BioStack.Application.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

/// <summary>
/// Deterministic inventory manifest guard for the Protocol Operations offline verification kit.
/// The manifest enumerates the components an auditor should expect in the kit (contracts, verifier
/// CLI, CLI surfaces, docs, and the test-owned boundary/dependency guards) without shipping any
/// product surface. This guard is inventory-only: it does not add endpoints, persistence, PDF
/// generation, frontend behavior, or Protocol Intelligence runtime behavior.
/// </summary>
public sealed class ProtocolOperationsExportBundleOfflineKitInventoryManifestTests
{
    private static readonly string[] ExpectedComponentIdsInOrder =
    [
        "auditor-packet-design-note",
        "boundary-language-expectations",
        "cli-help-usage",
        "cli-result-json",
        "dependency-boundary-guard",
        "docs-drilldown",
        "export-bundle-contract",
        "install-runbook",
        "release-checklist",
        "result-code-catalog",
        "smoke-scripts",
        "verification-receipt-contract",
        "verifier-cli",
    ];

    private static readonly string[] ForbiddenComponentTypes =
    [
        "api-endpoint-artifact",
        "database-persistence-artifact",
        "frontend-artifact",
        "medical-guidance-artifact",
        "pdf-generator-artifact",
        "runtime-protocol-intelligence-artifact",
    ];

    [Fact]
    public void InventoryManifest_SnapshotExists()
    {
        Assert.True(
            File.Exists(ManifestSnapshotPath()),
            $"Expected inventory manifest snapshot at '{ManifestSnapshotPath()}'.");
    }

    [Fact]
    public void InventoryManifest_SnapshotShapeIsFrozen()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestSnapshotPath()));
        var root = manifest.RootElement;

        Assert.Equal(
            "biostack.protocol-operations-offline-kit-inventory-manifest",
            root.GetProperty("manifestSchemaId").GetString());
        Assert.Equal("1.0.0", root.GetProperty("manifestSchemaVersion").GetString());
        Assert.Equal(
            "protocol_operations_offline_kit_inventory_manifest",
            root.GetProperty("manifestScope").GetString());

        var posture = root.GetProperty("manifestPosture");
        Assert.True(posture.GetProperty("backendOnly").GetBoolean());
        Assert.True(posture.GetProperty("testOwned").GetBoolean());
        Assert.True(posture.GetProperty("inventoryOnly").GetBoolean());
        Assert.True(posture.GetProperty("nonProductSurface").GetBoolean());

        Assert.True(root.TryGetProperty("components", out _));
        Assert.True(root.TryGetProperty("forbiddenComponentTypes", out _));
        Assert.True(root.TryGetProperty("boundaries", out _));

        // Each component entry exposes exactly the frozen contract shape.
        foreach (var component in root.GetProperty("components").EnumerateArray())
        {
            var propertyNames = component.EnumerateObject().Select(p => p.Name).ToArray();
            Assert.Equal(
                new[] { "id", "type", "expectedLocation", "required", "hashSurface", "boundaryPosture" },
                propertyNames);
        }
    }

    [Fact]
    public void InventoryManifest_ListsAllExpectedComponentsInDeterministicOrder()
    {
        var actualIds = ComponentIdsFromSnapshot();

        Assert.Equal(ExpectedComponentIdsInOrder, actualIds);

        // Ordering is deterministic: ascending, ordinal, and duplicate-free.
        var sorted = actualIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        Assert.Equal(sorted, actualIds);
        Assert.Equal(actualIds.Length, actualIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void InventoryManifest_AllRequiredComponentsResolveToRealRepositoryLocations()
    {
        var root = RepositoryRoot();
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestSnapshotPath()));

        var missing = new List<string>();
        foreach (var component in manifest.RootElement.GetProperty("components").EnumerateArray())
        {
            if (!component.GetProperty("required").GetBoolean())
            {
                continue;
            }

            var location = component.GetProperty("expectedLocation").GetString()!;
            var absolute = Path.Combine(root, location.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute))
            {
                missing.Add($"{component.GetProperty("id").GetString()} -> {location}");
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void InventoryManifest_ContainsNoForbiddenComponent()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestSnapshotPath()));
        var root = manifest.RootElement;

        var declaredForbidden = root.GetProperty("forbiddenComponentTypes")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        Assert.Equal(ForbiddenComponentTypes, declaredForbidden);

        // No declared component may use a forbidden type, and no component location may point at a
        // persistence, endpoint, frontend, PDF, runtime, or medical-guidance surface.
        foreach (var component in root.GetProperty("components").EnumerateArray())
        {
            var type = component.GetProperty("type").GetString()!;
            Assert.DoesNotContain(type, ForbiddenComponentTypes);

            var location = component.GetProperty("expectedLocation").GetString()!;
            Assert.DoesNotContain("Migrations", location, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Controllers", location, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Endpoints", location, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("frontend/", location, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(".tsx", location, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Pdf", location, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void InventoryManifest_DeclaresProtectiveBoundaryPosture()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestSnapshotPath()));
        var boundaries = manifest.RootElement.GetProperty("boundaries");

        Assert.True(boundaries.GetProperty("noMedicalAdvice").GetBoolean());
        Assert.True(boundaries.GetProperty("noPdfGeneration").GetBoolean());
        Assert.True(boundaries.GetProperty("noPersistenceOrDatabaseState").GetBoolean());
        Assert.True(boundaries.GetProperty("noProtocolIntelligenceRuntimeBehavior").GetBoolean());
        Assert.True(boundaries.GetProperty("noApiEndpointSurface").GetBoolean());
        Assert.True(boundaries.GetProperty("noFrontendSurface").GetBoolean());
        Assert.True(boundaries.GetProperty("suppliedArtifactInspectionOnly").GetBoolean());

        // Every component carries a non-empty boundary posture from the frozen vocabulary.
        var allowedPostures = new[]
        {
            "supplied-artifact-inspection-only",
            "documentation-non-authority",
            "test-guard-non-product",
            "design-note-non-product",
        };
        foreach (var component in manifest.RootElement.GetProperty("components").EnumerateArray())
        {
            var posture = component.GetProperty("boundaryPosture").GetString();
            Assert.False(string.IsNullOrWhiteSpace(posture));
            Assert.Contains(posture, allowedPostures);
        }
    }

    [Fact]
    public void InventoryManifest_ReparsesToStableCanonicalJson()
    {
        var raw = File.ReadAllText(ManifestSnapshotPath());
        var first = CanonicalizeJson(raw);
        var second = CanonicalizeJson(first);

        Assert.Equal(first, second);
    }

    private static string[] ComponentIdsFromSnapshot()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestSnapshotPath()));
        return manifest.RootElement.GetProperty("components")
            .EnumerateArray()
            .Select(component => component.GetProperty("id").GetString()!)
            .ToArray();
    }

    private static string CanonicalizeJson(string json)
    {
        return JsonNode.Parse(json)!.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static string ManifestSnapshotPath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "backend",
            "tests",
            "Fixtures",
            "ProtocolOperationsExportBundle",
            "ProtocolOperationsOfflineKitInventoryManifest.golden.json");
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "backend", "BioStack.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate BioStack repository root.");
    }
}
