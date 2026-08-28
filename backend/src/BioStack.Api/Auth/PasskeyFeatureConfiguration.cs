namespace BioStack.Api.Auth;

public sealed record PasskeyFeatureConfiguration(
    bool Enabled,
    string RpId,
    string ServerName,
    IReadOnlySet<string> Origins)
{
    public static PasskeyFeatureConfiguration Load(IConfiguration configuration)
    {
        var section = configuration.GetSection("Auth:Passkeys");
        return new PasskeyFeatureConfiguration(
            section.GetValue<bool>("Enabled"),
            section["RpId"]?.Trim() ?? string.Empty,
            section["ServerName"]?.Trim() ?? "BioStack",
            (section.GetSection("Origins").Get<string[]>() ?? [])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim().TrimEnd('/'))
                .ToHashSet(StringComparer.Ordinal));
    }

    public static void Validate(PasskeyFeatureConfiguration options, bool isProduction, string? frontendUrl)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.RpId) ||
            options.RpId.Contains("://", StringComparison.Ordinal) ||
            options.RpId.Contains('/') ||
            options.RpId.Contains('*'))
        {
            throw new InvalidOperationException("Auth:Passkeys:RpId must be a hostname, without a scheme, path, port, or wildcard.");
        }

        if (string.IsNullOrWhiteSpace(options.ServerName))
        {
            throw new InvalidOperationException("Auth:Passkeys:ServerName is required when passkeys are enabled.");
        }

        if (options.Origins.Count == 0)
        {
            throw new InvalidOperationException("Auth:Passkeys:Origins must contain at least one exact WebAuthn origin.");
        }

        foreach (var origin in options.Origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                !string.IsNullOrEmpty(uri.PathAndQuery.Trim('/')) ||
                !string.IsNullOrEmpty(uri.Fragment) ||
                (isProduction && uri.Scheme != Uri.UriSchemeHttps) ||
                (!uri.Host.Equals(options.RpId, StringComparison.OrdinalIgnoreCase) &&
                 !uri.Host.EndsWith($".{options.RpId}", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Auth:Passkeys:Origins contains an invalid origin for RP ID '{options.RpId}'.");
            }
        }

        if (isProduction &&
            (!Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontend) ||
             !options.Origins.Contains(frontend.GetLeftPart(UriPartial.Authority).TrimEnd('/'))))
        {
            throw new InvalidOperationException("FrontendUrl must exactly match an Auth:Passkeys:Origins entry in production.");
        }
    }
}
