namespace BioStack.Domain.Tests.ValueObjects;

using BioStack.Domain.ValueObjects;
using Xunit;

public sealed class UserFrequencyTests
{
    private static readonly DateTime Created = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static FrequencyDue Due(
        DateTime date,
        string? kind,
        int? interval = null,
        int[]? weekdays = null,
        DateTime? frequencyStart = null,
        DateTime? start = null,
        DateTime? end = null)
        => UserFrequency.GetDue(date, kind, interval, weekdays ?? [], frequencyStart, start, end, Created);

    // ── validation ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_BlankKindWithNothingElse_IsValidAndStaysBlank()
    {
        var error = UserFrequency.Validate(new FrequencyInput(null, null, null, null, null, null), out var spec);

        Assert.Null(error);
        Assert.Null(spec);
    }

    [Fact]
    public void Validate_BlankKindWithWeekdays_IsRejected_NothingIsInferred()
    {
        var error = UserFrequency.Validate(new FrequencyInput("  ", null, [1, 4], null, null, null), out var spec);

        Assert.Equal("frequencyKind", error?.Field);
        Assert.Null(spec);
    }

    [Fact]
    public void Validate_SpecificWeekdays_SortsAndDeduplicates()
    {
        var error = UserFrequency.Validate(new FrequencyInput("Weekdays", null, [4, 1, 4], null, null, null), out var spec);

        Assert.Null(error);
        Assert.Equal("weekdays", spec!.Kind);
        Assert.Equal([1, 4], spec.Weekdays);
    }

    [Theory]
    [InlineData("weekly")]
    [InlineData("twice weekly")]
    [InlineData("monthly")]
    public void Validate_UnknownKind_IsRejected(string kind)
    {
        var error = UserFrequency.Validate(new FrequencyInput(kind, null, null, null, null, null), out _);

        Assert.Equal("frequencyKind", error?.Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(366)]
    [InlineData(-3)]
    public void Validate_EveryNDays_OutsideTwoToThreeSixtyFive_IsRejected(int days)
    {
        var error = UserFrequency.Validate(new FrequencyInput("every_n_days", days, null, null, null, null), out _);

        Assert.Equal("frequencyIntervalDays", error?.Field);
    }

    [Fact]
    public void Validate_EveryNDays_WithoutInterval_IsRejected()
    {
        var error = UserFrequency.Validate(new FrequencyInput("every_n_days", null, null, null, null, null), out _);

        Assert.Equal("frequencyIntervalDays", error?.Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Validate_TimesPerWeek_OutsideOneToSeven_IsRejected(int count)
    {
        var error = UserFrequency.Validate(new FrequencyInput("times_per_week", null, null, count, null, null), out _);

        Assert.Equal("frequencyTimesPerWeek", error?.Field);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Validate_WeekdayOutOfRange_IsRejected(int day)
    {
        var error = UserFrequency.Validate(new FrequencyInput("weekdays", null, [1, day], null, null, null), out _);

        Assert.Equal("frequencyWeekdays", error?.Field);
    }

    [Fact]
    public void Validate_WeekdaysKindWithNoDays_IsRejected()
    {
        var error = UserFrequency.Validate(new FrequencyInput("weekdays", null, [], null, null, null), out _);

        Assert.Equal("frequencyWeekdays", error?.Field);
    }

    [Fact]
    public void Validate_FieldsThatBelongToAnotherKind_AreRejected()
    {
        Assert.Equal("frequencyWeekdays", UserFrequency.Validate(new FrequencyInput("daily", null, [1], null, null, null), out _)?.Field);
        Assert.Equal("frequencyIntervalDays", UserFrequency.Validate(new FrequencyInput("weekdays", 3, [1], null, null, null), out _)?.Field);
        Assert.Equal("frequencyTimesPerWeek", UserFrequency.Validate(new FrequencyInput("daily", null, null, 2, null, null), out _)?.Field);
    }

    [Fact]
    public void Validate_AsNeeded_AllowsNoStartDateOrTimes()
    {
        Assert.Null(UserFrequency.Validate(new FrequencyInput("as_needed", null, null, null, null, null), out _));
        Assert.Equal("frequencyKind", UserFrequency.Validate(new FrequencyInput("as_needed", null, null, null, new DateTime(2026, 1, 1), null), out _)?.Field);
        Assert.Equal("frequencyKind", UserFrequency.Validate(new FrequencyInput("as_needed", null, null, null, null, ["08:00"]), out _)?.Field);
    }

    [Fact]
    public void Validate_TimesOfDay_AreNormalisedSortedAndDeduplicated()
    {
        var error = UserFrequency.Validate(
            new FrequencyInput("daily", null, null, null, new DateTime(2026, 3, 8, 17, 45, 0, DateTimeKind.Local), ["20:00", "08:00", "08:00"]),
            out var spec);

        Assert.Null(error);
        Assert.Equal(["08:00", "20:00"], spec!.TimesOfDay);
        Assert.Equal(new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc), spec.StartDate);
    }

    [Theory]
    [InlineData("8am")]
    [InlineData("24:00")]
    [InlineData("12:60")]
    [InlineData("")]
    [InlineData("morning")]
    public void Validate_BadTimeOfDay_IsRejected(string time)
    {
        var error = UserFrequency.Validate(new FrequencyInput("daily", null, null, null, null, [time]), out _);

        Assert.Equal("frequencyTimesOfDay", error?.Field);
    }

    [Fact]
    public void Validate_TooManyTimesOfDay_IsRejected()
    {
        var times = Enumerable.Range(0, 7).Select(h => $"{h:00}:00").ToList();

        var error = UserFrequency.Validate(new FrequencyInput("daily", null, null, null, null, times), out _);

        Assert.Equal("frequencyTimesOfDay", error?.Field);
    }

    // ── due days ────────────────────────────────────────────────────────────

    [Fact]
    public void GetDue_NoStructuredFrequency_IsDailyAsBefore()
    {
        for (var offset = 0; offset < 10; offset++)
        {
            Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 10, 1).AddDays(offset), null));
        }
    }

    [Fact]
    public void GetDue_SpecificWeekdays_MondayAndThursdayOnly()
    {
        var monday = new DateTime(2026, 10, 5);
        var expected = new Dictionary<DayOfWeek, FrequencyDue>
        {
            [DayOfWeek.Monday] = FrequencyDue.Scheduled,
            [DayOfWeek.Tuesday] = FrequencyDue.NotDue,
            [DayOfWeek.Wednesday] = FrequencyDue.NotDue,
            [DayOfWeek.Thursday] = FrequencyDue.Scheduled,
            [DayOfWeek.Friday] = FrequencyDue.NotDue,
            [DayOfWeek.Saturday] = FrequencyDue.NotDue,
            [DayOfWeek.Sunday] = FrequencyDue.NotDue,
        };

        for (var offset = 0; offset < 7; offset++)
        {
            var date = monday.AddDays(offset);
            Assert.Equal(expected[date.DayOfWeek], Due(date, "weekdays", weekdays: [1, 4]));
        }
    }

    [Fact]
    public void GetDue_SpecificWeekdays_AcrossNewYear_StaysOnTheSameWeekdays()
    {
        // 2026-12-31 is a Thursday and 2027-01-04 a Monday.
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 12, 31), "weekdays", weekdays: [1, 4]));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2027, 1, 1), "weekdays", weekdays: [1, 4]));
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2027, 1, 4), "weekdays", weekdays: [1, 4]));
    }

    [Fact]
    public void GetDue_EveryTwoDays_AcrossUsSpringForward_NeverShiftsADay()
    {
        // US clocks go forward on 2026-03-08. Calendar-date maths ignores the missing hour.
        var anchor = new DateTime(2026, 3, 5);
        var due = Enumerable.Range(0, 8)
            .Where(offset => Due(anchor.AddDays(offset), "every_n_days", 2, frequencyStart: anchor) == FrequencyDue.Scheduled)
            .Select(offset => anchor.AddDays(offset).Day)
            .ToList();

        Assert.Equal([5, 7, 9, 11], due);
    }

    [Fact]
    public void GetDue_EveryTwoDays_AcrossUsFallBack_NeverRepeatsADay()
    {
        // US clocks go back on 2026-11-01 (a 25-hour day).
        var anchor = new DateTime(2026, 10, 30);
        var due = Enumerable.Range(0, 6)
            .Where(offset => Due(anchor.AddDays(offset), "every_n_days", 2, frequencyStart: anchor) == FrequencyDue.Scheduled)
            .Select(offset => anchor.AddDays(offset))
            .ToList();

        Assert.Equal([new DateTime(2026, 10, 30), new DateTime(2026, 11, 1), new DateTime(2026, 11, 3)], due);
    }

    [Fact]
    public void GetDue_EveryTwoDays_AcrossEuropeanDstChanges_StaysOnTheCalendarGrid()
    {
        // EU clocks: forward 2026-03-29, back 2026-10-25.
        var springAnchor = new DateTime(2026, 3, 27);
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 3, 29), "every_n_days", 2, frequencyStart: springAnchor));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 3, 30), "every_n_days", 2, frequencyStart: springAnchor));
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 3, 31), "every_n_days", 2, frequencyStart: springAnchor));

        var fallAnchor = new DateTime(2026, 10, 23);
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 10, 25), "every_n_days", 2, frequencyStart: fallAnchor));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 10, 26), "every_n_days", 2, frequencyStart: fallAnchor));
    }

    [Fact]
    public void GetDue_EveryThreeDays_AcrossMonthBoundary()
    {
        var anchor = new DateTime(2026, 1, 30);
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 1, 30), "every_n_days", 3, frequencyStart: anchor));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 1, 31), "every_n_days", 3, frequencyStart: anchor));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 2, 1), "every_n_days", 3, frequencyStart: anchor));
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 2, 2), "every_n_days", 3, frequencyStart: anchor));
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 2, 5), "every_n_days", 3, frequencyStart: anchor));
    }

    [Fact]
    public void GetDue_EverySevenDays_AcrossLeapDay()
    {
        var anchor = new DateTime(2028, 2, 24);
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2028, 3, 2), "every_n_days", 7, frequencyStart: anchor));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2028, 3, 1), "every_n_days", 7, frequencyStart: anchor));
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2028, 3, 9), "every_n_days", 7, frequencyStart: anchor));
    }

    [Fact]
    public void GetDue_EveryNDays_WithoutAFrequencyStart_AnchorsOnTheCompoundStart_ThenCreationDate()
    {
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 5, 3), "every_n_days", 2, start: new DateTime(2026, 5, 1)));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 5, 2), "every_n_days", 2, start: new DateTime(2026, 5, 1)));
        // Created 2026-01-01: 2026-01-03 is two days later.
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 1, 3), "every_n_days", 2));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 1, 4), "every_n_days", 2));
    }

    [Fact]
    public void GetDue_NothingIsDueBeforeTheFrequencyStartDate()
    {
        var start = new DateTime(2026, 10, 12);
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 10, 11), "daily", frequencyStart: start));
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 10, 12), "daily", frequencyStart: start));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 10, 5), "weekdays", weekdays: [1], frequencyStart: start));
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 10, 12), "weekdays", weekdays: [1], frequencyStart: start));
    }

    [Fact]
    public void GetDue_RespectsTheCompoundStartAndEndDates()
    {
        var start = new DateTime(2026, 10, 5);
        var end = new DateTime(2026, 10, 9);
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 10, 4), null, start: start, end: end));
        Assert.Equal(FrequencyDue.Scheduled, Due(new DateTime(2026, 10, 9), null, start: start, end: end));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 10, 10), null, start: start, end: end));
        Assert.Equal(FrequencyDue.NotDue, Due(new DateTime(2026, 10, 10), "as_needed", start: start, end: end));
    }

    [Fact]
    public void GetDue_TimesPerWeek_IsAWeeklyTarget_NotAFixedDay()
    {
        for (var offset = 0; offset < 7; offset++)
        {
            Assert.Equal(FrequencyDue.WeeklyTarget, Due(new DateTime(2026, 10, 5).AddDays(offset), "times_per_week"));
        }
    }

    [Fact]
    public void GetDue_AsNeeded_IsNeverScheduledForADay()
    {
        Assert.Equal(FrequencyDue.AsNeeded, Due(new DateTime(2026, 10, 5), "as_needed"));
    }

    // ── labels and parsing ──────────────────────────────────────────────────

    [Theory]
    [InlineData("daily", null, "Every day")]
    [InlineData("as_needed", null, "As needed")]
    [InlineData("every_n_days", 3, "Every 3 days")]
    public void Describe_UsesTheUsersChoice(string kind, int? interval, string expected)
        => Assert.Equal(expected, UserFrequency.Describe(kind, interval, [], null));

    [Fact]
    public void Describe_Weekdays_ListsMondayFirstAndSundayLast()
        => Assert.Equal("Mon, Thu, Sun", UserFrequency.Describe("weekdays", null, [0, 1, 4], null));

    [Fact]
    public void Describe_TimesPerWeek_IsLabelledAsAWeeklyTarget()
    {
        Assert.Equal("2 times per week (weekly target)", UserFrequency.Describe("times_per_week", null, [], 2));
        Assert.Equal("1 time per week (weekly target)", UserFrequency.Describe("times_per_week", null, [], 1));
    }

    [Fact]
    public void Describe_NoStructuredChoice_ReturnsNull()
        => Assert.Null(UserFrequency.Describe(null, null, [], null));

    [Fact]
    public void WeekdaysAndTimes_RoundTripThroughTheirStoredText()
    {
        Assert.Equal("1,4", UserFrequency.FormatWeekdays([1, 4]));
        Assert.Equal([1, 4], UserFrequency.ParseWeekdays("4, 1,4,9,x"));
        Assert.Null(UserFrequency.FormatWeekdays([]));
        Assert.Empty(UserFrequency.ParseWeekdays(null));
        Assert.Equal("08:00,20:00", UserFrequency.FormatTimes(["08:00", "20:00"]));
        Assert.Equal(["08:00", "20:00"], UserFrequency.ParseTimes("08:00,20:00"));
        Assert.Empty(UserFrequency.ParseTimes(""));
    }

    // ── "today" in the user's time zone ─────────────────────────────────────

    [Fact]
    public void ResolveToday_UsesTheUsersTimeZoneDate()
    {
        var utc = new DateTime(2026, 10, 28, 3, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 10, 27), UserFrequency.ResolveToday(utc, "America/New_York"));
        Assert.Equal(new DateTime(2026, 10, 28), UserFrequency.ResolveToday(utc, "Europe/London"));
        Assert.Equal(new DateTime(2026, 10, 28), UserFrequency.ResolveToday(utc, null));
        // Ahead of UTC: 20:00Z on the 27th is already the 28th in Auckland (NZDT, UTC+13).
        Assert.Equal(new DateTime(2026, 10, 28), UserFrequency.ResolveToday(new DateTime(2026, 10, 27, 20, 0, 0, DateTimeKind.Utc), "Pacific/Auckland"));
    }

    [Fact]
    public void ResolveToday_AroundTheUsFallBackHour_StaysOnTheRightCalendarDay()
    {
        // 05:30Z is 01:30 EDT, 06:30Z is 01:30 EST (the repeated hour): both are still 1 November.
        Assert.Equal(new DateTime(2026, 11, 1), UserFrequency.ResolveToday(new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc), "America/New_York"));
        Assert.Equal(new DateTime(2026, 11, 1), UserFrequency.ResolveToday(new DateTime(2026, 11, 1, 6, 30, 0, DateTimeKind.Utc), "America/New_York"));
        Assert.Equal(new DateTime(2026, 10, 31), UserFrequency.ResolveToday(new DateTime(2026, 11, 1, 3, 30, 0, DateTimeKind.Utc), "America/New_York"));
    }

    [Fact]
    public void ResolveToday_AroundTheUsSpringForwardGap_StaysOnTheRightCalendarDay()
    {
        // 02:00 EST on 2026-03-08 does not exist; 07:30Z is 03:30 EDT.
        Assert.Equal(new DateTime(2026, 3, 8), UserFrequency.ResolveToday(new DateTime(2026, 3, 8, 7, 30, 0, DateTimeKind.Utc), "America/New_York"));
        Assert.Equal(new DateTime(2026, 3, 7), UserFrequency.ResolveToday(new DateTime(2026, 3, 8, 4, 59, 0, DateTimeKind.Utc), "America/New_York"));
    }

    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveToday_UnknownOrBlankZone_FallsBackToTheUtcDate(string? zone)
    {
        var utc = new DateTime(2026, 10, 28, 3, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 10, 28), UserFrequency.ResolveToday(utc, zone));
    }
}
