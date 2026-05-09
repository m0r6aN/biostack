namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

public interface IEvidencePacketSubstanceRecordCompiler
{
    JsonNode CompileDraft(JsonNode evidencePacket);
}

public sealed class EvidencePacketSubstanceRecordCompiler : IEvidencePacketSubstanceRecordCompiler
{
    public JsonNode CompileDraft(JsonNode evidencePacket)
    {
        if (evidencePacket is null) throw new ArgumentNullException(nameof(evidencePacket));

        var root = evidencePacket.AsObject();
        var packet = root["packet"]!.AsObject();
        var compound = root["compound"]!.AsObject();
        var ops = root["ops"]!.AsObject();
        var canonicalName = ReadString(compound["canonicalName"]);
        var slug = SubstanceRecordNormalizer.Slugify(canonicalName);
        var sourceIds = ReadSourceIds(root).ToList();
        var reviewReasons = ReadStringArray(ops["reviewReasons"])
            .Append("Compiled draft requires human review before publication.")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var qualityFlags = ReadStringArray(ops["qualityFlags"])
            .Append("compiled-from-evidence-packet")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new JsonObject
        {
            ["schemaVersion"] = "1.0.0",
            ["recordType"] = "substance",
            ["identity"] = BuildIdentity(compound, canonicalName, slug),
            ["regulatory"] = BuildRegulatory(),
            ["mechanism"] = BuildMechanism(root),
            ["formulations"] = new JsonArray(),
            ["indications"] = new JsonArray(),
            ["dosingGuidance"] = new JsonArray(),
            ["compatibility"] = BuildCompatibility(),
            ["safety"] = BuildSafety(),
            ["interactions"] = new JsonArray(),
            ["stackIntelligence"] = BuildStackIntelligence(),
            ["supportiveGuidance"] = BuildSupportiveGuidance(sourceIds),
            ["evidence"] = BuildEvidence(root),
            ["provenance"] = BuildProvenance(root, reviewReasons),
            ["ops"] = new JsonObject
            {
                ["recordVersion"] = 1,
                ["contentHash"] = null,
                ["ingestionSource"] = $"evidence-packet:{ReadString(packet["packetId"])}",
                ["ingestedAt"] = NullableString(ReadString(packet["generatedAt"])),
                ["updatedAt"] = NullableString(ReadString(packet["generatedAt"])),
                ["lastChangeType"] = "seed",
                ["isActive"] = false,
                ["completeness"] = ReadString(ops["completeness"]) is { Length: > 0 } c ? c : "minimal",
                ["needsReview"] = true,
                ["reviewReasons"] = ToJsonArray(reviewReasons),
                ["qualityFlags"] = ToJsonArray(qualityFlags),
            },
        };
    }

    private static JsonObject BuildIdentity(JsonObject compound, string canonicalName, string slug)
    {
        var ids = compound["externalIdentifiers"]!.AsObject();
        return new JsonObject
        {
            ["canonicalId"] = slug,
            ["canonicalName"] = canonicalName,
            ["slug"] = slug,
            ["aliases"] = ToJsonArray(ReadStringArray(compound["aliases"])),
            ["brandNames"] = new JsonArray(),
            ["synonyms"] = ToJsonArray(ReadStringArray(compound["aliases"])),
            ["classification"] = NormalizeClassification(ReadString(compound["classification"])),
            ["compoundFamily"] = ReadString(compound["compoundFamily"]) is { Length: > 0 } f ? f : "Unknown",
            ["isCombinationProduct"] = false,
            ["activeMoieties"] = ToJsonArray(new[] { canonicalName }),
            ["externalIdentifiers"] = new JsonObject
            {
                ["rxnorm"] = NullableString(ReadString(ids["rxnorm"])),
                ["unii"] = NullableString(ReadString(ids["unii"])),
                ["casNumber"] = NullableString(ReadString(ids["casNumber"])),
                ["drugbank"] = NullableString(ReadString(ids["drugbank"])),
                ["pubchem"] = NullableString(ReadString(ids["pubchem"])),
            },
        };
    }

    private static JsonObject BuildRegulatory() => new()
    {
        ["requiresPrescription"] = false,
        ["regulatoryStatus"] = "unknown",
        ["labelStatusByUseCase"] = new JsonArray(),
        ["jurisdiction"] = "unknown",
        ["approvedIndications"] = new JsonArray(),
        ["offLabelNotes"] = ToJsonArray(new[] { "Draft record: regulatory status has not been authoritatively established." }),
    };

    private static JsonObject BuildMechanism(JsonObject root)
    {
        var mechanismClaims = Claims(root, "mechanism", "target-pathway").ToList();
        return new JsonObject
        {
            ["mechanismSummary"] = mechanismClaims.FirstOrDefault() is { } c
                ? ReadString(c["statement"])
                : "Mechanism has not been reviewed for this draft record.",
            ["primaryMechanisms"] = ToJsonArray(mechanismClaims.Select(c => ReadString(c["statement"]))),
            ["pathways"] = new JsonArray(),
            ["targets"] = new JsonArray(),
            ["effectTags"] = new JsonArray(),
            ["goalTags"] = new JsonArray(),
        };
    }

    private static JsonObject BuildCompatibility() => new()
    {
        ["vialCompatibilitySummary"] = null,
        ["blendingRules"] = new JsonArray(),
        ["compatibleBlends"] = new JsonArray(),
        ["incompatibleBlends"] = new JsonArray(),
        ["unknownCompatibility"] = new JsonArray(),
    };

    private static JsonObject BuildSafety() => new()
    {
        ["contraindications"] = new JsonArray(),
        ["warnings"] = new JsonArray(),
        ["precautions"] = new JsonArray(),
        ["adverseEffects"] = new JsonObject
        {
            ["common"] = new JsonArray(),
            ["serious"] = new JsonArray(),
        },
        ["monitoring"] = new JsonArray(),
    };

    private static JsonObject BuildStackIntelligence() => new()
    {
        ["pairsWellWith"] = new JsonArray(),
        ["avoidWith"] = new JsonArray(),
        ["overlapRules"] = new JsonArray(),
        ["synergyRules"] = new JsonArray(),
        ["conflictRules"] = new JsonArray(),
        ["redundancyRules"] = new JsonArray(),
        ["timingRules"] = new JsonArray(),
    };

    private static JsonObject BuildSupportiveGuidance(IEnumerable<string> sourceIds) => new()
    {
        ["nutrition"] = new JsonArray(),
        ["supplements"] = new JsonArray(),
        ["sleep"] = new JsonArray(),
        ["exercise"] = new JsonArray(),
        ["applicabilityNotes"] = new JsonArray(),
        ["sources"] = ToJsonArray(sourceIds),
    };

    private static JsonObject BuildEvidence(JsonObject root)
    {
        var claims = root["claims"]?.AsArray().Select(c => c!.AsObject()).ToList() ?? new List<JsonObject>();
        var claimEvidence = new JsonArray();
        foreach (var claim in claims)
        {
            claimEvidence.Add(new JsonObject
            {
                ["claim"] = ReadString(claim["statement"]),
                ["tier"] = NormalizeEvidenceTier(ReadString(claim["evidenceTier"])),
                ["confidence"] = NormalizeConfidence(ReadString(claim["confidence"])),
                ["sources"] = ToJsonArray(ReadStringArray(claim["sourceRefs"])),
            });
        }

        return new JsonObject
        {
            ["overallTier"] = BestEvidenceTier(claims.Select(c => ReadString(c["evidenceTier"]))),
            ["claimSpecificEvidence"] = claimEvidence,
            ["evidenceGaps"] = ToJsonArray(Claims(root, "evidence-gap").Select(c => ReadString(c["statement"]))),
            ["controversies"] = ToJsonArray(Claims(root, "controversy", "misinformation-claim").Select(c => ReadString(c["statement"]))),
        };
    }

    private static JsonObject BuildProvenance(JsonObject root, IEnumerable<string> reviewReasons)
    {
        var sourceRecords = new JsonArray();
        foreach (var sourceNode in root["sources"]?.AsArray() ?? new JsonArray())
        {
            if (sourceNode is null) continue;
            var source = sourceNode.AsObject();
            sourceRecords.Add(new JsonObject
            {
                ["sourceType"] = MapSourceType(ReadString(source["sourceType"])),
                ["title"] = ReadString(source["title"]),
                ["publisher"] = NullableString(ReadString(source["publisher"])),
                ["url"] = NullableString(ReadString(source["url"])),
                ["publishedAt"] = NullableString(ReadString(source["publishedAt"])),
                ["lastCheckedAt"] = NullableString(ReadString(source["accessedAt"])),
            });
        }

        return new JsonObject
        {
            ["sourceRecords"] = sourceRecords,
            ["curationNotes"] = ToJsonArray(reviewReasons),
            ["lastReviewedAt"] = null,
            ["reviewStatus"] = "draft",
        };
    }

    private static IEnumerable<JsonObject> Claims(JsonObject root, params string[] claimTypes)
    {
        var set = new HashSet<string>(claimTypes, StringComparer.OrdinalIgnoreCase);
        foreach (var claimNode in root["claims"]?.AsArray() ?? new JsonArray())
        {
            if (claimNode is null) continue;
            var claim = claimNode.AsObject();
            if (set.Contains(ReadString(claim["claimType"]))) yield return claim;
        }
    }

    private static IEnumerable<string> ReadSourceIds(JsonObject root)
        => (root["sources"]?.AsArray() ?? new JsonArray())
            .Where(s => s is not null)
            .Select(s => ReadString(s!["sourceId"]))
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static string MapSourceType(string sourceType) => sourceType.ToLowerInvariant() switch
    {
        "label" => "label",
        "regulator" => "regulator",
        "guideline" => "guideline",
        "manufacturer" => "manufacturer",
        "paper" => "paper",
        "review" or "systematic-review" or "medical-summary" => "review",
        "database" or "clinical-trial-registry" => "database",
        "internal-curation" => "internal-curation",
        _ => "other",
    };

    private static string NormalizeClassification(string value)
        => ResearchAllowedClassifications.Contains(value) ? value : "Other";

    private static string NormalizeEvidenceTier(string value)
        => value.Equals("Anecdotal", StringComparison.OrdinalIgnoreCase) ? "Insufficient"
            : AllowedEvidenceTiers.Contains(value) ? value
            : "Unknown";

    private static string NormalizeConfidence(string value)
        => AllowedConfidence.Contains(value.ToLowerInvariant()) ? value.ToLowerInvariant() : "unknown";

    private static string BestEvidenceTier(IEnumerable<string> tiers)
    {
        var best = tiers.Select(NormalizeEvidenceTier)
            .OrderByDescending(t => EvidenceTierRank.TryGetValue(t, out var rank) ? rank : 0)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(best) ? "Unknown" : best;
    }

    private static string ReadString(JsonNode? node)
        => node?.GetValue<string>()?.Trim() ?? string.Empty;

    private static List<string> ReadStringArray(JsonNode? node)
    {
        if (node is not JsonArray arr) return new List<string>();
        return arr.Select(item => item?.GetValue<string>()?.Trim() ?? string.Empty)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static JsonNode? NullableString(string value)
        => string.IsNullOrWhiteSpace(value) ? null : JsonValue.Create(value);

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var arr = new JsonArray();
        foreach (var value in values.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            arr.Add(value);
        }
        return arr;
    }

    private static readonly HashSet<string> ResearchAllowedClassifications = new(StringComparer.Ordinal)
    {
        "Peptide", "Small Molecule", "Biologic", "Hormone", "SARM", "SERM", "Vitamin",
        "Mineral", "Supplement", "Pharmaceutical", "Amino Acid", "Research Compound", "Other",
    };

    private static readonly HashSet<string> AllowedEvidenceTiers = new(StringComparer.Ordinal)
    {
        "Strong", "Moderate", "Limited", "Insufficient", "Unknown",
    };

    private static readonly HashSet<string> AllowedConfidence = new(StringComparer.Ordinal)
    {
        "low", "moderate", "high", "unknown",
    };

    private static readonly Dictionary<string, int> EvidenceTierRank = new(StringComparer.Ordinal)
    {
        ["Strong"] = 5,
        ["Moderate"] = 4,
        ["Limited"] = 3,
        ["Insufficient"] = 2,
        ["Unknown"] = 1,
    };
}