namespace BioStack.Domain.Tests.ValueObjects;

using BioStack.Domain.ValueObjects;
using Xunit;

/// <summary>
/// The dose is the user's own entry. These tests pin that validation only checks "positive, storable
/// number in an allowed unit" and never grows a recommended range, a prefill or a computed value.
/// </summary>
public sealed class UserDoseTests
{
    private static (string Field, string Message)? Validate(
        decimal? amount, string? unit, string? frequency = null, string? time = null)
        => UserDose.Validate(amount, unit, frequency, time, out _, out _, out _, out _);

    [Fact]
    public void AllowedUnits_AreExactlyTheAgreedSet()
    {
        Assert.Equal(new[] { "mcg", "mg", "g", "IU", "mL", "units" }, UserDose.AllowedUnits);
    }

    [Fact]
    public void EmptyEntry_IsValidAndStaysBlank()
    {
        var error = UserDose.Validate(null, null, null, null, out var amount, out var unit, out var frequency, out var time);

        Assert.Null(error);
        Assert.Null(amount);
        Assert.Null(unit);
        Assert.Null(frequency);
        Assert.Null(time);
    }

    [Fact]
    public void WhitespaceOnlyEntry_IsTreatedAsBlank()
    {
        var error = UserDose.Validate(null, "  ", "   ", "\t", out var amount, out var unit, out var frequency, out var time);

        Assert.Null(error);
        Assert.Null(amount);
        Assert.Null(unit);
        Assert.Null(frequency);
        Assert.Null(time);
    }

    [Fact]
    public void TheCanonicalExample_250McgTwiceWeekly_IsAcceptedVerbatim()
    {
        var error = UserDose.Validate(250m, "mcg", "twice weekly", null, out var amount, out var unit, out var frequency, out _);

        Assert.Null(error);
        Assert.Equal(250m, amount);
        Assert.Equal("mcg", unit);
        Assert.Equal("twice weekly", frequency);
    }

    [Theory]
    [InlineData("mcg", "mcg")]
    [InlineData("MCG", "mcg")]
    [InlineData(" mg ", "mg")]
    [InlineData("G", "g")]
    [InlineData("iu", "IU")]
    [InlineData("ML", "mL")]
    [InlineData("Units", "units")]
    public void Unit_IsNormalisedToCanonicalSpelling(string input, string expected)
    {
        var error = UserDose.Validate(1m, input, null, null, out _, out var unit, out _, out _);

        Assert.Null(error);
        Assert.Equal(expected, unit);
    }

    [Theory]
    [InlineData("kg")]
    [InlineData("tablet")]
    [InlineData("ug")]
    [InlineData("cc")]
    public void Unit_OutsideTheAllowedSet_IsRejected(string unit)
    {
        var error = Validate(5m, unit);

        Assert.NotNull(error);
        Assert.Equal("doseUnit", error!.Value.Field);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-0.0001")]
    public void Amount_ZeroOrNegative_IsRejected(string amount)
    {
        var error = Validate(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "mg");

        Assert.NotNull(error);
        Assert.Equal("doseAmount", error!.Value.Field);
    }

    [Theory]
    [InlineData("0.0001")]
    [InlineData("0.5")]
    [InlineData("250")]
    [InlineData("100000")]
    [InlineData("99999999999999.9999")]
    public void Amount_AnyPositiveStorableNumber_IsAccepted_WithNoRangeLimits(string amount)
    {
        // No minimum or maximum "reasonable dose" exists on purpose: a threshold would read as a
        // recommended range. Only what the column can store is enforced.
        var error = Validate(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "mcg");

        Assert.Null(error);
    }

    [Fact]
    public void Amount_BeyondWhatTheColumnCanStore_IsRejectedAsTooLarge()
    {
        var error = Validate(100_000_000_000_000m, "mg");

        Assert.NotNull(error);
        Assert.Equal("doseAmount", error!.Value.Field);
        Assert.Contains("too large to store", error.Value.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Amount_WithMoreThanFourDecimalPlaces_IsRejectedInsteadOfSilentlyRounded()
    {
        var error = Validate(0.00001m, "mg");

        Assert.NotNull(error);
        Assert.Equal("doseAmount", error!.Value.Field);
    }

    [Fact]
    public void Amount_WithoutUnit_IsRejected()
    {
        var error = Validate(250m, null);

        Assert.NotNull(error);
        Assert.Equal("doseUnit", error!.Value.Field);
    }

    [Fact]
    public void Unit_WithoutAmount_IsRejected()
    {
        var error = Validate(null, "mg");

        Assert.NotNull(error);
        Assert.Equal("doseAmount", error!.Value.Field);
    }

    [Fact]
    public void FrequencyAndTime_CanBeEnteredWithoutAnAmount()
    {
        var error = UserDose.Validate(null, null, "every other day", "evenings", out var amount, out _, out var frequency, out var time);

        Assert.Null(error);
        Assert.Null(amount);
        Assert.Equal("every other day", frequency);
        Assert.Equal("evenings", time);
    }

    [Fact]
    public void FrequencyAndTime_AreTrimmed_AndLengthCapped()
    {
        Assert.Null(UserDose.Validate(null, null, new string('a', UserDose.FrequencyMaxLength), null, out _, out _, out _, out _));
        Assert.Equal("doseFrequency", Validate(null, null, new string('a', UserDose.FrequencyMaxLength + 1))!.Value.Field);
        Assert.Equal("doseTime", Validate(null, null, null, new string('a', UserDose.TimeMaxLength + 1))!.Value.Field);

        UserDose.Validate(null, null, "  weekly  ", "  8 AM  ", out _, out _, out var frequency, out var time);
        Assert.Equal("weekly", frequency);
        Assert.Equal("8 AM", time);
    }

    [Theory]
    [InlineData("250", "mcg", "250 mcg")]
    [InlineData("0.5", "mL", "0.5 mL")]
    [InlineData("2.5000", "mg", "2.5 mg")]
    [InlineData("1000", "IU", "1000 IU")]
    public void FormatAmount_EchoesTheUsersEntryWithoutScientificNotation(string amount, string unit, string expected)
    {
        Assert.Equal(expected, UserDose.FormatAmount(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), unit));
    }

    [Fact]
    public void FormatAmount_ReturnsNull_WhenThereIsNothingToEcho()
    {
        Assert.Null(UserDose.FormatAmount(null, "mg"));
        Assert.Null(UserDose.FormatAmount(5m, null));
    }

    [Fact]
    public void ValidationMessages_NeverImplyARecommendedDose()
    {
        // Every message the validator can emit, collected by driving each failure path.
        var messages = new List<string>
        {
            Validate(0m, "mg")!.Value.Message,
            Validate(100_000_000_000_000m, "mg")!.Value.Message,
            Validate(0.00001m, "mg")!.Value.Message,
            Validate(5m, null)!.Value.Message,
            Validate(null, "mg")!.Value.Message,
            Validate(5m, "kg")!.Value.Message,
            Validate(null, null, new string('a', 101))!.Value.Message,
            Validate(null, null, null, new string('a', 101))!.Value.Message,
        };

        string[] bannedWords = ["recommend", "typical", "usual", "safe", "unsafe", "too high", "too low", "normal", "range", "should", "suggest", "consult", "warning"];
        foreach (var message in messages)
        {
            foreach (var word in bannedWords)
            {
                Assert.DoesNotContain(word, message, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
