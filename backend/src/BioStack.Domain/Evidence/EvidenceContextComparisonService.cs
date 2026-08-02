namespace BioStack.Domain.Evidence;

/// <summary>
/// Pure domain comparison: unit math + range checks + non-prescriptive statements.
/// The 12 mg vs 0.5–1.0 mg weekly initiation case is one instance of this general pattern
/// (user-recorded amount from informal sources vs reviewed trial initiation).
/// </summary>
public sealed class EvidenceContextComparisonService : IEvidenceContextComparisonService
{
    public EvidenceContextComparison Compare(
        ProtocolExposure exposure,
        ReviewedEvidenceProfile evidence)
    {
        ArgumentNullException.ThrowIfNull(exposure);
        ArgumentNullException.ThrowIfNull(evidence);

        if (exposure.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exposure), "Amount must be greater than zero.");
        }

        var signals = new List<string>();
        var statements = new List<string>();
        var uncertainty = new List<string>();
        var sources = evidence.Regimens
            .Select(r => r.SourceCitation)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!evidence.HasHumanEvidence || evidence.Regimens.Count == 0)
        {
            signals.Add(EvidenceComparisonSignals.NoHumanEvidence);
            uncertainty.Add("EVIDENCE_LIMITED");
            statements.Add(
                "Human evidence was not found for this comparison. " +
                "The available evidence cannot establish how the entered amount compares with reviewed research.");
            return Build(
                exposure,
                signals,
                null, null, null, null, null,
                NormalizeUnit(exposure.Unit),
                null, null, null,
                routeMismatch: false,
                frequencyMismatch: false,
                unitMismatch: false,
                decimalError: false,
                sources,
                statements,
                uncertainty);
        }

        if (evidence.EvidenceLimitedToAnimals)
        {
            signals.Add(EvidenceComparisonSignals.EvidenceLimitedToAnimals);
            uncertainty.Add("EVIDENCE_NON_HUMAN");
        }

        if (evidence.EvidenceLimitedToCaseReports)
        {
            signals.Add(EvidenceComparisonSignals.EvidenceLimitedToCaseReports);
            uncertainty.Add("EVIDENCE_CASE_REPORT");
        }

        if (evidence.ConflictingHumanEvidence)
        {
            signals.Add(EvidenceComparisonSignals.ConflictingHumanEvidence);
            uncertainty.Add("EVIDENCE_CONFLICTING");
        }

        var normalizedUnit = ChooseComparisonUnit(exposure, evidence.Regimens);
        var userAmount = TryConvert(exposure.Amount, exposure.Unit, normalizedUnit);

        var unitMismatch = userAmount is null;
        if (unitMismatch)
        {
            signals.Add(EvidenceComparisonSignals.UnitMismatchSuspected);
            uncertainty.Add("OUTSIDE_REVIEWED_CONTEXT");
            statements.Add(
                "A unit mismatch is suspected, so a numeric comparison with reviewed study amounts could not be completed safely.");
        }

        var initiationRegimens = evidence.Regimens
            .Where(r => r.InitiationAmountMin.HasValue || r.InitiationAmountMax.HasValue)
            .Select(r => new
            {
                Regimen = r,
                Min = ConvertOptional(r.InitiationAmountMin, r.Unit, normalizedUnit),
                Max = ConvertOptional(r.InitiationAmountMax, r.Unit, normalizedUnit)
                    ?? ConvertOptional(r.InitiationAmountMin, r.Unit, normalizedUnit),
            })
            .Where(x => x.Min.HasValue || x.Max.HasValue)
            .ToList();

        decimal? closestInitMin = null;
        decimal? closestInitMax = null;
        decimal? timesMin = null;
        decimal? timesMax = null;
        decimal? highest = null;

        foreach (var regimen in evidence.Regimens)
        {
            var candidates = new[]
            {
                ConvertOptional(regimen.InitiationAmountMax, regimen.Unit, normalizedUnit),
                ConvertOptional(regimen.MaintenanceAmountMax, regimen.Unit, normalizedUnit),
                ConvertOptional(regimen.MaximumStudiedAmount, regimen.Unit, normalizedUnit),
                ConvertOptional(regimen.InitiationAmountMin, regimen.Unit, normalizedUnit),
                ConvertOptional(regimen.MaintenanceAmountMin, regimen.Unit, normalizedUnit),
            };
            foreach (var value in candidates)
            {
                if (value is null)
                {
                    continue;
                }

                if (highest is null || value > highest)
                {
                    highest = value;
                }
            }
        }

        if (initiationRegimens.Count == 0)
        {
            signals.Add(EvidenceComparisonSignals.NoReviewedInitiationMatch);
            statements.Add(
                "No reviewed initiation range was available in this evidence set for comparison.");
        }
        else if (userAmount is not null)
        {
            closestInitMin = initiationRegimens.Min(x => x.Min ?? x.Max);
            closestInitMax = initiationRegimens.Max(x => x.Max ?? x.Min);

            var inAnyInitiation = initiationRegimens.Any(x =>
            {
                var min = x.Min ?? x.Max;
                var max = x.Max ?? x.Min;
                return min.HasValue && max.HasValue && userAmount >= min && userAmount <= max;
            });

            if (inAnyInitiation)
            {
                signals.Add(EvidenceComparisonSignals.ExactMatchStudyContext);
                statements.Add(
                    $"Reviewed trials initiated participants between {Format(closestInitMin)} and {Format(closestInitMax)} {normalizedUnit}" +
                    FrequencyClause(exposure.Frequency) +
                    ". The entered amount falls within that reviewed initiation range.");
            }
            else if (closestInitMax.HasValue && userAmount > closestInitMax)
            {
                signals.Add(EvidenceComparisonSignals.AboveReviewedInitiationRange);
                uncertainty.Add("OUTSIDE_REVIEWED_CONTEXT");
                timesMin = closestInitMin is > 0 ? RoundRatio(userAmount.Value / closestInitMin.Value) : null;
                timesMax = closestInitMax is > 0 ? RoundRatio(userAmount.Value / closestInitMax.Value) : null;

                statements.Add(
                    $"Reviewed trials initiated participants between {Format(closestInitMin)} and {Format(closestInitMax)} {normalizedUnit}" +
                    FrequencyClause(exposure.Frequency) + ".");

                if (timesMin is not null && timesMax is not null)
                {
                    var low = Math.Min(timesMin.Value, timesMax.Value);
                    var high = Math.Max(timesMin.Value, timesMax.Value);
                    statements.Add(
                        $"The recorded {Format(userAmount)} {normalizedUnit} amount is {Format(low)} to {Format(high)} times the initiation range used in the reviewed trials.");
                }

                statements.Add(
                    "No reviewed trial in this evidence set initiated participants at the entered amount.");

                // Decimal-error heuristic: exact 10x / 100x of a range bound often indicates misplaced decimal.
                if (IsNearMultiple(userAmount.Value, closestInitMax.Value, 10m)
                    || IsNearMultiple(userAmount.Value, closestInitMin ?? closestInitMax.Value, 10m)
                    || IsNearMultiple(userAmount.Value, closestInitMax.Value, 100m))
                {
                    signals.Add(EvidenceComparisonSignals.DecimalErrorSuspected);
                    statements.Add(
                        "A decimal or unit placement error is possible because the entered amount is an exact multiple of a reviewed initiation value.");
                }
            }
            else if (closestInitMin.HasValue && userAmount < closestInitMin)
            {
                signals.Add(EvidenceComparisonSignals.BelowReviewedRange);
                statements.Add(
                    $"Reviewed trials initiated participants between {Format(closestInitMin)} and {Format(closestInitMax)} {normalizedUnit}" +
                    FrequencyClause(exposure.Frequency) +
                    ". The entered amount is below that reviewed initiation range.");
            }
            else
            {
                signals.Add(EvidenceComparisonSignals.NoReviewedInitiationMatch);
                statements.Add(
                    "No reviewed trial in this evidence set initiated participants at the entered amount.");
            }
        }

        if (userAmount is not null && highest is not null && userAmount > highest)
        {
            if (!signals.Contains(EvidenceComparisonSignals.AboveHighestReviewedExposure))
            {
                signals.Add(EvidenceComparisonSignals.AboveHighestReviewedExposure);
            }

            statements.Add(
                $"The entered amount is above the highest reviewed exposure in this evidence set ({Format(highest)} {normalizedUnit}).");
        }

        var routeMismatch = HasRouteMismatch(exposure, evidence.Regimens);
        if (routeMismatch)
        {
            signals.Add(EvidenceComparisonSignals.RouteNotStudied);
            uncertainty.Add("ROUTE_NOT_STUDIED");
            statements.Add(
                "Human evidence for the entered route was not found in this reviewed set, so route applicability is uncertain.");
        }

        var frequencyMismatch = HasFrequencyMismatch(exposure, evidence.Regimens);
        if (frequencyMismatch)
        {
            signals.Add(EvidenceComparisonSignals.FrequencyNotStudied);
            statements.Add(
                "The entered frequency was not found among reviewed regimens in this evidence set.");
        }

        // Never emit Class D language. Encourage clinician discussion only as Class C context.
        if (signals.Contains(EvidenceComparisonSignals.AboveReviewedInitiationRange)
            || signals.Contains(EvidenceComparisonSignals.AboveHighestReviewedExposure))
        {
            statements.Add(
                "The reviewed evidence supports a lower-exposure initiation context than the amount entered. " +
                "Discuss material differences with a qualified clinician before proceeding.");
        }

        return Build(
            exposure,
            signals.Distinct(StringComparer.Ordinal).ToList(),
            closestInitMin,
            closestInitMax,
            evidence.Regimens
                .Select(r => ConvertOptional(r.MaintenanceAmountMin, r.Unit, normalizedUnit))
                .Where(v => v.HasValue)
                .DefaultIfEmpty()
                .Min(),
            evidence.Regimens
                .Select(r => ConvertOptional(r.MaintenanceAmountMax, r.Unit, normalizedUnit))
                .Where(v => v.HasValue)
                .DefaultIfEmpty()
                .Max(),
            highest,
            normalizedUnit,
            userAmount,
            timesMin,
            timesMax,
            routeMismatch,
            frequencyMismatch,
            unitMismatch,
            signals.Contains(EvidenceComparisonSignals.DecimalErrorSuspected),
            sources,
            statements,
            uncertainty.Distinct(StringComparer.Ordinal).ToList());
    }

    private static EvidenceContextComparison Build(
        ProtocolExposure exposure,
        IReadOnlyList<string> signals,
        decimal? initMin,
        decimal? initMax,
        decimal? maintMin,
        decimal? maintMax,
        decimal? highest,
        string unit,
        decimal? userAmount,
        decimal? timesMin,
        decimal? timesMax,
        bool routeMismatch,
        bool frequencyMismatch,
        bool unitMismatch,
        bool decimalError,
        IReadOnlyList<string> sources,
        IReadOnlyList<string> statements,
        IReadOnlyList<string> uncertainty)
        => new(
            SubjectName: exposure.SubjectName,
            Exposure: exposure,
            RiskSignals: signals,
            ClosestInitiationMin: initMin,
            ClosestInitiationMax: initMax,
            ClosestMaintenanceMin: maintMin,
            ClosestMaintenanceMax: maintMax,
            HighestStudiedExposure: highest,
            NormalizedUnit: unit,
            UnitNormalizedUserAmount: userAmount,
            TimesAboveInitiationMin: timesMin,
            TimesAboveInitiationMax: timesMax,
            RouteMismatch: routeMismatch,
            FrequencyMismatch: frequencyMismatch,
            UnitMismatchSuspected: unitMismatch,
            DecimalErrorSuspected: decimalError,
            SourceReferences: sources,
            Statements: statements,
            UncertaintyMarkers: uncertainty);

    private static string ChooseComparisonUnit(
        ProtocolExposure exposure,
        IReadOnlyList<PublishedExposureRegimen> regimens)
    {
        // Prefer reviewed-evidence units as the comparison base so statements cite
        // amounts the way sources report them.
        var regimenUnit = NormalizeUnit(regimens[0].Unit);
        var exposureUnit = NormalizeUnit(exposure.Unit);
        if (UnitsCompatible(exposureUnit, regimenUnit))
        {
            return regimenUnit;
        }

        return exposureUnit;
    }

    private static decimal? ConvertOptional(decimal? amount, string fromUnit, string toUnit)
        => amount is null ? null : TryConvert(amount.Value, fromUnit, toUnit);

    private static decimal? TryConvert(decimal amount, string fromUnit, string toUnit)
    {
        var from = NormalizeUnit(fromUnit);
        var to = NormalizeUnit(toUnit);
        if (from == to)
        {
            return amount;
        }

        // Mass units only for foundation comparison.
        static decimal? ToMg(decimal value, string unit) => unit switch
        {
            "mg" => value,
            "mcg" or "ug" or "µg" => value / 1000m,
            "g" => value * 1000m,
            _ => null,
        };

        var mg = ToMg(amount, from);
        if (mg is null)
        {
            return null;
        }

        return to switch
        {
            "mg" => mg,
            "mcg" or "ug" or "µg" => mg * 1000m,
            "g" => mg / 1000m,
            _ => null,
        };
    }

    private static bool UnitsCompatible(string a, string b)
        => TryConvert(1m, a, b) is not null;

    private static string NormalizeUnit(string unit)
    {
        var u = unit.Trim().ToLowerInvariant();
        return u switch
        {
            "µg" or "ug" or "mcg" => "mcg",
            "milligram" or "milligrams" or "mg" => "mg",
            "gram" or "grams" or "g" => "g",
            _ => u,
        };
    }

    private static bool HasRouteMismatch(ProtocolExposure exposure, IReadOnlyList<PublishedExposureRegimen> regimens)
    {
        if (string.IsNullOrWhiteSpace(exposure.Route))
        {
            return false;
        }

        var studied = regimens
            .Select(r => r.Route)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r!.Trim())
            .ToList();
        if (studied.Count == 0)
        {
            return false;
        }

        return studied.All(r => !string.Equals(r, exposure.Route.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasFrequencyMismatch(ProtocolExposure exposure, IReadOnlyList<PublishedExposureRegimen> regimens)
    {
        if (string.IsNullOrWhiteSpace(exposure.Frequency))
        {
            return false;
        }

        var studied = regimens
            .Select(r => r.Frequency)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => NormalizeFrequency(f!))
            .ToList();
        if (studied.Count == 0)
        {
            return false;
        }

        var user = NormalizeFrequency(exposure.Frequency);
        return studied.All(f => f != user);
    }

    private static string NormalizeFrequency(string frequency)
    {
        var f = frequency.Trim().ToLowerInvariant();
        if (f.Contains("week", StringComparison.Ordinal))
        {
            return "weekly";
        }

        if (f.Contains("day", StringComparison.Ordinal) || f.Contains("daily", StringComparison.Ordinal))
        {
            return "daily";
        }

        return f;
    }

    private static string FrequencyClause(string? frequency)
        => string.IsNullOrWhiteSpace(frequency) ? string.Empty : $" {frequency.Trim()}";

    private static decimal RoundRatio(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static bool IsNearMultiple(decimal value, decimal basis, decimal multiple)
    {
        if (basis <= 0)
        {
            return false;
        }

        var target = basis * multiple;
        return Math.Abs(value - target) < 0.0000001m;
    }

    private static string Format(decimal? value)
        => value is null
            ? "?"
            : value.Value == decimal.Truncate(value.Value)
                ? decimal.Truncate(value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : value.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
