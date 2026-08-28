namespace BioStack.Api.Auth;

using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;

public static class ProductionDataProtectionConfiguration
{
    public const string StableApplicationName = "BioStack.Api.SessionCookie.v1";

    public static void Configure(
        IServiceCollection services,
        IConfiguration configuration,
        bool isProduction)
    {
        var settings = isProduction ? ReadAndValidate(configuration) : null;
        var applicationName = configuration["DataProtection:ApplicationName"];
        var dataProtection = services
            .AddDataProtection()
            .SetApplicationName(settings?.ApplicationName ??
                (string.IsNullOrWhiteSpace(applicationName)
                    ? StableApplicationName
                    : applicationName.Trim()));

        if (settings is null)
        {
            return;
        }

        var credential = CreateManagedIdentityCredential(settings.ManagedIdentityClientId);

        dataProtection
            .PersistKeysToAzureBlobStorage(settings.BlobUri, credential)
            .ProtectKeysWithAzureKeyVault(settings.KeyVaultKeyIdentifier, credential);
    }

    public static void Validate(IConfiguration configuration, bool isProduction)
    {
        if (isProduction)
        {
            _ = ReadAndValidate(configuration);
        }
    }

    private static Settings ReadAndValidate(IConfiguration configuration)
    {
        var applicationName = configuration["DataProtection:ApplicationName"]?.Trim();
        if (!string.Equals(applicationName, StableApplicationName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"DataProtection:ApplicationName must be the stable value '{StableApplicationName}' in Production.");
        }

        var blobUri = RequireHttpsUri(configuration["DataProtection:BlobUri"], "DataProtection:BlobUri");
        if (!string.IsNullOrEmpty(blobUri.Query) ||
            blobUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length < 2)
        {
            throw new InvalidOperationException(
                "DataProtection:BlobUri must identify one Blob object and must not contain a SAS token or query string.");
        }

        var keyVaultKeyIdentifier = RequireHttpsUri(
            configuration["DataProtection:KeyVaultKeyIdentifier"],
            "DataProtection:KeyVaultKeyIdentifier");
        var keyPath = keyVaultKeyIdentifier.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (keyPath.Length != 2 ||
            !keyPath[0].Equals("keys", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(keyPath[1]))
        {
            throw new InvalidOperationException(
                "DataProtection:KeyVaultKeyIdentifier must be a versionless HTTPS key URI in the form https://<vault>/keys/<key-name>.");
        }

        var clientId = configuration["DataProtection:ManagedIdentityClientId"]?.Trim();
        if (!string.IsNullOrEmpty(clientId) && !Guid.TryParse(clientId, out _))
        {
            throw new InvalidOperationException(
                "DataProtection:ManagedIdentityClientId must be a user-assigned managed identity client ID GUID when configured.");
        }

        return new Settings(blobUri, keyVaultKeyIdentifier, StableApplicationName, clientId);
    }

    private static Uri RequireHttpsUri(string? value, string key)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException(
                $"{key} must be an absolute HTTPS URI with no credentials, query, or fragment.");
        }

        return uri;
    }

    private static TokenCredential CreateManagedIdentityCredential(string? clientId)
        => new ManagedIdentityCredential(
            string.IsNullOrWhiteSpace(clientId)
                ? ManagedIdentityId.SystemAssigned
                : ManagedIdentityId.FromUserAssignedClientId(clientId));

    private sealed record Settings(
        Uri BlobUri,
        Uri KeyVaultKeyIdentifier,
        string ApplicationName,
        string? ManagedIdentityClientId);
}
