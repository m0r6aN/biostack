namespace BioStack.Application.Governance;

/// <summary>
/// Classifies the intent of a text fragment before policy checking.
/// Maps to Keon Runtime's classification taxonomy.
/// </summary>
public enum LanguageClassification
{
    Educational,
    Observational,
    Comparative,
    Optimization,
    Prescriptive,
    Medical,
    Prohibited
}

public sealed record ClassificationResult(
    LanguageClassification Classification,
    string? Rationale);
