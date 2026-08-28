namespace BioStack.Api.Tests.Auth;

using BioStack.Api.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

public sealed class PasskeyFeatureConfigurationTests
{
    [Fact]
    public void DisabledGate_DoesNotRequireProductionPrerequisites()
    {
        var options = Load(new Dictionary<string, string?>
        {
            ["Auth:Passkeys:Enabled"] = "false",
        });

        PasskeyFeatureConfiguration.Validate(options, isProduction: true, frontendUrl: null);
        Assert.False(options.Enabled);
    }

    [Fact]
    public void ProductionGate_AcceptsExactHttpsOriginAndParentRpId()
    {
        var options = Load(new Dictionary<string, string?>
        {
            ["Auth:Passkeys:Enabled"] = "true",
            ["Auth:Passkeys:RpId"] = "biostack.cc",
            ["Auth:Passkeys:ServerName"] = "BioStack",
            ["Auth:Passkeys:Origins:0"] = "https://app.biostack.cc",
        });

        PasskeyFeatureConfiguration.Validate(options, isProduction: true, "https://app.biostack.cc");
        Assert.Contains("https://app.biostack.cc", options.Origins);
    }

    [Theory]
    [InlineData("http://biostack.cc", "https://biostack.cc")]
    [InlineData("https://evil.example", "https://evil.example")]
    [InlineData("https://biostack.cc/path", "https://biostack.cc")]
    public void ProductionGate_RejectsUnsafeOrMismatchedOrigins(string origin, string frontendUrl)
    {
        var options = Load(new Dictionary<string, string?>
        {
            ["Auth:Passkeys:Enabled"] = "true",
            ["Auth:Passkeys:RpId"] = "biostack.cc",
            ["Auth:Passkeys:ServerName"] = "BioStack",
            ["Auth:Passkeys:Origins:0"] = origin,
        });

        Assert.Throws<InvalidOperationException>(() =>
            PasskeyFeatureConfiguration.Validate(options, isProduction: true, frontendUrl));
    }

    [Fact]
    public void ProductionGate_RejectsFrontendOriginNotInExactAllowlist()
    {
        var options = Load(new Dictionary<string, string?>
        {
            ["Auth:Passkeys:Enabled"] = "true",
            ["Auth:Passkeys:RpId"] = "biostack.cc",
            ["Auth:Passkeys:ServerName"] = "BioStack",
            ["Auth:Passkeys:Origins:0"] = "https://biostack.cc",
        });

        Assert.Throws<InvalidOperationException>(() =>
            PasskeyFeatureConfiguration.Validate(options, isProduction: true, "https://app.biostack.cc"));
    }

    private static PasskeyFeatureConfiguration Load(Dictionary<string, string?> values) =>
        PasskeyFeatureConfiguration.Load(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
}
