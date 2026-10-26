namespace BioStack.Api.Tests.Security;

using BioStack.Api.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

public sealed class ClaimSubmissionSaltConfigurationTests
{
    private const string GoodSalt = "4f1c9a7e2b8d4e6fa3c5b7d9e1f20a4c6e8b0d2f4a6c8e0b";

    [Fact]
    public void Validate_AllowsMissingSaltOutsideProduction()
        => ClaimSubmissionSaltConfiguration.Validate(Build(null), isProduction: false);

    [Fact]
    public void Validate_AcceptsPrivateSaltInProduction()
        => ClaimSubmissionSaltConfiguration.Validate(Build(GoodSalt), isProduction: true);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsMissingSaltInProduction(string? salt)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ClaimSubmissionSaltConfiguration.Validate(Build(salt), isProduction: true));
        Assert.Contains("ClaimSubmissions:Salt must be set", ex.Message);
    }

    [Theory]
    [InlineData(ClaimSubmissionSaltConfiguration.DevelopmentDefault)]
    [InlineData("  BIOSTACK-CLAIM-SUBMISSIONS ")]
    public void Validate_RejectsPublicDefaultInProduction(string salt)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ClaimSubmissionSaltConfiguration.Validate(Build(salt), isProduction: true));
        Assert.Contains("public development default", ex.Message);
    }

    [Fact]
    public void Validate_RejectsShortSaltInProduction()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ClaimSubmissionSaltConfiguration.Validate(Build("too-short"), isProduction: true));
        Assert.Contains("at least 32", ex.Message);
    }

    [Fact]
    public void Resolve_UsesConfiguredSaltElseDevelopmentDefault()
    {
        Assert.Equal(GoodSalt, ClaimSubmissionSaltConfiguration.Resolve(Build($" {GoodSalt} ")));
        Assert.Equal(ClaimSubmissionSaltConfiguration.DevelopmentDefault, ClaimSubmissionSaltConfiguration.Resolve(Build(null)));
    }

    [Fact]
    public void DeployScript_WiresSaltFromKeyVaultReference()
    {
        var script = File.ReadAllText(Path.Combine(RepoRoot(), "infra", "azure", "deploy-container-apps.ps1"));
        Assert.Contains("claim-submissions-salt=keyvaultref:", script);
        Assert.Equal(2, CountOf(script, "\"ClaimSubmissions__Salt=secretref:claim-submissions-salt\""));
    }

    private static int CountOf(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal)) count++;
        return count;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "infra", "azure"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("repository root with infra/azure");
    }

    private static IConfiguration Build(string? salt)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ClaimSubmissionSaltConfiguration.ConfigurationKey] = salt })
            .Build();
}
