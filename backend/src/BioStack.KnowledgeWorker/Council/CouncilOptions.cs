namespace BioStack.KnowledgeWorker.Council;

/// <summary>
/// Two-reviewer AI council configuration, bound from the <c>Council</c> section.
/// API keys are read from configuration that is populated by environment variables
/// (<c>Council__Reviewers__0__ApiKey</c>, <c>Council__Reviewers__1__ApiKey</c>), which the
/// Container App Job resolves from Key Vault. Never put a key in appsettings or source.
/// </summary>
public sealed class CouncilOptions
{
    public List<CouncilReviewerOptions> Reviewers { get; set; } = new();

    /// <summary>Max compounds reviewed per run.</summary>
    public int MaxItemsPerRun { get; set; } = 20;

    /// <summary>Where review-decision batches are read from and written to.</summary>
    public string DecisionDirectory { get; set; } = "research/review-decisions";

    /// <summary>Where owner escalations (options for Clint) are written.</summary>
    public string EscalationDirectory { get; set; } = "research/owner-escalations";

    /// <summary>Where legal items are logged as known risks.</summary>
    public string KnownRiskDirectory { get; set; } = "research/known-risks";

    public int RequestTimeoutSeconds { get; set; } = 120;

    public IReadOnlyList<string> MissingSettings()
    {
        var missing = new List<string>();
        if (Reviewers.Count != 2)
        {
            missing.Add($"Council:Reviewers must have exactly 2 entries (found {Reviewers.Count})");
        }
        for (var i = 0; i < Reviewers.Count; i++)
        {
            var r = Reviewers[i];
            if (string.IsNullOrWhiteSpace(r.Provider)) missing.Add($"Council__Reviewers__{i}__Provider");
            else if (!CouncilProviders.IsKnown(r.Provider)) missing.Add($"Council__Reviewers__{i}__Provider (unknown '{r.Provider}'; use {string.Join(" or ", CouncilProviders.All)})");
            if (string.IsNullOrWhiteSpace(r.Model)) missing.Add($"Council__Reviewers__{i}__Model");
            if (string.IsNullOrWhiteSpace(r.ApiKey)) missing.Add($"Council__Reviewers__{i}__ApiKey");
        }
        return missing;
    }
}

public sealed class CouncilReviewerOptions
{
    /// <summary>Stable id used in decisions, e.g. "reviewer-a". Defaults to "reviewer-{index}".</summary>
    public string? Id { get; set; }

    /// <summary><c>openai-compatible</c> or <c>anthropic</c>.</summary>
    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    /// <summary>Base URL. Defaults: https://api.openai.com/v1 or https://api.anthropic.com.</summary>
    public string? Endpoint { get; set; }

    /// <summary>For openai-compatible only: <c>bearer</c> (default) or <c>api-key</c> (Azure OpenAI).</summary>
    public string AuthHeader { get; set; } = "bearer";

    /// <summary>Secret. Supplied by environment / Key Vault only.</summary>
    public string? ApiKey { get; set; }
}

public static class CouncilProviders
{
    public const string OpenAiCompatible = "openai-compatible";
    public const string Anthropic = "anthropic";
    public static readonly string[] All = { OpenAiCompatible, Anthropic };
    public static bool IsKnown(string provider) => All.Contains(provider.Trim(), StringComparer.OrdinalIgnoreCase);
}
