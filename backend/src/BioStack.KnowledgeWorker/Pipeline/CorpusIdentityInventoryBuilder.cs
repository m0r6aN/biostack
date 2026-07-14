namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Builds a deterministic, metadata-only inventory of the repository seed, pilot candidate,
/// evidence-packet, and source-registry identity surfaces. It does not acquire sources,
/// inspect claim text, authorize promotion, or invoke a model or network.
/// </summary>
public sealed class CorpusIdentityInventoryBuilder
{
    public const string CurrentSnapshotVersion = "1.0.0";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _repositoryRoot;

    public CorpusIdentityInventoryBuilder(string? repositoryRoot = null)
    {
        _repositoryRoot = string.IsNullOrWhiteSpace(repositoryRoot)
            ? LocateRepositoryRoot()
            : Path.GetFullPath(repositoryRoot);
    }

    public CorpusIdentityInventorySnapshot Build()
    {
        var workerDirectory = Path.Combine(
            _repositoryRoot,
            "backend",
            "src",
            "BioStack.KnowledgeWorker");
        var schemaDirectory = Path.Combine(workerDirectory, "Schemas");
        var seedPath = Path.Combine(workerDirectory, "Seeds", "substances-seed.json");
        var candidatePath = Path.Combine(
            _repositoryRoot,
            "research",
            "input",
            "candidates",
            "pilot-compound-candidates.json");
        var sourceRegistryPath = Path.Combine(
            _repositoryRoot,
            "research",
            "input",
            "sources",
            "pilot-source-registry.json");
        var evidenceDirectory = Path.Combine(
            _repositoryRoot,
            "research",
            "input",
            "evidence");

        var seedRecords = new SubstanceRecordLoader().Load(seedPath);
        var seedValidator = SubstanceRecordValidator.LoadFromFile(
            Path.Combine(schemaDirectory, "substance-record.schema.json"));
        foreach (var record in seedRecords)
        {
            EnsureValid(seedValidator.Validate(record.Node), $"seed record {record.Index}");
        }

        var artifactLoader = new ResearchArtifactLoader();
        var artifactValidator = ResearchArtifactValidator.LoadFromDirectory(schemaDirectory);
        var candidateArtifact = LoadAndValidate(
            artifactLoader,
            artifactValidator,
            ResearchArtifactKind.CompoundCandidateBatch,
            candidatePath);
        var sourceRegistry = LoadAndValidate(
            artifactLoader,
            artifactValidator,
            ResearchArtifactKind.SourceRegistry,
            sourceRegistryPath);
        var evidenceArtifacts = Directory.GetFiles(evidenceDirectory, "*.evidence.json")
            .Order(StringComparer.Ordinal)
            .Select(path => LoadAndValidate(
                artifactLoader,
                artifactValidator,
                ResearchArtifactKind.EvidencePacket,
                path))
            .ToArray();

        var seedIdentities = seedRecords
            .Select(record => ProjectSeedIdentity(record.Node))
            .OrderBy(identity => identity.CanonicalId, StringComparer.Ordinal)
            .ToArray();
        var candidateIdentities = RequiredArray(candidateArtifact, "candidates")
            .Select(ProjectCandidateIdentity)
            .OrderBy(identity => identity.CanonicalId, StringComparer.Ordinal)
            .ToArray();
        var evidenceCanonicalIds = evidenceArtifacts
            .Select(ProjectEvidenceCanonicalId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        EnsureUnique(seedIdentities.Select(identity => identity.CanonicalId), "seed canonical IDs");
        EnsureUnique(candidateIdentities.Select(identity => identity.CanonicalId), "candidate canonical IDs");
        EnsureUnique(evidenceCanonicalIds, "evidence packet canonical IDs");

        var seedIds = seedIdentities
            .Select(identity => identity.CanonicalId)
            .ToHashSet(StringComparer.Ordinal);
        var candidateIds = candidateIdentities
            .Select(identity => identity.CanonicalId)
            .ToHashSet(StringComparer.Ordinal);
        var evidenceIds = evidenceCanonicalIds.ToHashSet(StringComparer.Ordinal);
        var sourceNodes = RequiredArray(sourceRegistry, "sources");
        var sourceRegistryAuthorizer = new SourceRegistryAuthorizer();
        var authorizedEvidencePacketCount = evidenceArtifacts.Count(packet =>
        {
            var result = sourceRegistryAuthorizer.Authorize(packet, sourceRegistry);
            return result.ReviewReasons.Count == 0 && result.QualityFlags.Count == 0;
        });

        var sources = sourceNodes.Select(node => RequiredObject(node, "source registry entry")).ToArray();

        return new CorpusIdentityInventorySnapshot(
            SnapshotVersion: CurrentSnapshotVersion,
            Scope: "repository-identity-and-provenance-metadata-only",
            SeedRecordCount: seedIdentities.Length,
            CandidateRecordCount: candidateIdentities.Length,
            EvidencePacketCount: evidenceCanonicalIds.Length,
            SourceRegistryRecordCount: sources.Length,
            SeedCandidateOverlapCount: seedIds.Intersect(candidateIds, StringComparer.Ordinal).Count(),
            SeedOnlyCanonicalIds: Difference(seedIds, candidateIds),
            CandidateOnlyCanonicalIds: Difference(candidateIds, seedIds),
            CandidatesMissingEvidenceCanonicalIds: Difference(candidateIds, evidenceIds),
            EvidenceWithoutCandidateCanonicalIds: Difference(evidenceIds, candidateIds),
            ApprovedRightsSourceCount: sources.Count(source =>
                StringComparer.Ordinal.Equals(
                    RequiredString(RequiredObject(source["rights"], "source rights"), "reviewStatus"),
                    "approved")),
            ActiveOperationsSourceCount: sources.Count(source =>
                StringComparer.Ordinal.Equals(
                    RequiredString(RequiredObject(source["operations"], "source operations"), "status"),
                    "active")),
            AcquisitionEnabledSourceCount: sources.Count(source =>
                RequiredBool(RequiredObject(source["acquisition"], "source acquisition"), "enabled")),
            RegistryAuthorizedEvidencePacketCount: authorizedEvidencePacketCount,
            IdentityTokenCollisions: BuildIdentityTokenCollisions(seedIdentities, candidateIdentities),
            ExternalIdentifierCollisions: BuildExternalIdentifierCollisions(seedIdentities, candidateIdentities),
            ModelInvoked: false,
            NetworkAccessed: false,
            Limitations:
            [
                "inventory records repository state only; it does not establish taxonomy coverage targets",
                "identity overlap and collision signals are not evidence, safety, regulatory, or clinical conclusions",
                "source registry authorization state is observed without granting legal or acquisition authority",
                "no claim text, raw customer data, model output, runtime, staging, production, or live source is evaluated",
            ]);
    }

    public string BuildJson()
        => JsonSerializer.Serialize(Build(), SerializerOptions);

    private static JsonObject LoadAndValidate(
        ResearchArtifactLoader loader,
        ResearchArtifactValidator validator,
        ResearchArtifactKind kind,
        string path)
    {
        var artifact = loader.Load(kind, path).Node;
        EnsureValid(validator.Validate(kind, artifact), $"{kind} artifact '{path}'");
        return RequiredObject(artifact, kind.ToString());
    }

    private static CorpusIdentityProjection ProjectSeedIdentity(JsonNode node)
    {
        var identity = RequiredObject(RequiredObject(node, "seed record")["identity"], "seed identity");
        return new CorpusIdentityProjection(
            Source: "seed",
            CanonicalId: RequiredString(identity, "canonicalId"),
            IdentityTokens: ReadIdentityTokens(identity, "canonicalName", "aliases", "brandNames", "synonyms"),
            ExternalIdentifiers: ReadExternalIdentifiers(identity));
    }

    private static CorpusIdentityProjection ProjectCandidateIdentity(JsonNode? node)
    {
        var candidate = RequiredObject(node, "candidate");
        var canonicalName = RequiredString(candidate, "canonicalNameCandidate");
        return new CorpusIdentityProjection(
            Source: "candidate",
            CanonicalId: SubstanceRecordNormalizer.Slugify(canonicalName),
            IdentityTokens: ReadIdentityTokens(
                candidate,
                "canonicalNameCandidate",
                "aliases",
                "brandNames"),
            ExternalIdentifiers: ReadExternalIdentifiers(candidate));
    }

    private static string ProjectEvidenceCanonicalId(JsonObject packet)
        => SubstanceRecordNormalizer.Slugify(
            RequiredString(RequiredObject(packet["compound"], "evidence compound"), "canonicalName"));

    private static IReadOnlyList<string> ReadIdentityTokens(
        JsonObject identity,
        string canonicalNameProperty,
        params string[] arrayProperties)
    {
        var tokens = new List<string> { RequiredString(identity, canonicalNameProperty) };
        foreach (var property in arrayProperties)
        {
            tokens.AddRange(ReadStringArray(identity[property]));
        }

        return tokens
            .Select(SubstanceRecordNormalizer.Slugify)
            .Where(token => token.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> ReadExternalIdentifiers(JsonObject identity)
    {
        if (identity["externalIdentifiers"] is not JsonObject identifiers)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return identifiers
            .Select(pair => new
            {
                Type = pair.Key,
                Value = pair.Value is JsonValue value && value.TryGetValue<string>(out var text)
                    ? text.Trim()
                    : string.Empty,
            })
            .Where(pair => pair.Value.Length > 0)
            .OrderBy(pair => pair.Type, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Type, pair => pair.Value, StringComparer.Ordinal);
    }

    private static IReadOnlyList<CorpusIdentityCollision> BuildIdentityTokenCollisions(
        IEnumerable<CorpusIdentityProjection> seed,
        IEnumerable<CorpusIdentityProjection> candidates)
        => seed.Concat(candidates)
            .SelectMany(identity => identity.IdentityTokens.Select(token => new
            {
                Token = token,
                Owner = $"{identity.Source}:{identity.CanonicalId}",
                identity.CanonicalId,
            }))
            .GroupBy(item => item.Token, StringComparer.Ordinal)
            .Select(group => new
            {
                Key = group.Key,
                CanonicalIds = group.Select(item => item.CanonicalId).Distinct(StringComparer.Ordinal).ToArray(),
                Owners = group.Select(item => item.Owner).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            })
            .Where(group => group.CanonicalIds.Length > 1)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new CorpusIdentityCollision("identity-token", group.Key, group.Owners))
            .ToArray();

    private static IReadOnlyList<CorpusIdentityCollision> BuildExternalIdentifierCollisions(
        IEnumerable<CorpusIdentityProjection> seed,
        IEnumerable<CorpusIdentityProjection> candidates)
        => seed.Concat(candidates)
            .SelectMany(identity => identity.ExternalIdentifiers.Select(pair => new
            {
                Key = $"{pair.Key}:{pair.Value.ToLowerInvariant()}",
                Owner = $"{identity.Source}:{identity.CanonicalId}",
                identity.CanonicalId,
            }))
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .Select(group => new
            {
                Key = group.Key,
                CanonicalIds = group.Select(item => item.CanonicalId).Distinct(StringComparer.Ordinal).ToArray(),
                Owners = group.Select(item => item.Owner).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            })
            .Where(group => group.CanonicalIds.Length > 1)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new CorpusIdentityCollision("external-identifier", group.Key, group.Owners))
            .ToArray();

    private static string[] Difference(IEnumerable<string> left, IEnumerable<string> right)
        => left.Except(right, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

    private static void EnsureUnique(IEnumerable<string> values, string label)
    {
        var items = values.ToArray();
        if (items.Distinct(StringComparer.Ordinal).Count() != items.Length)
        {
            throw ContractError($"{label} must be unique.");
        }
    }

    private static void EnsureValid(ValidationResult result, string label)
    {
        if (!result.IsValid)
        {
            throw ContractError($"{label} failed schema validation: {result.Summary()}");
        }
    }

    private static JsonObject RequiredObject(JsonNode? node, string label)
        => node is JsonObject obj
            ? obj
            : throw ContractError($"Required object '{label}' is missing.");

    private static JsonArray RequiredArray(JsonObject root, string propertyName)
        => root[propertyName] is JsonArray array
            ? array
            : throw ContractError($"Required array '{propertyName}' is missing.");

    private static string RequiredString(JsonObject root, string propertyName)
        => root[propertyName]?.GetValue<string>() is { Length: > 0 } value
            ? value
            : throw ContractError($"Required string '{propertyName}' is missing.");

    private static bool RequiredBool(JsonObject root, string propertyName)
        => root[propertyName] is JsonValue value && value.TryGetValue<bool>(out var result)
            ? result
            : throw ContractError($"Required boolean '{propertyName}' is missing.");

    private static IReadOnlyList<string> ReadStringArray(JsonNode? node)
        => node is JsonArray array
            ? array.Select(item => item?.GetValue<string>()?.Trim() ?? string.Empty)
                .Where(value => value.Length > 0)
                .ToArray()
            : Array.Empty<string>();

    private static InvalidOperationException ContractError(string message)
        => new($"KEO-75 corpus identity inventory invalid: {message}");

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

    private sealed record CorpusIdentityProjection(
        string Source,
        string CanonicalId,
        IReadOnlyList<string> IdentityTokens,
        IReadOnlyDictionary<string, string> ExternalIdentifiers);
}

public sealed record CorpusIdentityInventorySnapshot(
    string SnapshotVersion,
    string Scope,
    int SeedRecordCount,
    int CandidateRecordCount,
    int EvidencePacketCount,
    int SourceRegistryRecordCount,
    int SeedCandidateOverlapCount,
    IReadOnlyList<string> SeedOnlyCanonicalIds,
    IReadOnlyList<string> CandidateOnlyCanonicalIds,
    IReadOnlyList<string> CandidatesMissingEvidenceCanonicalIds,
    IReadOnlyList<string> EvidenceWithoutCandidateCanonicalIds,
    int ApprovedRightsSourceCount,
    int ActiveOperationsSourceCount,
    int AcquisitionEnabledSourceCount,
    int RegistryAuthorizedEvidencePacketCount,
    IReadOnlyList<CorpusIdentityCollision> IdentityTokenCollisions,
    IReadOnlyList<CorpusIdentityCollision> ExternalIdentifierCollisions,
    bool ModelInvoked,
    bool NetworkAccessed,
    IReadOnlyList<string> Limitations);

public sealed record CorpusIdentityCollision(
    string CollisionType,
    string Key,
    IReadOnlyList<string> Owners);
