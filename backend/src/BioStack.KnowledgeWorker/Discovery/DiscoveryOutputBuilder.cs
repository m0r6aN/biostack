namespace BioStack.KnowledgeWorker.Discovery;

using System.Globalization;
using System.Text.Json.Nodes;

/// <summary>
/// Turns scored candidates into (a) a schema-valid research-request batch and (b) schema-valid
/// draft substance records. Drafts state only what the discovery sources say (identity, registry
/// or approval status, citations); every other field is "unknown" or empty and the overall tier
/// is "Not graded". Drafts enter the AI council like any record — there is no human gate.
/// </summary>
public static class DiscoveryOutputBuilder
{
    public const string RequesterId = "knowledge-worker-discovery";

    public static JsonObject? BuildResearchRequests(IReadOnlyList<DiscoveredCandidate> candidates, DateTimeOffset now, int minScore, int max)
    {
        var requestedAt = now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var stamp = now.UtcDateTime.ToString("yyyyMMdd'T'HHmm'Z'", CultureInfo.InvariantCulture);
        var requests = new JsonArray();
        foreach (var c in candidates.Where(c => c.Status != CorpusMatchStatus.InCorpus && c.Score >= minScore).Take(Math.Max(1, max)))
        {
            var isAlias = c.Status == CorpusMatchStatus.PossibleAlias;
            var rationale = isAlias
                ? $"Discovery found \"{c.Name}\" in {Describe(c)}. It is not a name or alias of any record, but the {c.MatchedRecord} record mentions it next to its own name: confirm whether it is an alias of {c.MatchedRecord} or a distinct compound."
                : $"New {c.Classification.ToLowerInvariant()} found by open discovery in {Describe(c)}; not in the corpus under any name or alias.";
            requests.Add(new JsonObject
            {
                ["requestId"] = $"discovery-{stamp}-{CandidateNames.Slug(c.Name)}",
                ["compoundName"] = isAlias ? c.MatchedRecord : c.Name,
                ["aliases"] = Array(isAlias ? new[] { c.Name }.Concat(c.Aliases) : c.Aliases),
                ["categories"] = Array(c.AreaIds),
                ["classification"] = c.Classification,
                ["priority"] = c.Phases.Contains("FDA-approved") || c.Score >= 5 ? "high" : "normal",
                ["requesterId"] = RequesterId,
                ["requestedAt"] = requestedAt,
                ["rationale"] = rationale,
                ["notes"] = Array(c.Evidence.Take(6).Select(e => $"{e.SourceId} {e.Identifier}: {e.Url}")),
            });
        }
        if (requests.Count == 0) return null;
        return new JsonObject
        {
            ["schemaVersion"] = "1.0.0",
            ["recordType"] = "research-request-batch",
            ["batch"] = new JsonObject
            {
                ["batchId"] = $"discovery-{stamp}",
                ["requesterId"] = RequesterId,
                ["requestedAt"] = requestedAt,
                ["notes"] = Array(new[]
                {
                    "Open discovery (RunMode=Discovery): ClinicalTrials.gov v2, PubMed E-utilities, openFDA Drugs@FDA, bioRxiv/medRxiv.",
                    "Each request cites the registry, paper, approval or preprint that surfaced it.",
                }),
            },
            ["requests"] = requests,
        };
    }

    public static JsonArray BuildDraftRecords(IReadOnlyList<DiscoveredCandidate> candidates, DateTimeOffset now, int minScore, int max)
    {
        var drafts = new JsonArray();
        foreach (var c in candidates.Where(c => c.Status == CorpusMatchStatus.New && c.Score >= minScore).Take(Math.Max(1, max)))
            drafts.Add(BuildDraft(c, now));
        return drafts;
    }

    public static JsonObject BuildDraft(DiscoveredCandidate c, DateTimeOffset now)
    {
        var slug = CandidateNames.Slug(c.Name);
        var checkedAt = now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var refs = c.Evidence.Select(RefId).ToList();
        var trials = c.Evidence.Where(e => e.SourceId == ClinicalTrialsGovDiscoverySource.Id).ToList();
        var approvals = c.Evidence.Where(e => e.SourceId == OpenFdaDiscoverySource.Id).ToList();

        string status;
        if (approvals.Count > 0)
            status = $"FDA original approval {approvals[0].Date:yyyy-MM-dd} ({approvals[0].Identifier}, Drugs@FDA); label not yet reviewed";
        else if (trials.Count > 0)
            status = $"investigational; registered trials: {string.Join(", ", trials.Take(3).Select(t => t.Identifier))} ({string.Join("/", c.Phases.Where(p => p != "FDA-approved").DefaultIfEmpty("phase unknown"))})";
        else
            status = "unknown (found in literature or preprints only; no registry or approval record seen)";

        var claims = new JsonArray();
        foreach (var t in trials.Take(5))
            claims.Add(Claim($"{c.Name} is being studied in a registered trial: {t.Title} ({t.Identifier}; {t.Detail}).", RefId(t)));
        foreach (var a in approvals.Take(2))
            claims.Add(Claim($"{c.Name} received an FDA original approval on {a.Date:yyyy-MM-dd} ({a.Identifier}).", RefId(a)));
        foreach (var p in c.Evidence.Where(e => e.SourceId is PubMedDiscoverySource.Id or PreprintDiscoverySource.Id).Take(5))
            claims.Add(Claim($"{c.Name} is named in: {p.Title}.", RefId(p)));

        return new JsonObject
        {
            ["schemaVersion"] = "1.0.0",
            ["recordType"] = "substance",
            ["identity"] = new JsonObject
            {
                ["canonicalId"] = slug,
                ["canonicalName"] = c.Name,
                ["slug"] = slug,
                ["aliases"] = Array(c.Aliases.Take(12)),
                ["brandNames"] = new JsonArray(),
                ["synonyms"] = new JsonArray(),
                ["classification"] = c.Classification,
                ["compoundFamily"] = $"unknown (auto-discovered: {string.Join(", ", c.AreaIds)})",
                ["isCombinationProduct"] = false,
                ["activeMoieties"] = Array(new[] { c.Name }),
                ["externalIdentifiers"] = new JsonObject { ["rxnorm"] = null, ["unii"] = null, ["casNumber"] = null, ["drugbank"] = null, ["pubchem"] = null },
            },
            ["regulatory"] = new JsonObject
            {
                ["requiresPrescription"] = approvals.Count > 0,
                ["regulatoryStatus"] = status,
                ["labelStatusByUseCase"] = new JsonArray(),
                ["jurisdiction"] = "US",
                ["approvedIndications"] = new JsonArray(),
                ["offLabelNotes"] = Array(new[] { "Auto-discovered draft: regulatory detail not yet researched." }),
            },
            ["mechanism"] = new JsonObject
            {
                ["mechanismSummary"] = $"Unknown — {c.Name} was found by open discovery on {checkedAt[..10]} and has not been researched yet.",
                ["primaryMechanisms"] = new JsonArray(), ["pathways"] = new JsonArray(), ["targets"] = new JsonArray(),
                ["effectTags"] = new JsonArray(), ["goalTags"] = new JsonArray(),
            },
            ["formulations"] = new JsonArray(),
            ["indications"] = new JsonArray(),
            ["dosingGuidance"] = new JsonArray(),
            ["compatibility"] = new JsonObject
            {
                ["vialCompatibilitySummary"] = "Unknown — not yet researched.",
                ["blendingRules"] = new JsonArray(), ["compatibleBlends"] = new JsonArray(), ["incompatibleBlends"] = new JsonArray(),
                ["unknownCompatibility"] = Array(new[] { "all compounds" }),
            },
            ["safety"] = new JsonObject
            {
                ["contraindications"] = new JsonArray(), ["warnings"] = new JsonArray(), ["precautions"] = new JsonArray(),
                ["adverseEffects"] = new JsonObject { ["common"] = new JsonArray(), ["serious"] = new JsonArray() },
                ["monitoring"] = new JsonArray(),
            },
            ["interactions"] = new JsonArray(),
            ["stackIntelligence"] = new JsonObject
            {
                ["pairsWellWith"] = new JsonArray(), ["avoidWith"] = new JsonArray(), ["overlapRules"] = new JsonArray(),
                ["synergyRules"] = new JsonArray(), ["conflictRules"] = new JsonArray(), ["redundancyRules"] = new JsonArray(),
                ["timingRules"] = new JsonArray(),
            },
            ["supportiveGuidance"] = new JsonObject
            {
                ["nutrition"] = new JsonArray(), ["supplements"] = new JsonArray(), ["sleep"] = new JsonArray(), ["exercise"] = new JsonArray(),
                ["applicabilityNotes"] = new JsonArray(), ["sources"] = new JsonArray(),
            },
            ["evidence"] = new JsonObject
            {
                ["overallTier"] = "Not graded",
                ["claimSpecificEvidence"] = claims,
                ["evidenceGaps"] = Array(new[]
                {
                    "Mechanism, dosing, safety, interactions and efficacy are unknown until research runs on this auto-discovered draft.",
                }),
                ["controversies"] = new JsonArray(),
            },
            ["provenance"] = new JsonObject
            {
                ["sourceRecords"] = new JsonArray(c.Evidence.Select(e => (JsonNode?)SourceRecord(e, checkedAt)).ToArray()),
                ["curationNotes"] = Array(new[]
                {
                    $"Auto-discovered {checkedAt[..10]} by RunMode=Discovery (score {c.Score}; sources: {string.Join(", ", c.SourceIds)}).",
                    $"Source refs: {string.Join(", ", refs.Take(10))}.",
                }.Concat(NameFlagNotes(c))),
                ["lastReviewedAt"] = null,
                ["reviewStatus"] = "draft",
            },
            ["ops"] = new JsonObject
            {
                ["recordVersion"] = 1,
                ["contentHash"] = null,
                ["ingestionSource"] = $"discovery:{string.Join("+", c.SourceIds)}",
                ["ingestedAt"] = null,
                ["updatedAt"] = checkedAt,
                ["lastChangeType"] = "upsert",
                ["isActive"] = true,
                ["completeness"] = "minimal",
                ["needsReview"] = true,
                ["reviewReasons"] = Array(new[] { "Auto-discovered draft awaiting research and AI council review." }.Concat(NameFlagNotes(c))),
                ["qualityFlags"] = Array(new[] { "auto-discovered" }.Concat(c.NameFlags ?? System.Array.Empty<string>())),
            },
        };
    }

    private static IEnumerable<string> NameFlagNotes(DiscoveredCandidate c)
        => (c.NameFlags ?? System.Array.Empty<string>()).Select(f => f switch
        {
            _ when f.StartsWith(CandidateScreener.RenamedFlag, StringComparison.Ordinal) => "Name corrected to the RxNorm spelling; the spelling found in the source is kept as an alias. Confirm the name.",
            CandidateScreener.UnavailableFlag => "The RxNorm spelling check was unavailable when this draft was made. Confirm the spelling.",
            _ => "Name flagged: it may be a material, a generic class word or a study descriptor rather than a substance. Confirm it names a real substance.",
        }).Distinct();

    public static string RefId(DiscoveryEvidence e)
        => $"{e.SourceId}:{CandidateNames.Slug(e.Identifier)}";

    private static JsonObject Claim(string claim, string source) => new()
    {
        ["claim"] = claim,
        ["tier"] = "Not graded",
        ["confidence"] = "unknown",
        ["sources"] = Array(new[] { source }),
        ["gradeRationale"] = "Discovery fact (where the name was found); not yet graded on the 7-tier scale.",
    };

    private static JsonObject SourceRecord(DiscoveryEvidence e, string checkedAt) => new()
    {
        ["sourceType"] = e.SourceId switch
        {
            ClinicalTrialsGovDiscoverySource.Id => "trial-registry",
            OpenFdaDiscoverySource.Id => "regulator",
            PubMedDiscoverySource.Id => "paper",
            _ => "other",
        },
        ["title"] = string.IsNullOrWhiteSpace(e.Title) ? e.Identifier : e.Title,
        ["publisher"] = e.SourceId switch
        {
            ClinicalTrialsGovDiscoverySource.Id => "ClinicalTrials.gov",
            OpenFdaDiscoverySource.Id => "U.S. Food and Drug Administration (Drugs@FDA via openFDA)",
            PubMedDiscoverySource.Id => e.Detail ?? "PubMed",
            _ => e.Detail ?? "bioRxiv/medRxiv",
        },
        ["url"] = e.Url,
        ["publishedAt"] = e.Date is { } d ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00:00Z" : null,
        ["lastCheckedAt"] = checkedAt,
    };

    private static string Describe(DiscoveredCandidate c)
        => string.Join("; ", c.Evidence.Take(3).Select(e => $"{e.SourceId} {e.Identifier}"))
           + (c.Evidence.Count > 3 ? $" and {c.Evidence.Count - 3} more" : string.Empty);

    private static JsonArray Array(IEnumerable<string> values)
        => new(values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).Select(v => (JsonNode?)v).ToArray());
}
