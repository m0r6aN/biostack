namespace BioStack.Api.Tests.Auth;

using BioStack.Api.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

public sealed class ProductionDataProtectionConfigurationTests
{
    [Fact]
    public void Validate_AllowsDevelopmentWithoutAzureSettings()
        => ProductionDataProtectionConfiguration.Validate(
            new ConfigurationBuilder().Build(),
            isProduction: false);

    [Theory]
    [InlineData(null, "https://storage.blob.core.windows.net/data-protection/key-ring.xml", "https://vault.vault.azure.net/keys/session-cookie")]
    [InlineData("11111111-1111-1111-1111-111111111111", "https://storage.blob.core.windows.net/data-protection/key-ring.xml", "https://vault.vault.azure.net/keys/session-cookie")]
    public void Validate_AcceptsSystemOrUserAssignedManagedIdentity(
        string? clientId,
        string blobUri,
        string keyVaultKeyIdentifier)
    {
        var configuration = Build(new Dictionary<string, string?>
        {
            ["DataProtection:ApplicationName"] = ProductionDataProtectionConfiguration.StableApplicationName,
            ["DataProtection:BlobUri"] = blobUri,
            ["DataProtection:KeyVaultKeyIdentifier"] = keyVaultKeyIdentifier,
            ["DataProtection:ManagedIdentityClientId"] = clientId,
        });

        ProductionDataProtectionConfiguration.Validate(configuration, isProduction: true);
    }

    [Theory]
    [InlineData(null, "https://storage.blob.core.windows.net/data-protection/key-ring.xml", "https://vault.vault.azure.net/keys/session-cookie")]
    [InlineData("changed-app-name", "https://storage.blob.core.windows.net/data-protection/key-ring.xml", "https://vault.vault.azure.net/keys/session-cookie")]
    [InlineData("BioStack.Api.SessionCookie.v1", "http://storage.blob.core.windows.net/data-protection/key-ring.xml", "https://vault.vault.azure.net/keys/session-cookie")]
    [InlineData("BioStack.Api.SessionCookie.v1", "https://storage.blob.core.windows.net/data-protection/key-ring.xml?sig=secret", "https://vault.vault.azure.net/keys/session-cookie")]
    [InlineData("BioStack.Api.SessionCookie.v1", "https://storage.blob.core.windows.net/data-protection", "https://vault.vault.azure.net/keys/session-cookie")]
    [InlineData("BioStack.Api.SessionCookie.v1", "https://storage.blob.core.windows.net/data-protection/key-ring.xml", "https://vault.vault.azure.net/keys/session-cookie/version")]
    [InlineData("BioStack.Api.SessionCookie.v1", "https://storage.blob.core.windows.net/data-protection/key-ring.xml", "http://vault.vault.azure.net/keys/session-cookie")]
    public void Validate_RejectsMissingOrUnsafeProductionSettings(
        string? applicationName,
        string blobUri,
        string keyVaultKeyIdentifier)
    {
        var configuration = Build(new Dictionary<string, string?>
        {
            ["DataProtection:ApplicationName"] = applicationName,
            ["DataProtection:BlobUri"] = blobUri,
            ["DataProtection:KeyVaultKeyIdentifier"] = keyVaultKeyIdentifier,
        });

        Assert.Throws<InvalidOperationException>(
            () => ProductionDataProtectionConfiguration.Validate(configuration, isProduction: true));
    }

    [Fact]
    public void Validate_RejectsMalformedUserAssignedManagedIdentityClientId()
    {
        var configuration = Build(new Dictionary<string, string?>
        {
            ["DataProtection:ApplicationName"] = ProductionDataProtectionConfiguration.StableApplicationName,
            ["DataProtection:BlobUri"] = "https://storage.blob.core.windows.net/data-protection/key-ring.xml",
            ["DataProtection:KeyVaultKeyIdentifier"] = "https://vault.vault.azure.net/keys/session-cookie",
            ["DataProtection:ManagedIdentityClientId"] = "not-a-guid",
        });

        Assert.Throws<InvalidOperationException>(
            () => ProductionDataProtectionConfiguration.Validate(configuration, isProduction: true));
    }

    private static IConfiguration Build(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
