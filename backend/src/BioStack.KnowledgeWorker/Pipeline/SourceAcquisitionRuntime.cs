namespace BioStack.KnowledgeWorker.Pipeline;

using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BioStack.KnowledgeWorker.Config;

public static class SourceAcquisitionRuntimeLimits
{
    public const int MaximumInputBytes = 4 * 1024 * 1024;
    public const int MaximumAttemptBytes = 8 * 1024 * 1024;
    public const int MaximumCandidatesPerIntent = 100;
    public const int MaximumFieldsPerCandidate = 256;
    public const int MaximumValuesPerField = 128;
    public const int MaximumTextLength = 64 * 1024;
    public const int MaximumErrorCodeLength = 128;
    public const int MaximumErrorMessageLength = 512;
    public const int MaximumRetentionDays = 3650;
}

public sealed record SourceAcquisitionInputBindings(
    string ResearchRequestSha256,
    string SourceDecisionSha256,
    string SourceRegistrySha256);

public sealed record SourceAcquisitionRuntimeConfiguration(
    string ResearchOutputDirectory,
    string CycleId,
    int CandidateRetentionDays,
    int ReceiptRetentionDays,
    string StorageProvider = "File",
    string? BlobServiceUri = null,
    string? BlobContainerName = null,
    string BlobPrefix = "source-acquisition",
    string? ManagedIdentityClientId = null,
    bool IsProduction = false);

internal interface ISourceAcquisitionRunLease : IAsyncDisposable
{
    CancellationToken LeaseLost { get; }
}

internal interface ISourceAcquisitionArtifactStore
{
    string Location { get; }

    Task<ISourceAcquisitionRunLease> AcquireRunLeaseAsync(
        CancellationToken cancellationToken);

    Task<SourceAcquisitionAttemptArtifact?> TryReadAttemptAsync(
        string intentId,
        SourceAcquisitionIntent intent,
        SourceAcquisitionPreflightEntry entry,
        SourceAcquisitionInputBindings bindings,
        SourceAcquisitionRuntimeConfiguration configuration,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task WriteAttemptAndCheckpointAsync(
        SourceAcquisitionAttemptArtifact attempt,
        CancellationToken cancellationToken);

    Task EnsureCheckpointAsync(
        SourceAcquisitionAttemptArtifact attempt,
        CancellationToken cancellationToken);

    Task WriteDerivedArtifactsAsync(
        SourceAcquisitionRunManifest manifest,
        SourceAcquisitionReviewQueue reviewQueue,
        CancellationToken cancellationToken);
}

public sealed record SourceAcquisitionAttemptArtifact(
    string SchemaVersion,
    string CycleId,
    string IntentId,
    int StableOrdinal,
    string SourceId,
    string AdapterId,
    string RequestId,
    string CompoundName,
    string CandidateMethod,
    string RegistryBindingSha256,
    SourceAcquisitionInputBindings InputBindings,
    IReadOnlyList<string> SearchTerms,
    IReadOnlyList<string> AuthorizedFieldUses,
    IReadOnlyList<string> RequiredProvenanceFields,
    string Status,
    DateTimeOffset CompletedAtUtc,
    DateTimeOffset RetainUntilUtc,
    bool Truncated,
    string? RetryAfter,
    string? ErrorCode,
    string? ErrorMessage,
    string? TombstoneOriginalStatus,
    IReadOnlyList<SourceAcquisitionCandidate> Candidates);

public sealed record SourceAcquisitionCheckpoint(
    string SchemaVersion,
    string CycleId,
    string IntentId,
    string Status,
    string AttemptRelativePath,
    string AttemptSha256,
    DateTimeOffset CompletedAtUtc);

public sealed record SourceAcquisitionTombstone(
    string SchemaVersion,
    string CycleId,
    string IntentId,
    int StableOrdinal,
    string SourceId,
    string RequestId,
    string OriginalStatus,
    string AttemptSha256,
    DateTimeOffset CompletedAtUtc,
    DateTimeOffset RetainUntilUtc,
    DateTimeOffset RemovedAtUtc,
    string RemovalReason);

public sealed record SourceAcquisitionReviewQueueItem(
    string ItemId,
    string IntentId,
    string SourceId,
    string RequestId,
    string CompoundName,
    string Status,
    int CandidateCount,
    string Reason,
    IReadOnlyList<string> References);

public sealed record SourceAcquisitionReviewQueue(
    string SchemaVersion,
    string CycleId,
    IReadOnlyList<SourceAcquisitionReviewQueueItem> Items);

public sealed record SourceAcquisitionRunManifest(
    string SchemaVersion,
    string CycleId,
    SourceAcquisitionInputBindings InputBindings,
    int UniqueRequestCount,
    int IntentCount,
    int ReadyCount,
    int BlockedCount,
    int SourceCount,
    int CompletedCount,
    int NoMatchCount,
    int RateLimitedCount,
    int BackPressureCount,
    int TruncatedCount,
    int ErrorCount,
    int ManualReviewPendingCount,
    int NotAttemptedCount,
    int ExpiredCount,
    bool Complete,
    IReadOnlyList<string> IntentIds);

public sealed record SourceAcquisitionRunResult(
    SourceAcquisitionRunManifest Manifest,
    string OutputDirectory);

public interface ISourceAcquisitionAdapterFactory
{
    IReadOnlyDictionary<string, ISourceAcquisitionAdapter> Create(
        string expectedRegistrySha256,
        WorkerOptions options);

    IReadOnlyList<SourceAcquisitionAdapterDescriptor> Descriptors { get; }
}

public sealed class SourceAcquisitionAdapterFactory : ISourceAcquisitionAdapterFactory
{
    private static readonly IReadOnlyList<SourceAcquisitionAdapterDescriptor>
        FixedDescriptors =
        Array.AsReadOnly<SourceAcquisitionAdapterDescriptor>(
        new SourceAcquisitionAdapterDescriptor[]
        {
            new("fda", "fda-planning-v1", "api"),
            new("pubchem", "pubchem-planning-v1", "api"),
            new("pubmed", "pubmed-planning-v1", "api"),
            new("clinicaltrials", "clinicaltrials-planning-v1", "api"),
            new("dailymed", "dailymed-planning-v1", "api"),
            new("nih-ods", "nih-ods-planning-v1", "api"),
            new("nih-nccih", "nih-nccih-planning-v1", "manual-review"),
        });

    public IReadOnlyList<SourceAcquisitionAdapterDescriptor> Descriptors =>
        FixedDescriptors;

    public IReadOnlyDictionary<string, ISourceAcquisitionAdapter> Create(
        string expectedRegistrySha256,
        WorkerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!string.IsNullOrWhiteSpace(options.SourceAcquisitionPubMedApiKey))
        {
            throw new InvalidOperationException(
                "PubMed API-key configuration is not approved for this runtime.");
        }
        if (string.IsNullOrWhiteSpace(options.SourceAcquisitionPubMedTool)
            || string.IsNullOrWhiteSpace(options.SourceAcquisitionPubMedContactEmail))
        {
            throw new InvalidOperationException(
                "PubMed tool name and contact email are required.");
        }

        var adapters = new ISourceAcquisitionAdapter[]
        {
            new FdaOpenFdaDrugLabelAcquisitionAdapter(expectedRegistrySha256),
            new PubChemPugRestCompoundAcquisitionAdapter(expectedRegistrySha256),
            new PubMedEutilitiesCitationMetadataAcquisitionAdapter(
                expectedRegistrySha256,
                options.SourceAcquisitionPubMedTool,
                options.SourceAcquisitionPubMedContactEmail),
            new ClinicalTrialsGovV2AcquisitionAdapter(expectedRegistrySha256),
            new DailyMedSplListJsonAcquisitionAdapter(expectedRegistrySha256),
            new NihOdsFactSheetAcquisitionAdapter(expectedRegistrySha256),
        };
        return new ReadOnlyDictionary<string, ISourceAcquisitionAdapter>(
            adapters.ToDictionary(adapter => adapter.SourceId, StringComparer.Ordinal));
    }
}

public interface ISourceAcquisitionRunner
{
    Task<SourceAcquisitionRunResult> RunAsync(
        SourceAcquisitionPlan plan,
        SourceAcquisitionExecutionPreflightResult preflight,
        IReadOnlyDictionary<string, ISourceAcquisitionAdapter> adapters,
        SourceAcquisitionInputBindings inputBindings,
        SourceAcquisitionRuntimeConfiguration configuration,
        CancellationToken cancellationToken = default);
}

public sealed partial class SourceAcquisitionRunner : ISourceAcquisitionRunner
{
    internal const string SchemaVersion = "source-acquisition-runtime-v1";
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private readonly TimeProvider _timeProvider;
    private readonly Func<
        SourceAcquisitionRuntimeConfiguration,
        ISourceAcquisitionArtifactStore> _storeFactory;

    public SourceAcquisitionRunner()
        : this(TimeProvider.System, CreateStore)
    {
    }

    internal SourceAcquisitionRunner(TimeProvider timeProvider)
        : this(timeProvider, CreateStore)
    {
    }

    internal SourceAcquisitionRunner(
        TimeProvider timeProvider,
        Func<
            SourceAcquisitionRuntimeConfiguration,
            ISourceAcquisitionArtifactStore> storeFactory)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _storeFactory = storeFactory
                        ?? throw new ArgumentNullException(nameof(storeFactory));
    }

    public async Task<SourceAcquisitionRunResult> RunAsync(
        SourceAcquisitionPlan plan,
        SourceAcquisitionExecutionPreflightResult preflight,
        IReadOnlyDictionary<string, ISourceAcquisitionAdapter> adapters,
        SourceAcquisitionInputBindings inputBindings,
        SourceAcquisitionRuntimeConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(plan, preflight, adapters, inputBindings, configuration);
        var store = _storeFactory(configuration);
        await using var runLease = await store.AcquireRunLeaseAsync(cancellationToken);
        using var leaseBoundCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                runLease.LeaseLost);
        var runCancellationToken = leaseBoundCancellation.Token;

        var intents = plan.Intents.ToDictionary(
            intent => (intent.RequestId, intent.SourceId),
            intent => intent,
            IntentIdentityComparer.Ordinal);
        var attempts = new List<SourceAcquisitionAttemptArtifact>(preflight.Entries.Count);
        var haltAutomated = false;

        foreach (var entry in preflight.Entries.OrderBy(item => item.StableOrdinal))
        {
            runCancellationToken.ThrowIfCancellationRequested();
            var intent = intents[(entry.RequestId!, entry.SourceId!)];
            var intentId = ComputeIntentId(
                configuration.CycleId,
                inputBindings,
                intent);

            var existing = await store.TryReadAttemptAsync(
                intentId,
                intent,
                entry,
                inputBindings,
                configuration,
                _timeProvider.GetUtcNow(),
                runCancellationToken);
            if (existing is not null)
            {
                attempts.Add(existing);
                if (existing.Status != "expired")
                {
                    await store.EnsureCheckpointAsync(existing, runCancellationToken);
                }
                if (EffectiveStatus(existing) is
                    "rate-limited" or "backpressure" or "error" or "truncated")
                {
                    haltAutomated = true;
                }
                continue;
            }

            SourceAcquisitionAttemptArtifact attempt;
            if (entry.Classification
                == SourceAcquisitionPreflightClassification.ManualReviewPending)
            {
                attempt = CreateReceipt(
                    configuration,
                    inputBindings,
                    entry,
                    intent,
                    intentId,
                    "manual-review-pending",
                    errorCode: null,
                    errorMessage: null);
            }
            else if (haltAutomated)
            {
                attempt = CreateReceipt(
                    configuration,
                    inputBindings,
                    entry,
                    intent,
                    intentId,
                    "not-attempted",
                    "halted-after-source-failure",
                    "The serial acquisition run halted after a source throttle or error.");
            }
            else
            {
                attempt = await AcquireAsync(
                    intent,
                    entry,
                    intentId,
                    adapters,
                    inputBindings,
                    configuration,
                    runCancellationToken);
                if (attempt.Status is
                    "rate-limited" or "backpressure" or "error" or "truncated")
                {
                    haltAutomated = true;
                }
            }

            await store.WriteAttemptAndCheckpointAsync(attempt, runCancellationToken);
            attempts.Add(attempt);
        }

        var ordered = attempts
            .OrderBy(attempt => attempt.StableOrdinal)
            .ToList();
        var manifest = BuildManifest(configuration.CycleId, inputBindings, preflight, ordered);
        var reviewQueue = BuildReviewQueue(configuration.CycleId, ordered);
        await store.WriteDerivedArtifactsAsync(
            manifest,
            reviewQueue,
            runCancellationToken);
        return new SourceAcquisitionRunResult(manifest, store.Location);
    }

    private static ISourceAcquisitionArtifactStore CreateStore(
        SourceAcquisitionRuntimeConfiguration configuration) =>
            string.Equals(
                configuration.StorageProvider,
                "AzureBlob",
                StringComparison.OrdinalIgnoreCase)
                ? new AzureBlobSourceAcquisitionArtifactStore(configuration)
                : new SourceAcquisitionArtifactStore(configuration);

    private async Task<SourceAcquisitionAttemptArtifact> AcquireAsync(
        SourceAcquisitionIntent intent,
        SourceAcquisitionPreflightEntry entry,
        string intentId,
        IReadOnlyDictionary<string, ISourceAcquisitionAdapter> adapters,
        SourceAcquisitionInputBindings bindings,
        SourceAcquisitionRuntimeConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!adapters.TryGetValue(intent.SourceId, out var adapter))
        {
            return CreateReceipt(
                configuration,
                bindings,
                entry,
                intent,
                intentId,
                "error",
                "adapter-unavailable",
                "The approved acquisition adapter is unavailable.");
        }

        try
        {
            var batch = await adapter.AcquireAsync(
                intent,
                _timeProvider.GetUtcNow(),
                cancellationToken);
            return CreateFromBatch(
                configuration,
                bindings,
                entry,
                intent,
                intentId,
                batch);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SourceAcquisitionException exception)
        {
            return CreateReceipt(
                configuration,
                bindings,
                entry,
                intent,
                intentId,
                "error",
                Sanitize(exception.Code, SourceAcquisitionRuntimeLimits.MaximumErrorCodeLength),
                "The source adapter reported a bounded acquisition error.");
        }
        catch
        {
            return CreateReceipt(
                configuration,
                bindings,
                entry,
                intent,
                intentId,
                "error",
                "unexpected-source-error",
                "The source adapter failed without a safe diagnostic.");
        }
    }

    private SourceAcquisitionAttemptArtifact CreateFromBatch(
        SourceAcquisitionRuntimeConfiguration configuration,
        SourceAcquisitionInputBindings bindings,
        SourceAcquisitionPreflightEntry entry,
        SourceAcquisitionIntent intent,
        string intentId,
        SourceAcquisitionBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        var candidates = NormalizeCandidates(batch.Candidates, intent);
        var status = batch.Status switch
        {
            SourceAcquisitionBatchStatus.Completed when batch.Truncated => "truncated",
            SourceAcquisitionBatchStatus.Completed => "completed",
            SourceAcquisitionBatchStatus.NoMatch => "no-match",
            SourceAcquisitionBatchStatus.RateLimited => "rate-limited",
            SourceAcquisitionBatchStatus.BackPressure => "backpressure",
            _ => throw new SourceAcquisitionException(
                "batch-status-unsupported",
                "The adapter returned an unsupported batch status."),
        };
        if (status is not "completed" and not "truncated"
            && candidates.Count != 0)
        {
            throw new SourceAcquisitionException(
                "batch-candidates-invalid",
                "Only a completed source batch may contain candidates.");
        }
        if (candidates.Count > SourceAcquisitionRuntimeLimits.MaximumCandidatesPerIntent)
        {
            throw new SourceAcquisitionException(
                "candidate-count-exceeded",
                "The source batch exceeded the runtime candidate limit.");
        }

        var completedAt = _timeProvider.GetUtcNow();
        var artifact = new SourceAcquisitionAttemptArtifact(
            SchemaVersion,
            configuration.CycleId,
            intentId,
            entry.StableOrdinal,
            entry.SourceId!,
            entry.AdapterId!,
            entry.RequestId!,
            entry.CompoundName!,
            entry.CandidateMethod!,
            entry.RegistryBindingSha256!,
            bindings,
            intent.SearchTerms.ToList(),
            intent.AuthorizedFieldUses
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList(),
            intent.RequiredProvenanceFields
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList(),
            status,
            completedAt,
                completedAt.AddDays(
                status is "completed" or "truncated"
                    ? configuration.CandidateRetentionDays
                    : configuration.ReceiptRetentionDays),
            batch.Truncated,
            Sanitize(batch.RetryAfter, 256),
            null,
            null,
            null,
            candidates);
        if (JsonSerializer.SerializeToUtf8Bytes(artifact, JsonOptions).Length
            > SourceAcquisitionRuntimeLimits.MaximumAttemptBytes)
        {
            throw new SourceAcquisitionException(
                "attempt-artifact-too-large",
                "The normalized attempt artifact exceeded the runtime size limit.");
        }
        return artifact;
    }

    private SourceAcquisitionAttemptArtifact CreateReceipt(
        SourceAcquisitionRuntimeConfiguration configuration,
        SourceAcquisitionInputBindings bindings,
        SourceAcquisitionPreflightEntry entry,
        SourceAcquisitionIntent intent,
        string intentId,
        string status,
        string? errorCode,
        string? errorMessage)
    {
        var completedAt = _timeProvider.GetUtcNow();
        return new SourceAcquisitionAttemptArtifact(
            SchemaVersion,
            configuration.CycleId,
            intentId,
            entry.StableOrdinal,
            entry.SourceId!,
            entry.AdapterId!,
            entry.RequestId!,
            entry.CompoundName!,
            entry.CandidateMethod!,
            entry.RegistryBindingSha256!,
            bindings,
            intent.SearchTerms.ToList(),
            intent.AuthorizedFieldUses
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList(),
            intent.RequiredProvenanceFields
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList(),
            status,
            completedAt,
            completedAt.AddDays(configuration.ReceiptRetentionDays),
            false,
            null,
            errorCode,
            errorMessage,
            null,
            []);
    }

    internal static IReadOnlyList<SourceAcquisitionCandidate> NormalizeCandidates(
        IReadOnlyList<SourceAcquisitionCandidate> candidates,
        SourceAcquisitionIntent intent)
    {
        if (candidates is null)
        {
            throw new SourceAcquisitionException(
                "batch-candidates-null",
                "The adapter returned a null candidate collection.");
        }

        var seen = new Dictionary<string, SourceAcquisitionCandidate>(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            if (candidate is null)
            {
                throw new SourceAcquisitionException(
                    "candidate-null",
                    "The adapter returned a null candidate.");
            }
            ValidateCandidateForPersistence(candidate, intent);
            var key = $"{candidate.SourceRegistryId}\0{candidate.SourceItemId}";
            if (!seen.TryAdd(key, candidate))
            {
                throw new SourceAcquisitionException(
                    "duplicate-candidate",
                    "The adapter returned a duplicate source candidate identity.");
            }
        }

        return seen.Values
            .OrderBy(candidate => candidate.SourceRegistryId, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.SourceItemId, StringComparer.Ordinal)
            .ToList();
    }

    private static void ValidateCandidateForPersistence(
        SourceAcquisitionCandidate candidate,
        SourceAcquisitionIntent intent)
    {
        var intentUses = intent.AuthorizedFieldUses
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var valid = string.Equals(
                        candidate.RequestId,
                        intent.RequestId,
                        StringComparison.Ordinal)
                    && string.Equals(
                        candidate.CompoundName,
                        intent.CompoundName,
                        StringComparison.Ordinal)
                    && string.Equals(
                        candidate.SourceRegistryId,
                        intent.SourceId,
                        StringComparison.Ordinal)
                    && IsBoundedSubstantive(candidate.SourceItemId)
                    && Uri.TryCreate(candidate.SourceUrl, UriKind.Absolute, out var sourceUri)
                    && sourceUri.Scheme == Uri.UriSchemeHttps
                    && sourceUri.UserInfo.Length == 0
                    && (candidate.QueryUrl is null
                        || Uri.TryCreate(candidate.QueryUrl, UriKind.Absolute, out var queryUri)
                        && queryUri.Scheme == Uri.UriSchemeHttps
                        && queryUri.UserInfo.Length == 0)
                    && string.Equals(
                        candidate.RegistryBindingSha256,
                        intent.RegistryBindingSha256,
                        StringComparison.Ordinal)
                    && candidate.RetrievedAtUtc != default
                    && candidate.RetrievedAtUtc.Offset == TimeSpan.Zero
                    && string.Equals(
                        candidate.RightsReviewStatusAtRetrieval,
                        "reviewed",
                        StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        candidate.HumanReviewStatus,
                        "review-required",
                        StringComparison.OrdinalIgnoreCase)
                    && IsBoundedSubstantive(
                        candidate.SourcePublicationOrUpdateDate)
                    && IsBoundedSubstantive(
                        candidate.TransformationPipelineVersion)
                    && candidate.EvidenceLimitations is { Count: > 0 }
                    && candidate.EvidenceLimitations.All(IsBoundedSubstantive)
                    && candidate.Fields is
                    {
                        Count: > 0 and
                        <= SourceAcquisitionRuntimeLimits.MaximumFieldsPerCandidate
                    }
                    && candidate.Fields.All(pair =>
                        IsBoundedSubstantive(pair.Key)
                        && pair.Value is
                        {
                            Count: > 0 and
                            <= SourceAcquisitionRuntimeLimits.MaximumValuesPerField
                        }
                        && pair.Value.All(IsBoundedSubstantive))
                    && candidate.AuthorizedFieldUses is { Count: > 0 }
                    && candidate.AuthorizedFieldUses.All(use =>
                        IsBoundedSubstantive(use)
                        && intentUses.Contains(use))
                    && candidate.SourceSpecificProvenance is not null
                    && candidate.SourceSpecificProvenance.All(pair =>
                        IsBoundedSubstantive(pair.Key)
                        && IsValidProvenanceValue(pair.Value))
                    && candidate.RightsAttributions is { Count: > 0 }
                    && candidate.RightsAttributions.All(
                        IsValidRightsAttribution)
                    && candidate.DocumentProvenance is not null
                    && candidate.DocumentProvenance.All(
                        IsValidDocumentProvenance)
                    && candidate.ReuseBoundary is not null
                    && IsBoundedSubstantive(
                        candidate.ReuseBoundary.Acknowledgement)
                    && candidate.ReuseBoundary.ExcludedContentClasses
                    is { Count: > 0 }
                    && candidate.ReuseBoundary.ExcludedContentClasses.All(
                        IsBoundedSubstantive)
                    && candidate.ManualCaptureAudit is null;
        if (!valid)
        {
            throw new SourceAcquisitionException(
                "candidate-persistence-invariant-invalid",
                "A normalized candidate violated the durable runtime boundary.");
        }

        var allowedNotProvided = candidate.SourceSpecificProvenance
            .Where(pair => pair.Value.Availability == "not-provided")
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedNotApplicable = candidate.SourceSpecificProvenance
            .Where(pair => pair.Value.Availability == "not-applicable")
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
            candidate,
            intent.RequiredProvenanceFields,
            intent.SourceId,
            intent.RegistryBindingSha256,
            allowedNotProvided,
            allowedNotApplicable);

        if (candidate.QueryUrl is not null
            && (candidate.QueryUrl.Contains("email=", StringComparison.OrdinalIgnoreCase)
                || candidate.QueryUrl.Contains("tool=", StringComparison.OrdinalIgnoreCase)
                || candidate.QueryUrl.Contains("api_key=", StringComparison.OrdinalIgnoreCase)))
        {
            throw new SourceAcquisitionException(
                "candidate-query-contains-runtime-config",
                "A normalized candidate query URL retained runtime-only configuration.");
        }
    }

    private static bool IsValidProvenanceValue(SourceProvenanceValue? value)
    {
        if (value is null || value.Values is null) return false;
        return value.Availability switch
        {
            "present" => value.Values.Count > 0
                         && value.Values.Count
                         <= SourceAcquisitionRuntimeLimits.MaximumValuesPerField
                         && value.Values.All(IsBoundedSubstantive)
                         && string.IsNullOrWhiteSpace(value.UnavailableReason),
            "not-provided" or "not-applicable" =>
                value.Values.Count == 0
                && IsBoundedSubstantive(value.UnavailableReason),
            _ => false,
        };
    }

    private static bool IsValidRightsAttribution(
        SourceRightsAttribution? attribution) =>
        attribution is not null
        && IsBoundedSubstantive(attribution.Scope)
        && IsBoundedSubstantive(attribution.Provider)
        && IsSafeHttpsUri(attribution.SourceUrl)
        && IsSafeHttpsUri(attribution.TermsUrl)
        && IsBoundedSubstantive(attribution.RightsStatus)
        && attribution.CoveredFields is { Count: > 0 }
        && attribution.CoveredFields.All(IsBoundedSubstantive);

    private static bool IsValidDocumentProvenance(
        SourceDocumentProvenance? provenance) =>
        provenance is not null
        && IsBoundedSubstantive(provenance.Title)
        && IsBoundedSubstantive(provenance.Section)
        && (IsBoundedSubstantive(provenance.PublishedDate)
            || IsBoundedSubstantive(provenance.UpdatedDate));

    private static bool IsBoundedSubstantive(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= SourceAcquisitionRuntimeLimits.MaximumTextLength
        && !string.Equals(value.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(value.Trim(), "unknown", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(
            value.Trim(),
            "not provided",
            StringComparison.OrdinalIgnoreCase)
        && !string.Equals(
            value.Trim(),
            "not applicable",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsSafeHttpsUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.UserInfo.Length == 0;

    internal static string ComputeIntentId(
        string cycleId,
        SourceAcquisitionInputBindings bindings,
        SourceAcquisitionIntent intent)
    {
        var canonical = new
        {
            cycleId,
            bindings.ResearchRequestSha256,
            bindings.SourceDecisionSha256,
            bindings.SourceRegistrySha256,
            intent.SourceId,
            intent.AdapterId,
            intent.RequestId,
            intent.CompoundName,
            SearchTerms = intent.SearchTerms.ToArray(),
            intent.CandidateMethod,
            AuthorizedFieldUses = intent.AuthorizedFieldUses
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray(),
            RequiredProvenanceFields = intent.RequiredProvenanceFields
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray(),
            intent.RegistrySchemaVersion,
            intent.RegistryBindingSha256,
            Disposition = (int)intent.Disposition,
            BlockingReasons = intent.BlockingReasons
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray(),
        };
        return Convert.ToHexString(
                SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(canonical)))
            .ToLowerInvariant();
    }

    private static SourceAcquisitionRunManifest BuildManifest(
        string cycleId,
        SourceAcquisitionInputBindings bindings,
        SourceAcquisitionExecutionPreflightResult preflight,
        IReadOnlyList<SourceAcquisitionAttemptArtifact> attempts)
    {
        var errorCount = attempts.Count(
            item => EffectiveStatus(item) == "error");
        var rateLimited = attempts.Count(
            item => EffectiveStatus(item) == "rate-limited");
        var backPressure = attempts.Count(
            item => EffectiveStatus(item) == "backpressure");
        var truncated = attempts.Count(
            item => EffectiveStatus(item) == "truncated");
        var notAttempted = attempts.Count(
            item => EffectiveStatus(item) == "not-attempted");
        var expired = attempts.Count(item => item.Status == "expired");
        return new SourceAcquisitionRunManifest(
            SchemaVersion,
            cycleId,
            bindings,
            preflight.UniqueRequestCount,
            preflight.IntentCount,
            preflight.ReadyCount,
            preflight.BlockedCount,
            preflight.SourceCount,
            attempts.Count(item => EffectiveStatus(item) == "completed"),
            attempts.Count(item => EffectiveStatus(item) == "no-match"),
            rateLimited,
            backPressure,
            truncated,
            errorCount,
            attempts.Count(
                item => EffectiveStatus(item) == "manual-review-pending"),
            notAttempted,
            expired,
            Complete: errorCount == 0
                      && rateLimited == 0
                      && backPressure == 0
                      && truncated == 0
                      && notAttempted == 0,
            attempts.Select(item => item.IntentId).ToList());
    }

    private static SourceAcquisitionReviewQueue BuildReviewQueue(
        string cycleId,
        IReadOnlyList<SourceAcquisitionAttemptArtifact> attempts)
    {
        var reviewable = new HashSet<string>(StringComparer.Ordinal)
        {
            "completed",
            "truncated",
            "no-match",
            "error",
            "manual-review-pending",
        };
        var items = attempts
            .Where(attempt => reviewable.Contains(attempt.Status))
            .Select(attempt => new SourceAcquisitionReviewQueueItem(
                $"source-acquisition-{attempt.IntentId}",
                attempt.IntentId,
                attempt.SourceId,
                attempt.RequestId,
                attempt.CompoundName,
                attempt.Status,
                attempt.Candidates.Count,
                attempt.Status switch
                {
                    "completed" => "Normalized source candidates require evidence review.",
                    "truncated" => "The bounded source result was truncated and requires review.",
                    "no-match" => "The approved source returned no matching candidate.",
                    "manual-review-pending" => "The approved source requires manual acquisition review.",
                    _ => "The source acquisition attempt failed and requires review.",
                },
                [$"intents/{attempt.IntentId}/checkpoint.json"]))
            .OrderBy(item => item.ItemId, StringComparer.Ordinal)
            .ToList();
        return new SourceAcquisitionReviewQueue(SchemaVersion, cycleId, items);
    }

    private static string EffectiveStatus(SourceAcquisitionAttemptArtifact attempt) =>
        attempt.Status == "expired"
            ? attempt.TombstoneOriginalStatus
              ?? throw new InvalidOperationException(
                  "An expired acquisition attempt lost its original status.")
            : attempt.Status;

    private static void ValidateArguments(
        SourceAcquisitionPlan plan,
        SourceAcquisitionExecutionPreflightResult preflight,
        IReadOnlyDictionary<string, ISourceAcquisitionAdapter> adapters,
        SourceAcquisitionInputBindings bindings,
        SourceAcquisitionRuntimeConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(adapters);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(configuration);
        if (!preflight.CanActivate
            || preflight.UniqueRequestCount != 70
            || preflight.IntentCount != 490
            || preflight.ReadyCount != 490
            || preflight.BlockedCount != 0
            || preflight.SourceCount != 7)
        {
            throw new InvalidOperationException(
                "Source acquisition requires the exact 70/490/490/0/7 activation preflight.");
        }
        if (adapters.Count != 6
            || !new[] { "fda", "pubchem", "pubmed", "clinicaltrials", "dailymed", "nih-ods" }
                .All(adapters.ContainsKey)
            || adapters.ContainsKey("nih-nccih"))
        {
            throw new InvalidOperationException(
                "Source acquisition requires exactly the six approved API adapters.");
        }
        if (!IsSha256(bindings.ResearchRequestSha256)
            || !IsSha256(bindings.SourceDecisionSha256)
            || !IsSha256(bindings.SourceRegistrySha256))
        {
            throw new InvalidOperationException("Input bindings must be lowercase SHA-256 values.");
        }
        if (!CycleIdRegex().IsMatch(configuration.CycleId)
            || configuration.CycleId is "." or "..")
        {
            throw new InvalidOperationException(
                "Source acquisition cycle ID must be caller-supplied safe ASCII.");
        }
        if (configuration.CandidateRetentionDays <= 0
            || configuration.ReceiptRetentionDays <= 0
            || configuration.CandidateRetentionDays
            > SourceAcquisitionRuntimeLimits.MaximumRetentionDays
            || configuration.ReceiptRetentionDays
            > SourceAcquisitionRuntimeLimits.MaximumRetentionDays)
        {
            throw new InvalidOperationException(
                "Positive candidate and receipt retention values are required.");
        }
        var fileStore = string.Equals(
            configuration.StorageProvider,
            "File",
            StringComparison.OrdinalIgnoreCase);
        var blobStore = string.Equals(
            configuration.StorageProvider,
            "AzureBlob",
            StringComparison.OrdinalIgnoreCase);
        if (!fileStore && !blobStore)
        {
            throw new InvalidOperationException(
                "Source acquisition storage provider must be File or AzureBlob.");
        }
        if (configuration.IsProduction
            && (!blobStore
                || configuration.CandidateRetentionDays != 30
                || configuration.ReceiptRetentionDays != 30))
        {
            throw new InvalidOperationException(
                "Production source acquisition requires AzureBlob storage and exact 30/30-day retention.");
        }
        if (blobStore)
        {
            AzureBlobSourceAcquisitionArtifactStore.ValidateConfiguration(
                configuration);
        }
    }

    private static string? Sanitize(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var sanitized = new string(value
            .Where(character => !char.IsControl(character))
            .Take(maximumLength)
            .ToArray());
        return sanitized.Length == 0 ? null : sanitized;
    }

    internal static bool IsSha256(string value) =>
        value.Length == 64
        && value.All(character =>
            character is >= '0' and <= '9'
            || character is >= 'a' and <= 'f');

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex CycleIdRegex();

    private sealed class IntentIdentityComparer
        : IEqualityComparer<(string RequestId, string SourceId)>
    {
        public static IntentIdentityComparer Ordinal { get; } = new();

        public bool Equals(
            (string RequestId, string SourceId) x,
            (string RequestId, string SourceId) y) =>
            string.Equals(x.RequestId, y.RequestId, StringComparison.Ordinal)
            && string.Equals(x.SourceId, y.SourceId, StringComparison.Ordinal);

        public int GetHashCode((string RequestId, string SourceId) value) =>
            HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(value.RequestId),
                StringComparer.Ordinal.GetHashCode(value.SourceId));
    }

    private sealed class SourceAcquisitionArtifactStore
        : ISourceAcquisitionArtifactStore
    {
        private readonly string _rootDirectory;

        public SourceAcquisitionArtifactStore(
            SourceAcquisitionRuntimeConfiguration configuration)
        {
            _rootDirectory = ResolveSafeRoot(configuration.ResearchOutputDirectory);
            CycleDirectory = Path.GetFullPath(
                Path.Combine(
                    _rootDirectory,
                    "source-acquisition",
                    "v1",
                    configuration.CycleId));
            RequireContained(_rootDirectory, CycleDirectory);
            Directory.CreateDirectory(CycleDirectory);
            RejectReparsePoints(_rootDirectory, CycleDirectory);
        }

        public string CycleDirectory { get; }

        public string Location => CycleDirectory;

        public Task<ISourceAcquisitionRunLease> AcquireRunLeaseAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(CycleDirectory, "run.lock");
            try
            {
                ISourceAcquisitionRunLease lease = new FileRunLease(
                    new FileStream(
                        path,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite,
                        FileShare.None,
                        1,
                        FileOptions.WriteThrough));
                return Task.FromResult(lease);
            }
            catch (IOException exception)
            {
                throw new InvalidOperationException(
                    "Another source-acquisition runner holds the cycle lock.",
                    exception);
            }
        }

        private sealed class FileRunLease(FileStream stream)
            : ISourceAcquisitionRunLease
        {
            public CancellationToken LeaseLost => CancellationToken.None;

            public ValueTask DisposeAsync() => stream.DisposeAsync();
        }

        public async Task<SourceAcquisitionAttemptArtifact?> TryReadAttemptAsync(
            string intentId,
            SourceAcquisitionIntent intent,
            SourceAcquisitionPreflightEntry entry,
            SourceAcquisitionInputBindings bindings,
            SourceAcquisitionRuntimeConfiguration configuration,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken)
        {
            try
            {
                return await TryReadAttemptCoreAsync(
                    intentId,
                    intent,
                    entry,
                    bindings,
                    configuration,
                    nowUtc,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                try
                {
                    await QuarantineIntentAsync(
                        intentId,
                        nowUtc,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    throw new InvalidOperationException(
                        $"Source acquisition intent '{intentId}' failed integrity validation; quarantine cleanup is incomplete.");
                }
                throw new InvalidOperationException(
                    $"Source acquisition intent '{intentId}' failed integrity validation and was quarantined.");
            }
        }

        private async Task<SourceAcquisitionAttemptArtifact?> TryReadAttemptCoreAsync(
            string intentId,
            SourceAcquisitionIntent intent,
            SourceAcquisitionPreflightEntry entry,
            SourceAcquisitionInputBindings bindings,
            SourceAcquisitionRuntimeConfiguration configuration,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken)
        {
            var priorQuarantine = QuarantineRoot(intentId);
            if (Directory.Exists(priorQuarantine)
                && Directory.EnumerateFiles(
                        priorQuarantine,
                        "quarantine-metadata.json",
                        SearchOption.AllDirectories)
                    .Any())
            {
                throw new InvalidOperationException(
                    "This intent has an unresolved integrity quarantine.");
            }

            var tombstone = await TryReadTombstoneAsync(
                intentId,
                intent,
                entry,
                configuration,
                cancellationToken);
            if (tombstone is not null)
            {
                RemoveExpiredContent(intentId);
                return ToExpiredAttempt(tombstone, intent, bindings);
            }

            var path = FindAttemptPath(intentId);
            if (path is null)
            {
                if (File.Exists(CheckpointPath(intentId)))
                {
                    throw new InvalidOperationException(
                        "A checkpoint exists without its immutable attempt.");
                }
                return null;
            }
            var bytes = await ReadBoundedAsync(
                path,
                SourceAcquisitionRuntimeLimits.MaximumAttemptBytes,
                cancellationToken);
            var pathHash = Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(pathHash, Sha256(bytes), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Source acquisition attempt '{intentId}' failed its content-address check.");
            }
            var attempt = JsonSerializer.Deserialize<SourceAcquisitionAttemptArtifact>(
                bytes,
                JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Source acquisition attempt '{intentId}' is invalid.");
            if (!AttemptMatchesExpected(
                    attempt,
                    intentId,
                    intent,
                    entry,
                    bindings,
                    configuration))
            {
                throw new InvalidOperationException(
                    $"Source acquisition attempt '{intentId}' failed its resume-boundary check.");
            }
            if (nowUtc >= attempt.RetainUntilUtc)
            {
                var removed = await TombstoneAndRemoveAsync(
                    attempt,
                    path,
                    nowUtc,
                    cancellationToken);
                return ToExpiredAttempt(removed, intent, bindings);
            }
            var checkpointPath = CheckpointPath(intentId);
            if (File.Exists(checkpointPath))
            {
                var checkpointBytes = await ReadBoundedAsync(
                    checkpointPath,
                    64 * 1024,
                    cancellationToken);
                var checkpoint = JsonSerializer.Deserialize<SourceAcquisitionCheckpoint>(
                    checkpointBytes,
                    JsonOptions)
                    ?? throw new InvalidOperationException(
                        $"Source acquisition checkpoint '{intentId}' is invalid.");
                if (!string.Equals(checkpoint.IntentId, intentId, StringComparison.Ordinal)
                    || !string.Equals(
                        checkpoint.AttemptSha256,
                        Sha256(bytes),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Source acquisition checkpoint '{intentId}' failed its integrity check.");
                }
            }
            return attempt;
        }

        private async Task QuarantineIntentAsync(
            string intentId,
            DateTimeOffset quarantinedAtUtc,
            CancellationToken cancellationToken)
        {
            var quarantineRoot = QuarantineRoot(intentId);
            PurgeQuarantinePayloads(quarantineRoot);

            var sourceDirectory = IntentDirectory(intentId);
            if (!Directory.Exists(sourceDirectory)) return;
            RejectReparsePoints(_rootDirectory, sourceDirectory);
            var files = Directory.EnumerateFiles(
                    sourceDirectory,
                    "*",
                    SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            if (files.Count == 0) return;

            Directory.CreateDirectory(quarantineRoot);
            RejectReparsePoints(_rootDirectory, quarantineRoot);
            var quarantineDirectory = Path.Combine(
                quarantineRoot,
                $"{quarantinedAtUtc:yyyyMMddHHmmssfffffff}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(quarantineDirectory);
            RejectReparsePoints(_rootDirectory, quarantineDirectory);

            var plannedArtifacts = files
                .Select((source, index) =>
                {
                    var safeName = Regex.Replace(
                        Path.GetFileName(source),
                        "[^A-Za-z0-9._-]",
                        "_",
                        RegexOptions.CultureInvariant);
                    if (safeName.Length == 0) safeName = "artifact";
                    return $"{index:D3}-{safeName}";
                })
                .ToList();

            var metadata = new
            {
                schemaVersion = SchemaVersion,
                intentId,
                quarantinedAtUtc,
                reasonCode = "integrity-validation-failed",
                artifactDisposition = "content-free-evidence-only",
                artifactCount = plannedArtifacts.Count,
                artifacts = plannedArtifacts,
            };
            await WriteImmutableAsync(
                Path.Combine(quarantineDirectory, "quarantine-metadata.json"),
                JsonSerializer.SerializeToUtf8Bytes(metadata, JsonOptions),
                cancellationToken);

            // The immutable marker is flushed first. Each suspect file is then
            // atomically relocated into the contained quarantine and removed.
            // A crash can leave only a quarantined payload, which the next
            // fail-closed resume purges before doing any other work.
            for (var index = 0; index < files.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = Path.Combine(
                    quarantineDirectory,
                    plannedArtifacts[index]);
                File.Move(files[index], destination, overwrite: false);
                File.Delete(destination);
            }
        }

        private void PurgeQuarantinePayloads(string quarantineRoot)
        {
            if (!Directory.Exists(quarantineRoot)) return;
            RejectReparsePoints(_rootDirectory, quarantineRoot);
            foreach (var file in Directory.EnumerateFiles(
                         quarantineRoot,
                         "*",
                         SearchOption.AllDirectories))
            {
                if (string.Equals(
                        Path.GetFileName(file),
                        "quarantine-metadata.json",
                        StringComparison.Ordinal))
                {
                    continue;
                }
                File.Delete(file);
            }
        }

        private string QuarantineRoot(string intentId)
        {
            if (!IsSha256(intentId))
            {
                throw new InvalidOperationException(
                    "Quarantine intent ID must be lowercase SHA-256.");
            }
            var path = Path.GetFullPath(
                Path.Combine(CycleDirectory, "quarantine", intentId));
            RequireContained(CycleDirectory, path);
            return path;
        }

        private async Task<SourceAcquisitionTombstone?> TryReadTombstoneAsync(
            string intentId,
            SourceAcquisitionIntent intent,
            SourceAcquisitionPreflightEntry entry,
            SourceAcquisitionRuntimeConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var path = TombstonePath(intentId);
            if (!File.Exists(path)) return null;
            var bytes = await ReadBoundedAsync(path, 64 * 1024, cancellationToken);
            var tombstone = JsonSerializer.Deserialize<SourceAcquisitionTombstone>(
                bytes,
                JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Source acquisition tombstone '{intentId}' is invalid.");
            var retentionDays = tombstone.OriginalStatus
                is "completed" or "truncated"
                ? configuration.CandidateRetentionDays
                : configuration.ReceiptRetentionDays;
            var terminalStatuses = new HashSet<string>(StringComparer.Ordinal)
            {
                "completed",
                "truncated",
                "no-match",
                "rate-limited",
                "backpressure",
                "error",
                "manual-review-pending",
                "not-attempted",
            };
            var valid = tombstone.SchemaVersion == SchemaVersion
                        && tombstone.CycleId == configuration.CycleId
                        && tombstone.IntentId == intentId
                        && tombstone.StableOrdinal == entry.StableOrdinal
                        && tombstone.SourceId == intent.SourceId
                        && tombstone.RequestId == intent.RequestId
                        && terminalStatuses.Contains(tombstone.OriginalStatus)
                        && IsSha256(tombstone.AttemptSha256)
                        && tombstone.CompletedAtUtc != default
                        && tombstone.CompletedAtUtc.Offset == TimeSpan.Zero
                        && tombstone.RetainUntilUtc
                        == tombstone.CompletedAtUtc.AddDays(retentionDays)
                        && tombstone.RemovedAtUtc >= tombstone.RetainUntilUtc
                        && tombstone.RemovedAtUtc.Offset == TimeSpan.Zero
                        && tombstone.RemovalReason == "retention-expired";
            if (!valid)
            {
                throw new InvalidOperationException(
                    $"Source acquisition tombstone '{intentId}' failed its integrity check.");
            }
            return tombstone;
        }

        private async Task<SourceAcquisitionTombstone> TombstoneAndRemoveAsync(
            SourceAcquisitionAttemptArtifact attempt,
            string attemptPath,
            DateTimeOffset removedAtUtc,
            CancellationToken cancellationToken)
        {
            var attemptBytes = await ReadBoundedAsync(
                attemptPath,
                SourceAcquisitionRuntimeLimits.MaximumAttemptBytes,
                cancellationToken);
            var existingTombstonePath = TombstonePath(attempt.IntentId);
            if (File.Exists(existingTombstonePath))
            {
                var existingBytes = await ReadBoundedAsync(
                    existingTombstonePath,
                    64 * 1024,
                    cancellationToken);
                var existing =
                    JsonSerializer.Deserialize<SourceAcquisitionTombstone>(
                        existingBytes,
                        JsonOptions)
                    ?? throw new InvalidOperationException(
                        "An existing retention tombstone is invalid.");
                if (!TombstoneMatchesAttempt(
                        existing,
                        attempt,
                        Sha256(attemptBytes)))
                {
                    throw new InvalidOperationException(
                        "An existing retention tombstone does not match the immutable attempt.");
                }
                RemoveExpiredContent(attempt.IntentId);
                return existing;
            }
            var tombstone = new SourceAcquisitionTombstone(
                SchemaVersion,
                attempt.CycleId,
                attempt.IntentId,
                attempt.StableOrdinal,
                attempt.SourceId,
                attempt.RequestId,
                attempt.Status,
                Sha256(attemptBytes),
                attempt.CompletedAtUtc,
                attempt.RetainUntilUtc,
                removedAtUtc,
                "retention-expired");
            await WriteImmutableAsync(
                TombstonePath(attempt.IntentId),
                JsonSerializer.SerializeToUtf8Bytes(tombstone, JsonOptions),
                cancellationToken);
            RemoveExpiredContent(attempt.IntentId);
            return tombstone;
        }

        private static bool TombstoneMatchesAttempt(
            SourceAcquisitionTombstone tombstone,
            SourceAcquisitionAttemptArtifact attempt,
            string attemptSha256) =>
            tombstone.SchemaVersion == SchemaVersion
            && tombstone.CycleId == attempt.CycleId
            && tombstone.IntentId == attempt.IntentId
            && tombstone.StableOrdinal == attempt.StableOrdinal
            && tombstone.SourceId == attempt.SourceId
            && tombstone.RequestId == attempt.RequestId
            && tombstone.OriginalStatus == attempt.Status
            && tombstone.AttemptSha256 == attemptSha256
            && tombstone.CompletedAtUtc == attempt.CompletedAtUtc
            && tombstone.RetainUntilUtc == attempt.RetainUntilUtc
            && tombstone.RemovedAtUtc >= attempt.RetainUntilUtc
            && tombstone.RemovedAtUtc.Offset == TimeSpan.Zero
            && tombstone.RemovalReason == "retention-expired";

        private void RemoveExpiredContent(string intentId)
        {
            // Tombstone is written and flushed first. A crash after that point is safe:
            // resume treats the tombstone as terminal and repeats this idempotent cleanup.
            var checkpoint = CheckpointPath(intentId);
            if (File.Exists(checkpoint)) File.Delete(checkpoint);
            var attemptDirectory = AttemptDirectory(intentId);
            if (!Directory.Exists(attemptDirectory)) return;
            foreach (var file in Directory.EnumerateFiles(
                         attemptDirectory,
                         "*.json",
                         SearchOption.TopDirectoryOnly))
            {
                File.Delete(file);
            }
        }

        private static SourceAcquisitionAttemptArtifact ToExpiredAttempt(
            SourceAcquisitionTombstone tombstone,
            SourceAcquisitionIntent intent,
            SourceAcquisitionInputBindings bindings) =>
            new(
                SchemaVersion,
                tombstone.CycleId,
                tombstone.IntentId,
                tombstone.StableOrdinal,
                tombstone.SourceId,
                intent.AdapterId,
                tombstone.RequestId,
                intent.CompoundName,
                intent.CandidateMethod,
                intent.RegistryBindingSha256,
                bindings,
                intent.SearchTerms.ToList(),
                intent.AuthorizedFieldUses
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToList(),
                intent.RequiredProvenanceFields
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToList(),
                "expired",
                tombstone.CompletedAtUtc,
                tombstone.RetainUntilUtc,
                false,
                null,
                null,
                null,
                tombstone.OriginalStatus,
                []);

        public async Task WriteAttemptAndCheckpointAsync(
            SourceAcquisitionAttemptArtifact attempt,
            CancellationToken cancellationToken)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(attempt, JsonOptions);
            if (bytes.Length > SourceAcquisitionRuntimeLimits.MaximumAttemptBytes)
            {
                throw new SourceAcquisitionException(
                    "attempt-artifact-too-large",
                    "The normalized attempt artifact exceeded the runtime size limit.");
            }
            var attemptPath = AttemptPath(attempt.IntentId, Sha256(bytes));
            await WriteImmutableAsync(attemptPath, bytes, cancellationToken);
            await EnsureCheckpointAsync(attempt, cancellationToken);
        }

        public async Task EnsureCheckpointAsync(
            SourceAcquisitionAttemptArtifact attempt,
            CancellationToken cancellationToken)
        {
            var attemptPath = FindAttemptPath(attempt.IntentId)
                              ?? throw new InvalidOperationException(
                                  "The immutable attempt is missing.");
            var bytes = await ReadBoundedAsync(
                attemptPath,
                SourceAcquisitionRuntimeLimits.MaximumAttemptBytes,
                cancellationToken);
            var checkpoint = new SourceAcquisitionCheckpoint(
                SchemaVersion,
                attempt.CycleId,
                attempt.IntentId,
                attempt.Status,
                Path.GetRelativePath(CycleDirectory, attemptPath)
                    .Replace('\\', '/'),
                Sha256(bytes),
                attempt.CompletedAtUtc);
            await WriteAtomicAsync(
                CheckpointPath(attempt.IntentId),
                JsonSerializer.SerializeToUtf8Bytes(checkpoint, JsonOptions),
                cancellationToken);
        }

        public async Task WriteDerivedArtifactsAsync(
            SourceAcquisitionRunManifest manifest,
            SourceAcquisitionReviewQueue reviewQueue,
            CancellationToken cancellationToken)
        {
            await WriteAtomicAsync(
                Path.Combine(CycleDirectory, "run-manifest.json"),
                JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions),
                cancellationToken);
            await WriteAtomicAsync(
                Path.Combine(CycleDirectory, "source-acquisition-review-queue.json"),
                JsonSerializer.SerializeToUtf8Bytes(reviewQueue, JsonOptions),
                cancellationToken);
        }

        private string AttemptPath(string intentId, string contentSha256)
        {
            if (!IsSha256(contentSha256))
            {
                throw new InvalidOperationException(
                    "Attempt content hash must be lowercase SHA-256.");
            }
            var directory = AttemptDirectory(intentId);
            Directory.CreateDirectory(directory);
            RejectReparsePoints(_rootDirectory, directory);
            return Path.Combine(directory, $"{contentSha256}.json");
        }

        private string? FindAttemptPath(string intentId)
        {
            var directory = AttemptDirectory(intentId);
            if (!Directory.Exists(directory)) return null;
            RejectReparsePoints(_rootDirectory, directory);
            var files = Directory.EnumerateFiles(
                    directory,
                    "*.json",
                    SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            return files.Count switch
            {
                0 => null,
                1 => files[0],
                _ => throw new InvalidOperationException(
                    $"Intent '{intentId}' has multiple immutable attempts in one cycle."),
            };
        }

        private string AttemptDirectory(string intentId)
        {
            var path = Path.Combine(IntentDirectory(intentId), "attempts");
            RequireContained(CycleDirectory, path);
            return path;
        }

        private string CheckpointPath(string intentId) =>
            Path.Combine(IntentDirectory(intentId), "checkpoint.json");

        private string TombstonePath(string intentId) =>
            Path.Combine(IntentDirectory(intentId), "tombstone.json");

        private string IntentDirectory(string intentId)
        {
            if (!IsSha256(intentId))
            {
                throw new InvalidOperationException("Intent ID must be a lowercase SHA-256 value.");
            }
            var path = Path.GetFullPath(Path.Combine(CycleDirectory, "intents", intentId));
            RequireContained(CycleDirectory, path);
            return path;
        }

        private static async Task WriteImmutableAsync(
            string path,
            byte[] bytes,
            CancellationToken cancellationToken)
        {
            if (File.Exists(path))
            {
                var existing = await ReadBoundedAsync(
                    path,
                    SourceAcquisitionRuntimeLimits.MaximumAttemptBytes,
                    cancellationToken);
                if (existing.AsSpan().SequenceEqual(bytes)) return;
                throw new InvalidOperationException(
                    "An immutable source-acquisition artifact already exists with different content.");
            }
            await WriteTempAndMoveAsync(path, bytes, replace: false, cancellationToken);
        }

        private static Task WriteAtomicAsync(
            string path,
            byte[] bytes,
            CancellationToken cancellationToken) =>
            WriteTempAndMoveAsync(path, bytes, replace: true, cancellationToken);

        private static async Task WriteTempAndMoveAsync(
            string path,
            byte[] bytes,
            bool replace,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tempPath = Path.Combine(
                Path.GetDirectoryName(path)!,
                $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            try
            {
                await using (var stream = new FileStream(
                                 tempPath,
                                 FileMode.CreateNew,
                                 FileAccess.Write,
                                 FileShare.None,
                                 16 * 1024,
                                 FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await stream.WriteAsync(bytes, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                    stream.Flush(flushToDisk: true);
                }

                if (replace && File.Exists(path))
                {
                    File.Replace(tempPath, path, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(tempPath, path, overwrite: false);
                }
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        private static async Task<byte[]> ReadBoundedAsync(
            string path,
            int maximumBytes,
            CancellationToken cancellationToken)
        {
            var info = new FileInfo(path);
            if (info.Length > maximumBytes)
            {
                throw new InvalidOperationException(
                    $"Artifact '{Path.GetFileName(path)}' exceeds its size limit.");
            }
            return await File.ReadAllBytesAsync(path, cancellationToken);
        }

        private static string ResolveSafeRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException(
                    "ResearchOutputDirectory is required for source acquisition.");
            }
            var fullPath = Path.GetFullPath(
                Path.IsPathRooted(path)
                    ? path
                    : Path.Combine(AppContext.BaseDirectory, path));
            if (!Directory.Exists(fullPath))
            {
                throw new InvalidOperationException(
                    "Configured ResearchOutputDirectory must already exist.");
            }
            if (new DirectoryInfo(fullPath).Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException(
                    "ResearchOutputDirectory cannot be a reparse point.");
            }
            return fullPath;
        }

        private static void RequireContained(string root, string path)
        {
            var prefix = root.TrimEnd(
                             Path.DirectorySeparatorChar,
                             Path.AltDirectorySeparatorChar)
                         + Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Source-acquisition output escaped ResearchOutputDirectory.");
            }
        }

        private static void RejectReparsePoints(string root, string path)
        {
            RequireContained(root, path);
            var current = new DirectoryInfo(path);
            var stop = Path.GetFullPath(root);
            while (current is not null)
            {
                if (current.Exists
                    && current.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new InvalidOperationException(
                        "Source-acquisition output cannot traverse a reparse point.");
                }
                if (string.Equals(
                        current.FullName,
                        stop,
                        StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                current = current.Parent;
            }
        }

        private static string Sha256(byte[] bytes) =>
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        private static bool AttemptMatchesExpected(
            SourceAcquisitionAttemptArtifact attempt,
            string intentId,
            SourceAcquisitionIntent intent,
            SourceAcquisitionPreflightEntry entry,
            SourceAcquisitionInputBindings bindings,
            SourceAcquisitionRuntimeConfiguration configuration)
        {
            var candidates = attempt.Candidates;
            if (candidates is null) return false;
            var allowedStatuses = new HashSet<string>(StringComparer.Ordinal)
            {
                "completed",
                "truncated",
                "no-match",
                "rate-limited",
                "backpressure",
                "error",
                "manual-review-pending",
                "not-attempted",
            };
            var basic = attempt.SchemaVersion == SchemaVersion
                        && attempt.CycleId == configuration.CycleId
                        && attempt.IntentId == intentId
                        && attempt.StableOrdinal == entry.StableOrdinal
                        && attempt.SourceId == intent.SourceId
                        && attempt.AdapterId == intent.AdapterId
                        && attempt.RequestId == intent.RequestId
                        && attempt.CompoundName == intent.CompoundName
                        && attempt.CandidateMethod == intent.CandidateMethod
                        && attempt.RegistryBindingSha256
                        == intent.RegistryBindingSha256
                        && attempt.InputBindings == bindings
                        && attempt.SearchTerms.SequenceEqual(intent.SearchTerms)
                        && attempt.AuthorizedFieldUses.SequenceEqual(
                            intent.AuthorizedFieldUses.OrderBy(
                                value => value,
                                StringComparer.Ordinal))
                        && attempt.RequiredProvenanceFields.SequenceEqual(
                            intent.RequiredProvenanceFields.OrderBy(
                                value => value,
                                StringComparer.Ordinal))
                        && allowedStatuses.Contains(attempt.Status)
                        && attempt.CompletedAtUtc != default
                        && attempt.CompletedAtUtc.Offset == TimeSpan.Zero
                        && attempt.RetainUntilUtc
                        == attempt.CompletedAtUtc.AddDays(
                            attempt.Status is "completed" or "truncated"
                                ? configuration.CandidateRetentionDays
                                : configuration.ReceiptRetentionDays)
                        && attempt.RetainUntilUtc.Offset == TimeSpan.Zero;
            basic = basic && attempt.TombstoneOriginalStatus is null;
            if (!basic) return false;

            if (intent.CandidateMethod == "manual-review")
            {
                return attempt.Status == "manual-review-pending"
                       && candidates.Count == 0;
            }
            if (attempt.Status is not "completed" and not "truncated"
                && candidates.Count != 0)
            {
                return false;
            }
            if (candidates.Count
                > SourceAcquisitionRuntimeLimits.MaximumCandidatesPerIntent)
            {
                return false;
            }

            try
            {
                var normalized = NormalizeCandidates(candidates, intent);
                return normalized.Select(candidate =>
                           (candidate.SourceRegistryId, candidate.SourceItemId))
                    .SequenceEqual(candidates.Select(candidate =>
                        (candidate.SourceRegistryId, candidate.SourceItemId)));
            }
            catch (SourceAcquisitionException)
            {
                return false;
            }
        }
    }
}
