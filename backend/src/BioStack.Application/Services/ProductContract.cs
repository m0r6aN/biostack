namespace BioStack.Application.Services;

using System.Text.Json;
using BioStack.Domain.Enums;

public sealed class ProductContract
{
    private const string ResourceName = "BioStack.ProductContract.v1.json";
    private static readonly Lazy<ProductContract> LazyCurrent = new(Load);

    private ProductContract(ProductContractDocument document)
    {
        Document = document;
    }

    public static ProductContract Current => LazyCurrent.Value;

    public ProductContractDocument Document { get; }
    public string Version => Document.ContractVersion;
    public BillingContract Billing => Document.Billing;
    public IReadOnlyDictionary<string, FeatureContract> Features => Document.Features;
    public RouteContract Routes => Document.Routes;
    public HealthContract Health => Document.Health;

    public ProductPlanContract GetPlan(string code)
        => Billing.Plans.Single(plan => plan.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    public ProductPlanContract GetPlan(ProductTier tier)
        => Billing.Plans.Single(plan => ParseTier(plan.Tier) == tier);

    public ProductTier GetTier(ProductPlanContract plan) => ParseTier(plan.Tier);

    public bool HasPaidAccess(SubscriptionStatus status, DateTime? currentPeriodEndUtc, DateTime utcNow)
    {
        var paidThrough = currentPeriodEndUtc is null || currentPeriodEndUtc > utcNow;
        return paidThrough && Billing.PaidAccessStatuses.Any(
            paidStatus => paidStatus.Equals(status.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    public bool IsFeatureEnabled(string featureCode, ProductTier tier)
        => Features.TryGetValue(featureCode, out var feature)
            && tier >= ParseTier(feature.MinimumTier);

    public int? GetLimit(string featureCode, ProductTier tier)
    {
        if (!Features.TryGetValue(featureCode, out var feature))
        {
            return null;
        }

        return feature.Limits.TryGetValue(tier.ToString(), out var limit) ? limit : null;
    }

    public string NormalizeRouteAlias(string route)
    {
        var suffixIndex = route.IndexOfAny(['?', '#']);
        var path = suffixIndex >= 0 ? route[..suffixIndex] : route;
        var suffix = suffixIndex >= 0 ? route[suffixIndex..] : string.Empty;
        var alias = Routes.Aliases.FirstOrDefault(item => item.Key.Equals(path, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(alias.Key) ? route : $"{alias.Value}{suffix}";
    }

    private static ProductContract Load()
    {
        using var stream = typeof(ProductContract).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded product contract {ResourceName} was not found.");
        var document = JsonSerializer.Deserialize<ProductContractDocument>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("Product contract could not be deserialized.");

        Validate(document);
        return new ProductContract(document);
    }

    private static void Validate(ProductContractDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.ContractVersion))
        {
            throw new InvalidOperationException("Product contract version is required.");
        }

        if (!document.Billing.Interval.Equals("month", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The launch product contract supports monthly billing only.");
        }

        if (document.Billing.PastDueGraceDays != 0)
        {
            throw new InvalidOperationException("The launch product contract requires immediate past-due downgrade.");
        }

        foreach (var tier in Enum.GetValues<ProductTier>())
        {
            if (document.Billing.Plans.Count(plan => ParseTier(plan.Tier) == tier) != 1)
            {
                throw new InvalidOperationException($"Product contract must define exactly one plan for {tier}.");
            }
        }

        if (!document.Health.LivenessPath.StartsWith('/') || !document.Health.KeonDependencyPath.StartsWith('/'))
        {
            throw new InvalidOperationException("Health contract paths must be application-relative.");
        }
    }

    private static ProductTier ParseTier(string tier)
        => Enum.TryParse<ProductTier>(tier, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Unknown product tier '{tier}' in product contract.");
}

public sealed record ProductContractDocument(
    string ContractVersion,
    string EffectiveDate,
    BillingContract Billing,
    Dictionary<string, FeatureContract> Features,
    RouteContract Routes,
    HealthContract Health);

public sealed record BillingContract(
    string Interval,
    string Currency,
    int PastDueGraceDays,
    string[] PaidAccessStatuses,
    ProductPlanContract[] Plans);

public sealed record ProductPlanContract(
    string Code,
    string Tier,
    string DisplayName,
    string Tagline,
    int MonthlyPriceCents,
    string? StripePriceConfigurationKey,
    string MarketingCtaPath);

public sealed record FeatureContract(
    string MinimumTier,
    Dictionary<string, int?> Limits);

public sealed record RouteContract(
    string[] PublicPrefixes,
    Dictionary<string, string> Canonical,
    Dictionary<string, string> Aliases);

public sealed record HealthContract(
    string LivenessPath,
    string KeonDependencyPath);
