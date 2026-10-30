namespace BioStack.KnowledgeWorker.Discovery;

/// <summary>Bound from the "Discovery" configuration section (env: <c>Discovery__*</c>).</summary>
public sealed class DiscoveryOptions
{
    public string ScopePath { get; set; } = "Seeds/discovery-scope.json";

    /// <summary>Where RunMode=Discovery writes its report and research-request batch.</summary>
    public string OutputDirectory { get; set; } = "research/discovery";

    /// <summary>How far back each run looks (registration, publication, approval or posting date).</summary>
    public int LookbackDays { get; set; } = 30;

    /// <summary>Minimum score for a new candidate to get a draft record (CT.gov/FDA 3, PubMed 2, preprint 1 per distinct source).</summary>
    public int MinScore { get; set; } = 2;

    public int MaxCandidatesPerRun { get; set; } = 50;

    /// <summary>Check the spelling of new plain-word names against NLM RxNorm before a draft is made (a misspelling is corrected, not drafted as a separate record).</summary>
    public bool NameCheckEnabled { get; set; } = true;

    /// <summary>Upper bound on RxNorm spelling checks per run (two requests each at most).</summary>
    public int MaxNameChecksPerRun { get; set; } = 80;

    public bool ClinicalTrialsEnabled { get; set; } = true;
    public bool PubMedEnabled { get; set; } = true;
    public bool OpenFdaEnabled { get; set; } = true;
    public bool PreprintsEnabled { get; set; } = true;

    public int ClinicalTrialsPageSize { get; set; } = 100;
    public int MaxPagesPerQuery { get; set; } = 2;
    public int PubMedRetMax { get; set; } = 100;
    public int OpenFdaMaxPages { get; set; } = 10;
    public int PreprintMaxPagesPerServer { get; set; } = 40;

    /// <summary>Preprint volume is high (~200/day on bioRxiv), so preprints use a shorter window.</summary>
    public int PreprintLookbackDays { get; set; } = 7;

    /// <summary>Delay between requests to one host. NCBI allows 3 req/s without a key, 10 with one.</summary>
    public int RequestDelayMilliseconds { get; set; } = 400;

    /// <summary>NCBI E-utilities "tool" parameter (required by NCBI usage policy).</summary>
    public string NcbiTool { get; set; } = "biostack-knowledge-worker";

    /// <summary>NCBI E-utilities "email" parameter. PubMed discovery is skipped until this is set.</summary>
    public string? NcbiEmail { get; set; }

    /// <summary>Optional NCBI API key (Key Vault secret → env Discovery__NcbiApiKey). Raises the limit to 10 req/s.</summary>
    public string? NcbiApiKey { get; set; }

    /// <summary>Optional openFDA API key (env Discovery__OpenFdaApiKey). Raises the daily quota.</summary>
    public string? OpenFdaApiKey { get; set; }

    public string UserAgent { get; set; } = "BioStackKnowledgeWorker/1.0 (open discovery; metadata only)";
}
