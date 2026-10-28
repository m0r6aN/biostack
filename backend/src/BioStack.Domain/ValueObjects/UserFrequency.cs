namespace BioStack.Domain.ValueObjects;

using System.Globalization;

/// <summary>What the user told us about how often they take a compound. Always the user's own entry.</summary>
public sealed record FrequencyInput(
    string? Kind,
    int? IntervalDays,
    IReadOnlyList<int>? Weekdays,
    int? TimesPerWeek,
    DateTime? StartDate,
    IReadOnlyList<string>? TimesOfDay);

/// <summary>The validated, normalised form of a <see cref="FrequencyInput"/>.</summary>
public sealed record FrequencySpec(
    string? Kind,
    int? IntervalDays,
    IReadOnlyList<int> Weekdays,
    int? TimesPerWeek,
    DateTime? StartDate,
    IReadOnlyList<string> TimesOfDay);

public enum FrequencyDue
{
    /// <summary>Not due on this date.</summary>
    NotDue,
    /// <summary>Due on this date (daily, every N days or a chosen weekday).</summary>
    Scheduled,
    /// <summary>"N times per week": a weekly target with no fixed day.</summary>
    WeeklyTarget,
    /// <summary>"As needed": never due on a particular day.</summary>
    AsNeeded,
}

/// <summary>
/// Structured frequency the user picks for a compound (daily, every N days, specific weekdays,
/// N times per week, as needed) plus an optional start date and times of day. Like <see cref="UserDose"/>
/// it only stores and echoes the user's choice: nothing here suggests a frequency, and a blank frequency
/// is never filled in. All due-day maths works on calendar dates, so daylight saving never shifts a day.
/// </summary>
public static class UserFrequency
{
    public const string Daily = "daily";
    public const string EveryNDays = "every_n_days";
    public const string SpecificWeekdays = "weekdays";
    public const string TimesPerWeek = "times_per_week";
    public const string AsNeeded = "as_needed";

    public static readonly IReadOnlyList<string> AllowedKinds = [Daily, EveryNDays, SpecificWeekdays, TimesPerWeek, AsNeeded];

    public const int MinIntervalDays = 2;
    public const int MaxIntervalDays = 365;
    public const int MaxTimesOfDay = 6;

    private static readonly string[] WeekdayShort = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    /// <summary>Returns an error (camelCase field plus message) or null; fills <paramref name="spec"/> when valid.</summary>
    public static (string Field, string Message)? Validate(FrequencyInput input, out FrequencySpec? spec)
    {
        spec = null;
        var kind = string.IsNullOrWhiteSpace(input.Kind) ? null : input.Kind.Trim().ToLowerInvariant();
        var weekdays = input.Weekdays ?? [];
        var times = input.TimesOfDay ?? [];

        if (kind is null)
        {
            if (input.IntervalDays is not null || weekdays.Count > 0 || input.TimesPerWeek is not null
                || input.StartDate is not null || times.Count > 0)
            {
                return ("frequencyKind", "Choose how often before adding days, a start date or times.");
            }

            return null;
        }

        if (!AllowedKinds.Contains(kind))
        {
            return ("frequencyKind", "Choose daily, every N days, specific weekdays, N times per week, or as needed.");
        }

        if (kind != EveryNDays && input.IntervalDays is not null)
        {
            return ("frequencyIntervalDays", "Number of days only applies to \"every N days\".");
        }

        if (kind != SpecificWeekdays && weekdays.Count > 0)
        {
            return ("frequencyWeekdays", "Weekdays only apply to \"specific weekdays\".");
        }

        if (kind != TimesPerWeek && input.TimesPerWeek is not null)
        {
            return ("frequencyTimesPerWeek", "Times per week only applies to \"N times per week\".");
        }

        if (kind == AsNeeded && (input.StartDate is not null || times.Count > 0))
        {
            return ("frequencyKind", "\"As needed\" has no start date or times of day.");
        }

        int? interval = null;
        if (kind == EveryNDays)
        {
            if (input.IntervalDays is not { } days || days < MinIntervalDays || days > MaxIntervalDays)
            {
                return ("frequencyIntervalDays", $"Enter a whole number of days from {MinIntervalDays} to {MaxIntervalDays}. Use daily for every day.");
            }

            interval = days;
        }

        var normalizedWeekdays = new List<int>();
        if (kind == SpecificWeekdays)
        {
            if (weekdays.Count == 0)
            {
                return ("frequencyWeekdays", "Choose at least one weekday.");
            }

            if (weekdays.Any(day => day < 0 || day > 6))
            {
                return ("frequencyWeekdays", "Weekdays must be 0 (Sunday) to 6 (Saturday).");
            }

            normalizedWeekdays = weekdays.Distinct().OrderBy(day => day).ToList();
        }

        int? perWeek = null;
        if (kind == TimesPerWeek)
        {
            if (input.TimesPerWeek is not { } count || count < 1 || count > 7)
            {
                return ("frequencyTimesPerWeek", "Enter a whole number of times per week from 1 to 7.");
            }

            perWeek = count;
        }

        if (times.Count > MaxTimesOfDay)
        {
            return ("frequencyTimesOfDay", $"Enter at most {MaxTimesOfDay} times of day.");
        }

        var normalizedTimes = new List<string>();
        foreach (var time in times)
        {
            if (!TimeOnly.TryParseExact(time?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return ("frequencyTimesOfDay", "Enter each time of day as HH:mm (24-hour).");
            }

            normalizedTimes.Add(parsed.ToString("HH:mm", CultureInfo.InvariantCulture));
        }

        var start = input.StartDate is null ? (DateTime?)null : DateTime.SpecifyKind(input.StartDate.Value.Date, DateTimeKind.Utc);

        spec = new FrequencySpec(
            kind,
            interval,
            normalizedWeekdays,
            perWeek,
            start,
            normalizedTimes.Distinct().OrderBy(time => time, StringComparer.Ordinal).ToList());
        return null;
    }

    /// <summary>Decides whether the compound is due on a calendar date. Pure date maths, so DST never shifts a day.</summary>
    public static FrequencyDue GetDue(
        DateTime date,
        string? kind,
        int? intervalDays,
        IReadOnlyCollection<int> weekdays,
        DateTime? frequencyStartDate,
        DateTime? compoundStartDate,
        DateTime? compoundEndDate,
        DateTime createdAtUtc)
    {
        var day = date.Date;
        if (compoundStartDate is { } start && day < start.Date)
        {
            return FrequencyDue.NotDue;
        }

        if (compoundEndDate is { } end && day > end.Date)
        {
            return FrequencyDue.NotDue;
        }

        if (kind == AsNeeded)
        {
            return FrequencyDue.AsNeeded;
        }

        if (frequencyStartDate is { } scheduleStart && day < scheduleStart.Date)
        {
            return FrequencyDue.NotDue;
        }

        switch (kind)
        {
            case EveryNDays when intervalDays is >= 1:
            {
                var anchor = (frequencyStartDate ?? compoundStartDate ?? createdAtUtc).Date;
                if (day < anchor)
                {
                    return FrequencyDue.NotDue;
                }

                return (day - anchor).Days % intervalDays.Value == 0 ? FrequencyDue.Scheduled : FrequencyDue.NotDue;
            }
            case SpecificWeekdays:
                return weekdays.Contains((int)day.DayOfWeek) ? FrequencyDue.Scheduled : FrequencyDue.NotDue;
            case TimesPerWeek:
                return FrequencyDue.WeeklyTarget;
            default:
                // No structured frequency (older rows) or daily: shown every day, as before.
                return FrequencyDue.Scheduled;
        }
    }

    /// <summary>Short label for the user's structured choice, e.g. "Mon, Thu" or "Every 3 days". Null when none was chosen.</summary>
    public static string? Describe(string? kind, int? intervalDays, IReadOnlyCollection<int> weekdays, int? timesPerWeek)
    {
        return kind switch
        {
            Daily => "Every day",
            EveryNDays when intervalDays is { } days => $"Every {days} days",
            SpecificWeekdays when weekdays.Count > 0 => string.Join(", ", weekdays
                .OrderBy(day => day == 0 ? 7 : day)
                .Select(day => WeekdayShort[day])),
            TimesPerWeek when timesPerWeek is { } n => n == 1 ? "1 time per week (weekly target)" : $"{n} times per week (weekly target)",
            AsNeeded => "As needed",
            _ => null,
        };
    }

    public static IReadOnlyList<int> ParseWeekdays(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var day) ? day : -1)
                .Where(day => day is >= 0 and <= 6)
                .Distinct()
                .OrderBy(day => day)
                .ToList();

    public static string? FormatWeekdays(IReadOnlyCollection<int> weekdays)
        => weekdays.Count == 0 ? null : string.Join(',', weekdays.Select(day => day.ToString(CultureInfo.InvariantCulture)));

    public static IReadOnlyList<string> ParseTimes(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    public static string? FormatTimes(IReadOnlyCollection<string> times)
        => times.Count == 0 ? null : string.Join(',', times);

    /// <summary>
    /// The user's calendar date "today" in their time zone (an IANA id the browser reports, or any id the
    /// host knows). Falls back to the UTC date when no usable zone is given.
    /// </summary>
    public static DateTime ResolveToday(DateTime utcNow, string? timeZoneId)
    {
        var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
                return TimeZoneInfo.ConvertTimeFromUtc(utc, zone).Date;
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return utc.Date;
    }
}
