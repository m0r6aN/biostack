namespace BioStack.Api.Auth;

using System.ComponentModel.DataAnnotations;

public static class ProductionAuthConfiguration
{
    public static void Validate(IConfiguration configuration, bool isProduction)
    {
        if (!isProduction)
        {
            return;
        }

        var frontendUrl = configuration["FrontendUrl"] ?? configuration["Auth:FrontendUrl"];
        var frontendOrigin = RequireHttpsOrigin(frontendUrl, "FrontendUrl");
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => RequireHttpsOrigin(value, "Cors:AllowedOrigins"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];

        if (allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one HTTPS origin in Production.");
        }

        if (!allowedOrigins.Contains(frontendOrigin, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must include the configured FrontendUrl origin in Production.");
        }

        var hasAzureEmail = !string.IsNullOrWhiteSpace(configuration["AzureCommunicationEmail:ConnectionString"]);
        var hasSmtp = !string.IsNullOrWhiteSpace(configuration["Smtp:Host"]);
        if (hasAzureEmail == hasSmtp)
        {
            throw new InvalidOperationException(
                "Production must configure exactly one magic-link email provider: AzureCommunicationEmail or Smtp.");
        }

        if (hasAzureEmail)
        {
            RequireEmail(configuration["AzureCommunicationEmail:SenderAddress"], "AzureCommunicationEmail:SenderAddress");
            return;
        }

        RequireEmail(configuration["Smtp:FromEmail"], "Smtp:FromEmail");
        if (bool.TryParse(configuration["Smtp:EnableSsl"], out var enableSsl) && !enableSsl)
        {
            throw new InvalidOperationException("Smtp:EnableSsl must not be disabled in Production.");
        }

        if (int.TryParse(configuration["Smtp:Port"], out var port) && (port < 1 || port > 65535))
        {
            throw new InvalidOperationException("Smtp:Port must be between 1 and 65535.");
        }

        if (!string.IsNullOrWhiteSpace(configuration["Smtp:Username"]) &&
            string.IsNullOrWhiteSpace(configuration["Smtp:Password"]))
        {
            throw new InvalidOperationException("Smtp:Password is required when Smtp:Username is configured.");
        }
    }

    private static string RequireHttpsOrigin(string? value, string key)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException($"{key} must be an absolute HTTPS origin with no path, query, fragment, or credentials.");
        }

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private static void RequireEmail(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value) || !new EmailAddressAttribute().IsValid(value))
        {
            throw new InvalidOperationException($"{key} must be a valid email address in Production.");
        }
    }
}
