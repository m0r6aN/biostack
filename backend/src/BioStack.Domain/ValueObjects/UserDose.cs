namespace BioStack.Domain.ValueObjects;

/// <summary>
/// Rules for the user-entered structured dose on a compound record.
/// The dose is always the user's own entry: BioStack never suggests, prefills,
/// computes or ranges a dose. Validation is limited to "is this a storable,
/// positive number in an allowed unit"; there are deliberately no minimum or
/// maximum dose thresholds, because any threshold would read as a recommended
/// range.
/// </summary>
public static class UserDose
{
    /// <summary>Canonical unit spellings, in display order.</summary>
    public static readonly IReadOnlyList<string> AllowedUnits =
        ["mcg", "mg", "g", "IU", "mL", "units", "capsule", "tablet", "softgel", "scoop", "drop", "serving", "gummy"];

    public const int FrequencyMaxLength = 100;
    public const int TimeMaxLength = 100;
    public const int LogNoteMaxLength = 500;
    public const int AmountPrecision = 18;
    public const int AmountScale = 4;

    // Largest value a decimal(18,4) column can hold.
    private const decimal MaxStorableAmount = 99_999_999_999_999.9999m;

    /// <summary>Returns the canonical unit spelling (case-insensitive match), or null when the unit is not allowed.</summary>
    public static string? NormalizeUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            return null;
        }

        var trimmed = unit.Trim();
        foreach (var allowed in AllowedUnits)
        {
            if (string.Equals(allowed, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return allowed;
            }
        }

        return null;
    }

    /// <summary>
    /// Validates the four user-entered dose fields and returns the normalized values.
    /// Returns an error (field name in camelCase wire form plus message) or null when valid.
    /// </summary>
    public static (string Field, string Message)? Validate(
        decimal? amount,
        string? unit,
        string? frequency,
        string? time,
        out decimal? normalizedAmount,
        out string? normalizedUnit,
        out string? normalizedFrequency,
        out string? normalizedTime)
    {
        normalizedAmount = null;
        normalizedUnit = null;
        normalizedFrequency = Clean(frequency);
        normalizedTime = Clean(time);

        if (normalizedFrequency is { Length: > FrequencyMaxLength })
        {
            return ("doseFrequency", $"Frequency must be {FrequencyMaxLength} characters or fewer.");
        }

        if (normalizedTime is { Length: > TimeMaxLength })
        {
            return ("doseTime", $"Time or schedule must be {TimeMaxLength} characters or fewer.");
        }

        var hasUnit = !string.IsNullOrWhiteSpace(unit);
        if (amount is null && !hasUnit)
        {
            return null;
        }

        if (amount is null)
        {
            return ("doseAmount", "Enter an amount, or clear the unit.");
        }

        if (amount.Value <= 0m)
        {
            return ("doseAmount", "Amount must be a number greater than zero.");
        }

        if (amount.Value > MaxStorableAmount)
        {
            return ("doseAmount", "Amount is too large to store.");
        }

        if (decimal.Round(amount.Value, AmountScale) != amount.Value)
        {
            return ("doseAmount", $"Amount can have at most {AmountScale} decimal places.");
        }

        if (!hasUnit)
        {
            return ("doseUnit", "Choose a unit for the amount.");
        }

        var canonical = NormalizeUnit(unit);
        if (canonical is null)
        {
            return ("doseUnit", $"Unit must be one of: {string.Join(", ", AllowedUnits)}.");
        }

        normalizedAmount = amount.Value;
        normalizedUnit = canonical;
        return null;
    }

    /// <summary>Plain-text rendering of the user's own entry, e.g. "250 mcg" (null when no amount was entered).</summary>
    public static string? FormatAmount(decimal? amount, string? unit)
    {
        if (amount is null || string.IsNullOrWhiteSpace(unit))
        {
            return null;
        }

        // decimal.ToString("0.####") drops trailing zeros without scientific notation.
        return $"{amount.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)} {unit}";
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
