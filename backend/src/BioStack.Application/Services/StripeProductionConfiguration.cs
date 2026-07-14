namespace BioStack.Application.Services;

using Microsoft.Extensions.Configuration;

public static class StripeProductionConfiguration
{
    public static readonly string[] RequiredKeys =
    [
        "Stripe:SecretKey",
        "Stripe:WebhookSecret",
        "Stripe:OperatorPriceId",
        "Stripe:CommanderPriceId",
        "Stripe:CheckoutSuccessUrl",
        "Stripe:CheckoutCancelUrl",
        "Stripe:PortalReturnUrl",
    ];

    public static void Validate(IConfiguration configuration, bool isProduction)
    {
        if (!isProduction)
            return;

        var missingKeys = RequiredKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .ToArray();
        if (missingKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"Production billing configuration is incomplete: {string.Join(", ", missingKeys)}.");
        }

        var secretKey = configuration["Stripe:SecretKey"]!;
        if (!secretKey.StartsWith("sk_live_", StringComparison.Ordinal) &&
            !secretKey.StartsWith("rk_live_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Production Stripe:SecretKey must be a live-mode secret or restricted key.");
        }

        if (!configuration["Stripe:WebhookSecret"]!.StartsWith("whsec_", StringComparison.Ordinal))
            throw new InvalidOperationException("Production Stripe:WebhookSecret must be a Stripe endpoint signing secret.");

        var operatorPrice = configuration["Stripe:OperatorPriceId"]!;
        var commanderPrice = configuration["Stripe:CommanderPriceId"]!;
        if (!operatorPrice.StartsWith("price_", StringComparison.Ordinal) ||
            !commanderPrice.StartsWith("price_", StringComparison.Ordinal) ||
            operatorPrice.Equals(commanderPrice, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Production Stripe price IDs must be distinct Stripe price_ identifiers.");
        }

        foreach (var key in RequiredKeys.Where(key => key.EndsWith("Url", StringComparison.Ordinal)))
        {
            if (!Uri.TryCreate(configuration[key], UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException($"Production billing URL {key} must be an absolute HTTPS URL.");
        }
    }
}
