namespace BioStack.Api.Security;

/// <summary>
/// The salt for claim-submission submitter hashes (ledger gate G2). The submitter key is a
/// SHA-256 of salt + (user id, or client address + UTC day), so a public or missing salt lets
/// anyone reverse an anonymous submitter's address by brute force over the address space.
/// Production refuses to start without a private salt.
/// </summary>
public static class ClaimSubmissionSaltConfiguration
{
    public const string ConfigurationKey = "ClaimSubmissions:Salt";

    /// <summary>Development-only fallback. Public in source, so never accepted in Production.</summary>
    public const string DevelopmentDefault = "biostack-claim-submissions";

    public const int MinimumLength = 32;

    public static void Validate(IConfiguration configuration, bool isProduction)
    {
        if (!isProduction)
        {
            return;
        }

        var salt = configuration[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(salt))
        {
            throw new InvalidOperationException(
                $"{ConfigurationKey} must be set in Production (env ClaimSubmissions__Salt, from Key Vault).");
        }

        if (string.Equals(salt.Trim(), DevelopmentDefault, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConfigurationKey} must not be the public development default in Production.");
        }

        if (salt.Trim().Length < MinimumLength)
        {
            throw new InvalidOperationException(
                $"{ConfigurationKey} must be at least {MinimumLength} characters in Production.");
        }
    }

    public static string Resolve(IConfiguration configuration)
    {
        var salt = configuration[ConfigurationKey];
        return string.IsNullOrWhiteSpace(salt) ? DevelopmentDefault : salt.Trim();
    }
}
