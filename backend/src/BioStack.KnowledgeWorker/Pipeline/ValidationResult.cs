namespace BioStack.KnowledgeWorker.Pipeline;

/// <summary>A single schema-validation failure, pinned to a JSON Pointer location.</summary>
public sealed record ValidationError(string Location, string Keyword, string Message)
{
    public override string ToString() => $"{Location} [{Keyword}]: {Message}";
}

/// <summary>
/// Result of schema validation for a single record. When <see cref="IsValid"/> is false,
/// <see cref="Errors"/> lists all reported schema violations; the record must be rejected.
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<ValidationError> Errors { get; }

    private ValidationResult(bool isValid, IReadOnlyList<ValidationError> errors)
    {
        IsValid = isValid;
        Errors  = errors;
    }

    public static ValidationResult Valid() => new(true, Array.Empty<ValidationError>());

    public static ValidationResult Invalid(IReadOnlyList<ValidationError> errors)
        => new(false, errors);

    public string Summary()
        => IsValid
            ? "valid"
            : $"invalid ({Errors.Count} error{(Errors.Count == 1 ? "" : "s")}): "
              + string.Join("; ", Errors.Select(e => e.ToString()));
}
