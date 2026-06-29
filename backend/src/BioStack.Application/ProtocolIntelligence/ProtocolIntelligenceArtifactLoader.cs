namespace BioStack.Application.ProtocolIntelligence;

using System.Text.Json;

public interface IProtocolIntelligenceArtifactLoader
{
    ProtocolIntelligenceArtifactSet Load();
}

/// <summary>
/// Loads the canonical Protocol Intelligence taxonomy/promotion-target artifacts from
/// <c>research/protocol-intelligence/*.json</c>. These JSON files are validation inputs
/// to the build-time gate — never runtime truth and never user-facing output.
/// </summary>
public sealed class ProtocolIntelligenceArtifactLoader : IProtocolIntelligenceArtifactLoader
{
    private readonly string _repositoryRoot;
    private ProtocolIntelligenceArtifactSet? _cached;

    public ProtocolIntelligenceArtifactLoader(string? repositoryRoot = null)
    {
        _repositoryRoot = string.IsNullOrWhiteSpace(repositoryRoot) ? LocateRepositoryRoot() : repositoryRoot;
    }

    public ProtocolIntelligenceArtifactSet Load()
        => _cached ??= LoadCore();

    private ProtocolIntelligenceArtifactSet LoadCore()
    {
        var artifactVersions = new Dictionary<string, string>(StringComparer.Ordinal);
        using var promotionTargets = ReadJson("promotion-target-specs.json", artifactVersions);
        using var relationships = ReadJson("relationship-taxonomy.json", artifactVersions);
        using var highRisk = ReadJson("high-risk-guardrails.json", artifactVersions);
        using var sideEffects = ReadJson("side-effect-ambiguity-detector.json", artifactVersions);
        using var sourceQuality = ReadJson("source-quality-taxonomy.json", artifactVersions);
        using var phases = ReadJson("protocol-phase-taxonomy.json", artifactVersions);
        using var glp1 = ReadJson("glp1-observability-pack.json", artifactVersions);

        var targets = promotionTargets.RootElement.GetProperty("targets").EnumerateArray()
            .Select(target => new PromotionTargetContract(
                Id: RequiredString(target, "id"),
                RequiredFields: RequiredStringArray(target, "requiredFields"),
                ReviewGate: RequiredString(target, "reviewGate"),
                ForbiddenOutputScanRequired: target.TryGetProperty("forbiddenOutputScanRequired", out var scan) && scan.GetBoolean()))
            .ToDictionary(target => target.Id, StringComparer.Ordinal);

        var relationshipIds = relationships.RootElement.GetProperty("relationshipTypes").EnumerateArray()
            .Select(item => RequiredString(item, "id"))
            .ToArray();

        var globalBlockedOutputs = highRisk.RootElement.GetProperty("globalBlockedOutputs").EnumerateArray()
            .Select(item => item.GetString() ?? throw new InvalidOperationException("globalBlockedOutputs contains null."))
            .ToHashSet(StringComparer.Ordinal);

        var allBlockedOutputs = new HashSet<string>(globalBlockedOutputs, StringComparer.Ordinal);
        foreach (var category in highRisk.RootElement.GetProperty("categories").EnumerateArray())
        {
            foreach (var blockedOutput in RequiredStringArray(category, "blockedOutputs"))
            {
                allBlockedOutputs.Add(blockedOutput);
            }
        }

        var sourceClasses = sourceQuality.RootElement.GetProperty("sourceClasses").EnumerateArray()
            .Select(sourceClass => new SourceClassContract(
                Id: RequiredString(sourceClass, "id"),
                WarningFirst: sourceClass.TryGetProperty("warningFirst", out var warningFirst) && warningFirst.GetBoolean(),
                BlockedOutputs: RequiredStringArray(sourceClass, "blockedOutputs")))
            .ToDictionary(sourceClass => sourceClass.Id, StringComparer.Ordinal);

        var observabilityModules = new[]
        {
            RequiredString(phases.RootElement, "recordType"),
            RequiredString(glp1.RootElement, "recordType"),
            RequiredString(sideEffects.RootElement, "recordType"),
            RequiredString(sourceQuality.RootElement, "recordType"),
            RequiredString(highRisk.RootElement, "recordType")
        };

        return new ProtocolIntelligenceArtifactSet(
            PromotionTargets: targets,
            SupportedRelationshipIds: relationshipIds,
            GlobalBlockedOutputs: globalBlockedOutputs,
            AllBlockedOutputs: allBlockedOutputs,
            SideEffectRequiredArtifactFields: RequiredStringArray(sideEffects.RootElement, "requiredArtifactFields"),
            SourceClasses: sourceClasses,
            ArtifactVersions: artifactVersions,
            AvailableObservabilityModules: observabilityModules);
    }

    private JsonDocument ReadJson(string fileName, Dictionary<string, string> artifactVersions)
    {
        var path = Path.Combine(_repositoryRoot, "research", "protocol-intelligence", fileName);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Protocol Intelligence artifact is missing: {path}");
        }

        try
        {
            var document = JsonDocument.Parse(File.ReadAllText(path));
            artifactVersions[fileName] = RequiredString(document.RootElement, "schemaVersion");
            return document;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Protocol Intelligence artifact is malformed: {path}", ex);
        }
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Required string property '{propertyName}' is missing.");
        }

        return property.GetString()!;
    }

    private static string[] RequiredStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"Required array property '{propertyName}' is missing.");
        }

        return property.EnumerateArray()
            .Select(item => item.GetString() ?? throw new InvalidOperationException($"'{propertyName}' contains null."))
            .ToArray();
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs")) &&
                Directory.Exists(Path.Combine(directory.FullName, "research")) &&
                Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the BioStack repository root.");
    }
}
