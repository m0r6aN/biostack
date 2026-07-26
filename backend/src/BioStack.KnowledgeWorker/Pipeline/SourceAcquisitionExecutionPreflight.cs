namespace BioStack.KnowledgeWorker.Pipeline;

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

public enum SourceAcquisitionPreflightClassification
{
    Blocked = 0,
    ManualReviewPending = 1,
    ReadyAutomated = 2,
    UnsupportedOrMismatched = 3,
}

public sealed record SourceAcquisitionAdapterDescriptor(
    string SourceId,
    string AdapterId,
    string CandidateMethod);

public sealed record SourceAcquisitionCampaignExpectation(
    int UniqueRequestCount,
    int IntentCount,
    int ReadyCount,
    int BlockedCount,
    int SourceCount)
{
    public static SourceAcquisitionCampaignExpectation CurrentRecommendedSevenActivation { get; } =
        new(
            UniqueRequestCount: 70,
            IntentCount: 490,
            ReadyCount: 490,
            BlockedCount: 0,
            SourceCount: 7);
}

public sealed record SourceAcquisitionPreflightIssue(
    string Code,
    string Detail);

public sealed record SourceAcquisitionPreflightEntry(
    int StableOrdinal,
    string? SourceId,
    string? AdapterId,
    string? RequestId,
    string? CompoundName,
    string? CandidateMethod,
    string? RegistryBindingSha256,
    SourceAcquisitionDisposition PlanDisposition,
    SourceAcquisitionPreflightClassification Classification,
    IReadOnlyList<string?> BlockingReasons,
    IReadOnlyList<SourceAcquisitionPreflightIssue> Issues)
{
    public bool IsDispatchable =>
        Classification == SourceAcquisitionPreflightClassification.ReadyAutomated;
}

public sealed record SourceAcquisitionExecutionPreflightResult(
    IReadOnlyList<SourceAcquisitionPreflightEntry> Entries,
    IReadOnlyList<SourceAcquisitionPreflightIssue> Issues,
    int UniqueRequestCount,
    int IntentCount,
    int ReadyCount,
    int BlockedCount,
    int SourceCount,
    int ReadyAutomatedCount,
    int ManualReviewPendingCount,
    int DispatchableCount,
    bool CanActivate);

public interface ISourceAcquisitionExecutionPreflight
{
    SourceAcquisitionExecutionPreflightResult Evaluate(
        SourceAcquisitionPlan plan,
        IEnumerable<SourceAcquisitionAdapterDescriptor> availableAdapters,
        SourceAcquisitionCampaignExpectation? expectation = null);
}

public sealed partial class SourceAcquisitionExecutionPreflight
    : ISourceAcquisitionExecutionPreflight
{
    private const string ApiMethod = "api";
    private const string ManualReviewMethod = "manual-review";

    private static readonly IReadOnlyDictionary<string, ApprovedSourceContract>
        ApprovedSources =
        new ReadOnlyDictionary<string, ApprovedSourceContract>(
            new Dictionary<string, ApprovedSourceContract>(
                StringComparer.Ordinal)
            {
                ["fda"] = new("fda-planning-v1", ApiMethod, 0),
                ["pubchem"] = new("pubchem-planning-v1", ApiMethod, 1),
                ["pubmed"] = new("pubmed-planning-v1", ApiMethod, 2),
                ["clinicaltrials"] =
                    new("clinicaltrials-planning-v1", ApiMethod, 3),
                ["dailymed"] = new("dailymed-planning-v1", ApiMethod, 4),
                ["nih-ods"] = new("nih-ods-planning-v1", ApiMethod, 5),
                ["nih-nccih"] =
                    new("nih-nccih-planning-v1", ManualReviewMethod, 6),
            });

    public SourceAcquisitionExecutionPreflightResult Evaluate(
        SourceAcquisitionPlan plan,
        IEnumerable<SourceAcquisitionAdapterDescriptor> availableAdapters,
        SourceAcquisitionCampaignExpectation? expectation = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(availableAdapters);

        var globalIssues = new List<SourceAcquisitionPreflightIssue>();
        ValidateExpectation(expectation, globalIssues);

        var descriptors = BuildDescriptorIndex(availableAdapters, globalIssues);
        IReadOnlyList<SourceAcquisitionIntent?> intents;
        if (plan.Intents is null)
        {
            globalIssues.Add(new SourceAcquisitionPreflightIssue(
                "plan-intents-null",
                "The acquisition plan must provide an intent collection."));
            intents = [];
        }
        else
        {
            intents = plan.Intents
                .Select(intent => (SourceAcquisitionIntent?)intent)
                .ToList();
        }

        ValidatePlanCounts(plan, intents, globalIssues);
        ValidateIntentIdentities(intents, globalIssues);
        ValidateExactSourceCoverage(intents, descriptors, globalIssues);

        var registryHashes = intents
            .Where(intent => intent is not null)
            .Select(intent => intent!.RegistryBindingSha256)
            .Where(IsLowercaseSha256)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (registryHashes.Count > 1)
        {
            globalIssues.Add(new SourceAcquisitionPreflightIssue(
                "registry-binding-hash-mismatch",
                "All intents in one execution plan must carry the same registry binding SHA-256."));
        }

        var orderedIntents = intents
            .Select((intent, originalIndex) => (intent, originalIndex))
            .OrderBy(item => item.intent?.RequestId, StringComparer.Ordinal)
            .ThenBy(item => SourceOrdinal(item.intent?.SourceId))
            .ThenBy(item => item.intent?.SourceId, StringComparer.Ordinal)
            .ThenBy(item => item.intent?.CompoundName, StringComparer.Ordinal)
            .ThenBy(item => item.intent?.AdapterId, StringComparer.Ordinal)
            .ThenBy(item => item.intent?.CandidateMethod, StringComparer.Ordinal)
            .ThenBy(
                item => item.intent?.RegistryBindingSha256,
                StringComparer.Ordinal)
            .ThenBy(item => item.originalIndex)
            .Select(item => item.intent)
            .ToList();

        var entries = orderedIntents
            .Select((intent, index) => BuildEntry(intent, index + 1, descriptors))
            .ToList();
        globalIssues.AddRange(entries.SelectMany(entry => entry.Issues));

        var uniqueRequestCount = intents
            .Where(intent => !string.IsNullOrWhiteSpace(intent?.RequestId))
            .Select(intent => intent!.RequestId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var sourceCount = intents
            .Where(intent => !string.IsNullOrWhiteSpace(intent?.SourceId))
            .Select(intent => intent!.SourceId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var readyCount = intents.Count(
            intent => intent?.Disposition == SourceAcquisitionDisposition.Ready);
        var blockedCount = intents.Count(
            intent => intent?.Disposition == SourceAcquisitionDisposition.Blocked);
        var readyAutomatedCount = entries.Count(
            entry => entry.Classification
                     == SourceAcquisitionPreflightClassification.ReadyAutomated);
        var manualReviewPendingCount = entries.Count(
            entry => entry.Classification
                     == SourceAcquisitionPreflightClassification.ManualReviewPending);

        ValidateCampaignExpectation(
            expectation,
            uniqueRequestCount,
            intents.Count,
            readyCount,
            blockedCount,
            sourceCount,
            globalIssues);

        var sortedIssues = ReadOnly(globalIssues
            .Distinct()
            .OrderBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.Detail, StringComparer.Ordinal));

        return new SourceAcquisitionExecutionPreflightResult(
            Entries: ReadOnly(entries),
            Issues: sortedIssues,
            UniqueRequestCount: uniqueRequestCount,
            IntentCount: intents.Count,
            ReadyCount: readyCount,
            BlockedCount: blockedCount,
            SourceCount: sourceCount,
            ReadyAutomatedCount: readyAutomatedCount,
            ManualReviewPendingCount: manualReviewPendingCount,
            DispatchableCount: readyAutomatedCount,
            CanActivate: sortedIssues.Count == 0
                         && blockedCount == 0
                         && entries.All(entry =>
                             entry.Classification
                             != SourceAcquisitionPreflightClassification
                                 .UnsupportedOrMismatched));
    }

    private static IReadOnlyDictionary<string, SourceAcquisitionAdapterDescriptor>
        BuildDescriptorIndex(
            IEnumerable<SourceAcquisitionAdapterDescriptor> availableAdapters,
            ICollection<SourceAcquisitionPreflightIssue> issues)
    {
        var descriptors = new Dictionary<string, SourceAcquisitionAdapterDescriptor>(
            StringComparer.Ordinal);
        foreach (var descriptor in availableAdapters)
        {
            if (descriptor is null)
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "adapter-descriptor-null",
                    "Available adapter descriptors cannot contain null."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(descriptor.SourceId))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "adapter-descriptor-source-id-required",
                    "Every adapter descriptor must declare a nonblank source ID."));
                continue;
            }

            if (!ApprovedSources.TryGetValue(
                    descriptor.SourceId,
                    out var approvedSource))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "adapter-descriptor-source-unknown",
                    $"Adapter descriptor source '{descriptor.SourceId}' is not selected."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(descriptor.AdapterId))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "adapter-descriptor-adapter-id-required",
                    $"Source '{descriptor.SourceId}' must declare a nonblank adapter ID."));
            }
            if (!string.Equals(
                    descriptor.AdapterId,
                    approvedSource.PlanningAdapterId,
                    StringComparison.Ordinal))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "adapter-descriptor-approved-adapter-id-mismatch",
                    $"Source '{descriptor.SourceId}' must use approved planning adapter "
                    + $"'{approvedSource.PlanningAdapterId}'."));
            }

            if (string.IsNullOrWhiteSpace(descriptor.CandidateMethod))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "adapter-descriptor-candidate-method-required",
                    $"Source '{descriptor.SourceId}' must declare a nonblank candidate method."));
            }
            if (!string.Equals(
                    descriptor.CandidateMethod,
                    approvedSource.CandidateMethod,
                    StringComparison.Ordinal))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "adapter-descriptor-approved-method-mismatch",
                    $"Source '{descriptor.SourceId}' must use approved candidate method "
                    + $"'{approvedSource.CandidateMethod}'."));
            }

            if (!descriptors.TryAdd(descriptor.SourceId, descriptor))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "adapter-descriptor-duplicate",
                    $"Source '{descriptor.SourceId}' has more than one adapter descriptor."));
            }
        }

        foreach (var sourceId in ApprovedSources.Keys)
        {
            if (!descriptors.ContainsKey(sourceId))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "adapter-descriptor-missing",
                    $"Source '{sourceId}' does not have an available adapter descriptor."));
            }
        }

        return new ReadOnlyDictionary<string, SourceAcquisitionAdapterDescriptor>(
            descriptors);
    }

    private static void ValidatePlanCounts(
        SourceAcquisitionPlan plan,
        IReadOnlyCollection<SourceAcquisitionIntent?> intents,
        ICollection<SourceAcquisitionPreflightIssue> issues)
    {
        var actualReady = intents.Count(
            intent => intent?.Disposition == SourceAcquisitionDisposition.Ready);
        var actualBlocked = intents.Count(
            intent => intent?.Disposition == SourceAcquisitionDisposition.Blocked);
        var recognized = actualReady + actualBlocked;

        if (plan.ReadyCount != actualReady
            || plan.BlockedCount != actualBlocked
            || recognized != intents.Count
            || plan.ReadyCount + plan.BlockedCount != intents.Count)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "plan-counts-contradictory",
                $"Declared ready/blocked counts {plan.ReadyCount}/{plan.BlockedCount} "
                + $"do not match the {actualReady}/{actualBlocked} recognized intents."));
        }
    }

    private static void ValidateIntentIdentities(
        IReadOnlyCollection<SourceAcquisitionIntent?> intents,
        ICollection<SourceAcquisitionPreflightIssue> issues)
    {
        foreach (var duplicate in intents
                     .Where(intent =>
                         !string.IsNullOrWhiteSpace(intent?.RequestId)
                         && !string.IsNullOrWhiteSpace(intent.SourceId))
                     .GroupBy(
                         intent => (intent!.RequestId, intent.SourceId),
                         StringTupleComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-identity-duplicate",
                $"Request '{duplicate.Key.RequestId}' has multiple intents for "
                + $"source '{duplicate.Key.SourceId}'."));
        }
    }

    private static void ValidateExactSourceCoverage(
        IReadOnlyCollection<SourceAcquisitionIntent?> intents,
        IReadOnlyDictionary<string, SourceAcquisitionAdapterDescriptor> descriptors,
        ICollection<SourceAcquisitionPreflightIssue> issues)
    {
        var selected = ApprovedSources.Keys
            .ToHashSet(StringComparer.Ordinal);
        var intentSources = intents
            .Where(intent => !string.IsNullOrWhiteSpace(intent?.SourceId))
            .Select(intent => intent!.SourceId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var unknown in intentSources.Except(selected, StringComparer.Ordinal))
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-source-unknown",
                $"Intent source '{unknown}' is not one of the selected seven sources."));
        }

        foreach (var missing in selected.Except(intentSources, StringComparer.Ordinal))
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-source-missing",
                $"Selected source '{missing}' has no intent."));
        }

        foreach (var requestGroup in intents
                     .Where(intent =>
                         !string.IsNullOrWhiteSpace(intent?.RequestId))
                     .GroupBy(
                         intent => intent!.RequestId,
                         StringComparer.Ordinal))
        {
            var requestSources = requestGroup
                .Where(intent => !string.IsNullOrWhiteSpace(intent?.SourceId))
                .Select(intent => intent!.SourceId)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var missing in selected.Except(requestSources, StringComparer.Ordinal))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "request-source-missing",
                    $"Request '{requestGroup.Key}' has no intent for source '{missing}'."));
            }
        }

        if (descriptors.Count != selected.Count)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "adapter-descriptor-source-set-mismatch",
                "Available adapter descriptors must cover exactly the selected seven sources."));
        }
    }

    private static SourceAcquisitionPreflightEntry BuildEntry(
        SourceAcquisitionIntent? intent,
        int stableOrdinal,
        IReadOnlyDictionary<string, SourceAcquisitionAdapterDescriptor> descriptors)
    {
        var issues = new List<SourceAcquisitionPreflightIssue>();
        if (intent is null)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-null",
                $"Intent at stable ordinal {stableOrdinal} is null."));
            return new SourceAcquisitionPreflightEntry(
                StableOrdinal: stableOrdinal,
                SourceId: null,
                AdapterId: null,
                RequestId: null,
                CompoundName: null,
                CandidateMethod: null,
                RegistryBindingSha256: null,
                PlanDisposition: default,
                Classification:
                    SourceAcquisitionPreflightClassification.UnsupportedOrMismatched,
                BlockingReasons: ReadOnly<string?>([]),
                Issues: ReadOnly(issues));
        }

        var sourceIdPresent = !string.IsNullOrWhiteSpace(intent.SourceId);
        if (!sourceIdPresent)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-source-id-required",
                $"Intent at stable ordinal {stableOrdinal} must declare a nonblank source ID."));
        }

        ApprovedSources.TryGetValue(
            sourceIdPresent ? intent.SourceId : string.Empty,
            out var approvedSource);
        if (sourceIdPresent && approvedSource is null)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-source-unknown",
                $"Intent source '{intent.SourceId}' is not one of the selected seven sources."));
        }

        if (string.IsNullOrWhiteSpace(intent.RequestId))
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-request-id-required",
                $"Source '{intent.SourceId}' must declare a nonblank request ID."));
        }
        if (string.IsNullOrWhiteSpace(intent.CompoundName))
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-compound-name-required",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' must declare "
                + "a nonblank compound name."));
        }
        if (string.IsNullOrWhiteSpace(intent.AdapterId))
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-adapter-id-required",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' must declare "
                + "a nonblank adapter ID."));
        }
        if (string.IsNullOrWhiteSpace(intent.CandidateMethod))
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-candidate-method-required",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' must declare "
                + "a nonblank candidate method."));
        }
        if (string.IsNullOrWhiteSpace(intent.RegistryBindingSha256))
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "registry-binding-hash-required",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' must declare "
                + "a registry binding SHA-256."));
        }
        else if (!IsLowercaseSha256(intent.RegistryBindingSha256))
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "registry-binding-hash-invalid",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' has an invalid "
                + "registry binding SHA-256."));
        }

        if (approvedSource is not null)
        {
            if (!string.Equals(
                    intent.AdapterId,
                    approvedSource.PlanningAdapterId,
                    StringComparison.Ordinal))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "intent-approved-adapter-id-mismatch",
                    $"Request '{intent.RequestId}' source '{intent.SourceId}' must use "
                    + $"approved planning adapter '{approvedSource.PlanningAdapterId}'."));
            }
            if (!string.Equals(
                    intent.CandidateMethod,
                    approvedSource.CandidateMethod,
                    StringComparison.Ordinal))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "intent-approved-method-mismatch",
                    $"Request '{intent.RequestId}' source '{intent.SourceId}' must use "
                    + $"approved candidate method '{approvedSource.CandidateMethod}'."));
            }
        }

        SourceAcquisitionAdapterDescriptor? descriptor = null;
        if (sourceIdPresent)
        {
            descriptors.TryGetValue(intent.SourceId, out descriptor);
        }
        if (descriptor is null)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-adapter-unavailable",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' has no "
                + "available adapter descriptor."));
        }
        else
        {
            if (!string.Equals(
                    descriptor.AdapterId,
                    intent.AdapterId,
                    StringComparison.Ordinal))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "intent-adapter-id-mismatch",
                    $"Request '{intent.RequestId}' source '{intent.SourceId}' planned "
                    + $"adapter '{intent.AdapterId}' but descriptor declares "
                    + $"'{descriptor.AdapterId}'."));
            }

            if (!string.Equals(
                    descriptor.CandidateMethod,
                    intent.CandidateMethod,
                    StringComparison.Ordinal))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "intent-candidate-method-mismatch",
                    $"Request '{intent.RequestId}' source '{intent.SourceId}' planned "
                    + $"method '{intent.CandidateMethod}' but descriptor declares "
                    + $"'{descriptor.CandidateMethod}'."));
            }
        }

        IReadOnlyList<string?> blockingReasons;
        if (intent.BlockingReasons is null)
        {
            blockingReasons = [];
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-blocking-reasons-null",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' must provide "
                + "a blocking-reasons collection."));
        }
        else
        {
            blockingReasons = intent.BlockingReasons
                .Select(reason => (string?)reason)
                .ToList();
            if (blockingReasons.Any(string.IsNullOrWhiteSpace))
            {
                issues.Add(new SourceAcquisitionPreflightIssue(
                    "intent-blocking-reason-required",
                    $"Request '{intent.RequestId}' source '{intent.SourceId}' has a "
                    + "null or blank blocking reason."));
            }
        }

        if (intent.Disposition == SourceAcquisitionDisposition.Ready
            && blockingReasons.Count != 0)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "ready-intent-has-blockers",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' is ready "
                + "but declares blocking reasons."));
        }
        else if (intent.Disposition == SourceAcquisitionDisposition.Blocked
                 && blockingReasons.Count == 0)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "blocked-intent-has-no-blockers",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' is blocked "
                + "but declares no blocking reason."));
        }

        if (intent.Disposition is not SourceAcquisitionDisposition.Ready
            and not SourceAcquisitionDisposition.Blocked)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-disposition-unsupported",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' has an "
                + "unsupported plan disposition."));
        }

        if (!string.IsNullOrWhiteSpace(intent.CandidateMethod)
            && intent.CandidateMethod is not ApiMethod and not ManualReviewMethod)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "intent-candidate-method-unsupported",
                $"Request '{intent.RequestId}' source '{intent.SourceId}' uses unsupported "
                + $"candidate method '{intent.CandidateMethod}'."));
        }

        var classification = issues.Count > 0
            ? SourceAcquisitionPreflightClassification.UnsupportedOrMismatched
            : intent.Disposition switch
            {
                SourceAcquisitionDisposition.Blocked =>
                    SourceAcquisitionPreflightClassification.Blocked,
                SourceAcquisitionDisposition.Ready
                    when intent.CandidateMethod == ManualReviewMethod =>
                    SourceAcquisitionPreflightClassification.ManualReviewPending,
                SourceAcquisitionDisposition.Ready
                    when intent.CandidateMethod == ApiMethod =>
                    SourceAcquisitionPreflightClassification.ReadyAutomated,
                _ => SourceAcquisitionPreflightClassification.UnsupportedOrMismatched,
            };

        return new SourceAcquisitionPreflightEntry(
            StableOrdinal: stableOrdinal,
            SourceId: intent.SourceId,
            AdapterId: intent.AdapterId,
            RequestId: intent.RequestId,
            CompoundName: intent.CompoundName,
            CandidateMethod: intent.CandidateMethod,
            RegistryBindingSha256: intent.RegistryBindingSha256,
            PlanDisposition: intent.Disposition,
            Classification: classification,
            BlockingReasons: ReadOnly(blockingReasons),
            Issues: ReadOnly(issues
                .OrderBy(issue => issue.Code, StringComparer.Ordinal)
                .ThenBy(issue => issue.Detail, StringComparer.Ordinal)));
    }

    private static void ValidateExpectation(
        SourceAcquisitionCampaignExpectation? expectation,
        ICollection<SourceAcquisitionPreflightIssue> issues)
    {
        if (expectation is null)
        {
            return;
        }

        if (expectation.UniqueRequestCount < 0
            || expectation.IntentCount < 0
            || expectation.ReadyCount < 0
            || expectation.BlockedCount < 0
            || expectation.SourceCount < 0
            || expectation.ReadyCount + expectation.BlockedCount
            != expectation.IntentCount)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "campaign-expectation-invalid",
                "Campaign expectation counts must be nonnegative and internally consistent."));
        }
    }

    private static void ValidateCampaignExpectation(
        SourceAcquisitionCampaignExpectation? expectation,
        int uniqueRequestCount,
        int intentCount,
        int readyCount,
        int blockedCount,
        int sourceCount,
        ICollection<SourceAcquisitionPreflightIssue> issues)
    {
        if (expectation is null)
        {
            return;
        }

        AddExpectationIssue(
            "unique-request-count",
            expectation.UniqueRequestCount,
            uniqueRequestCount,
            issues);
        AddExpectationIssue(
            "intent-count",
            expectation.IntentCount,
            intentCount,
            issues);
        AddExpectationIssue(
            "ready-count",
            expectation.ReadyCount,
            readyCount,
            issues);
        AddExpectationIssue(
            "blocked-count",
            expectation.BlockedCount,
            blockedCount,
            issues);
        AddExpectationIssue(
            "source-count",
            expectation.SourceCount,
            sourceCount,
            issues);
    }

    private static void AddExpectationIssue(
        string name,
        int expected,
        int actual,
        ICollection<SourceAcquisitionPreflightIssue> issues)
    {
        if (expected != actual)
        {
            issues.Add(new SourceAcquisitionPreflightIssue(
                "campaign-expectation-mismatch",
                $"Expected {name} {expected} but found {actual}."));
        }
    }

    private static int SourceOrdinal(string? sourceId) =>
        sourceId is not null
        && ApprovedSources.TryGetValue(sourceId, out var approvedSource)
            ? approvedSource.Ordinal
            : int.MaxValue;

    private static bool IsLowercaseSha256(string? value) =>
        value is not null && LowercaseSha256Regex().IsMatch(value);

    private static ReadOnlyCollection<T> ReadOnly<T>(IEnumerable<T> values) =>
        Array.AsReadOnly(values.ToArray());

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex LowercaseSha256Regex();

    private sealed record ApprovedSourceContract(
        string PlanningAdapterId,
        string CandidateMethod,
        int Ordinal);

    private sealed class StringTupleComparer
        : IEqualityComparer<(string RequestId, string SourceId)>
    {
        public static StringTupleComparer Ordinal { get; } = new();

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
}
