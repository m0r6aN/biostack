namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using AppSubscription = BioStack.Domain.Entities.Subscription;
using CheckoutSession = Stripe.Checkout.Session;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CheckoutSessionSubscriptionDataOptions = Stripe.Checkout.SessionSubscriptionDataOptions;
using StripeSubscription = Stripe.Subscription;

public sealed class BillingService : IBillingService
{
    private readonly BioStackDbContext _db;
    private readonly IAppUserRepository _userRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IFeatureGate _featureGate;
    private readonly IConfiguration _configuration;

    public BillingService(
        BioStackDbContext db,
        IAppUserRepository userRepository,
        ICurrentUserAccessor currentUserAccessor,
        IFeatureGate featureGate,
        IConfiguration configuration)
    {
        _db = db;
        _userRepository = userRepository;
        _currentUserAccessor = currentUserAccessor;
        _featureGate = featureGate;
        _configuration = configuration;
    }

    public async Task<CurrentSubscriptionResponse> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserAccessor.GetCurrentUserId();
        var tier = await _featureGate.GetEffectiveTierAsync(userId, cancellationToken);
        var subscription = await _db.Subscriptions
            .Where(s => s.AppUserId == userId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new CurrentSubscriptionResponse(
            tier.ToString(),
            subscription?.Status.ToString() ?? SubscriptionStatus.None.ToString(),
            subscription?.ProductCode ?? "observer",
            tier > ProductTier.Observer,
            subscription?.CancelAtPeriodEnd ?? false,
            subscription?.CurrentPeriodEndUtc,
            new Dictionary<string, bool>
            {
                [FeatureCodes.PaidIntelligence] = tier >= ProductTier.Operator,
                [FeatureCodes.CommanderIntelligence] = tier >= ProductTier.Commander
            },
            new Dictionary<string, int?>
            {
                [FeatureCodes.ActiveCompounds] = tier == ProductTier.Observer ? FeatureGate.ObserverActiveCompoundLimit : null
            });
    }

    public async Task<BillingSessionResponse> CreateCheckoutSessionAsync(string planCode, CancellationToken cancellationToken = default)
    {
        var plan = ResolvePaidPlan(planCode);
        var userId = _currentUserAccessor.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user was not found.");

        var customerId = await GetOrCreateStripeCustomerAsync(user, cancellationToken);
        var service = new CheckoutSessionService();
        var session = await service.CreateAsync(new CheckoutSessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            ClientReferenceId = user.Id.ToString(),
            SuccessUrl = _configuration["Stripe:CheckoutSuccessUrl"] ?? $"{_configuration["FrontendUrl"] ?? "http://localhost:3043"}/billing?checkout=success",
            CancelUrl = _configuration["Stripe:CheckoutCancelUrl"] ?? $"{_configuration["FrontendUrl"] ?? "http://localhost:3043"}/billing?checkout=cancelled",
            LineItems =
            [
                new CheckoutSessionLineItemOptions
                {
                    Price = plan.PriceId,
                    Quantity = 1
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["appUserId"] = user.Id.ToString(),
                ["planCode"] = plan.ProductCode,
                ["tier"] = plan.Tier.ToString()
            },
            SubscriptionData = new CheckoutSessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["appUserId"] = user.Id.ToString(),
                    ["planCode"] = plan.ProductCode,
                    ["tier"] = plan.Tier.ToString()
                }
            }
        }, cancellationToken: cancellationToken);

        return new BillingSessionResponse(session.Url);
    }

    public async Task<BillingSessionResponse> CreatePortalSessionAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserAccessor.GetCurrentUserId();
        var subscription = await _db.Subscriptions
            .Where(s => s.AppUserId == userId && s.StripeCustomerId != string.Empty)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No Stripe billing customer exists for this account.");

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = subscription.StripeCustomerId,
            ReturnUrl = _configuration["Stripe:PortalReturnUrl"] ?? $"{_configuration["FrontendUrl"] ?? "http://localhost:3043"}/billing"
        }, cancellationToken: cancellationToken);

        return new BillingSessionResponse(session.Url);
    }

    public async Task ProcessStripeEventAsync(Event stripeEvent, CancellationToken cancellationToken = default)
    {
        if (await _db.StripeWebhookEvents.AnyAsync(e => e.StripeEventId == stripeEvent.Id, cancellationToken))
            return;

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await ProcessCheckoutCompletedAsync(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await ProcessSubscriptionEventAsync(stripeEvent, cancellationToken);
                break;
            case "invoice.payment_failed":
                await ProcessPaymentFailedAsync(stripeEvent, cancellationToken);
                break;
        }

        _db.StripeWebhookEvents.Add(new StripeWebhookEvent
        {
            Id = Guid.NewGuid(),
            StripeEventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            ProcessedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReconcileSubscriptionAsync(
        Guid appUserId,
        string stripeCustomerId,
        string stripeSubscriptionId,
        string stripePriceId,
        string stripeStatus,
        DateTime? currentPeriodStartUtc,
        DateTime? currentPeriodEndUtc,
        bool cancelAtPeriodEnd,
        CancellationToken cancellationToken = default)
    {
        var plan = ResolvePlanFromPrice(stripePriceId);
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
        var now = DateTime.UtcNow;

        if (subscription is null)
        {
            subscription = new AppSubscription
            {
                Id = Guid.NewGuid(),
                AppUserId = appUserId,
                CreatedAtUtc = now
            };
            _db.Subscriptions.Add(subscription);
        }

        subscription.ProductCode = plan.ProductCode;
        subscription.Tier = plan.Tier;
        subscription.StripeCustomerId = stripeCustomerId;
        subscription.StripeSubscriptionId = stripeSubscriptionId;
        subscription.StripePriceId = stripePriceId;
        subscription.Status = MapStripeStatus(stripeStatus);
        subscription.CurrentPeriodStartUtc = currentPeriodStartUtc;
        subscription.CurrentPeriodEndUtc = currentPeriodEndUtc;
        subscription.CancelAtPeriodEnd = cancelAtPeriodEnd;
        subscription.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GetOrCreateStripeCustomerAsync(AppUser user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]))
            throw new InvalidOperationException("Stripe secret key is not configured.");

        if (!string.IsNullOrWhiteSpace(user.StripeCustomerId))
            return user.StripeCustomerId;

        var existing = await _db.Subscriptions
            .Where(s => s.AppUserId == user.Id && s.StripeCustomerId != string.Empty)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .Select(s => s.StripeCustomerId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        var service = new CustomerService();
        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = user.Email,
            Name = user.DisplayName,
            Metadata = new Dictionary<string, string>
            {
                ["appUserId"] = user.Id.ToString()
            }
        }, cancellationToken: cancellationToken);

        user.StripeCustomerId = customer.Id;
        await _userRepository.UpsertAsync(user, cancellationToken);
        return customer.Id;
    }

    private async Task ProcessCheckoutCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not CheckoutSession session || string.IsNullOrWhiteSpace(session.SubscriptionId))
            return;

        var service = new Stripe.SubscriptionService();
        var subscription = await service.GetAsync(session.SubscriptionId, cancellationToken: cancellationToken);
        await UpsertFromStripeSubscriptionAsync(subscription, cancellationToken);
    }

    private async Task ProcessSubscriptionEventAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is StripeSubscription subscription)
        {
            await UpsertFromStripeSubscriptionAsync(subscription, cancellationToken);
        }
    }

    private async Task ProcessPaymentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscriptionId = stripeEvent.Data.Object switch
        {
            Invoice invoice => invoice.Parent?.SubscriptionDetails?.SubscriptionId,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(subscriptionId))
            return;

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId, cancellationToken);
        if (subscription is null)
            return;

        subscription.Status = SubscriptionStatus.PastDue;
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertFromStripeSubscriptionAsync(StripeSubscription stripeSubscription, CancellationToken cancellationToken)
    {
        var appUserId = await ResolveAppUserIdAsync(stripeSubscription, cancellationToken);
        var item = stripeSubscription.Items?.Data?.FirstOrDefault();
        var priceId = item?.Price?.Id ?? string.Empty;
        await ReconcileSubscriptionAsync(
            appUserId,
            stripeSubscription.CustomerId,
            stripeSubscription.Id,
            priceId,
            stripeSubscription.Status,
            item?.CurrentPeriodStart,
            item?.CurrentPeriodEnd,
            stripeSubscription.CancelAtPeriodEnd,
            cancellationToken);
    }

    private async Task<Guid> ResolveAppUserIdAsync(StripeSubscription stripeSubscription, CancellationToken cancellationToken)
    {
        if (stripeSubscription.Metadata.TryGetValue("appUserId", out var value) && Guid.TryParse(value, out var metadataUserId))
            return metadataUserId;

        var existing = await _db.Subscriptions
            .Where(s => s.StripeCustomerId == stripeSubscription.CustomerId)
            .Select(s => s.AppUserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing != Guid.Empty)
            return existing;

        throw new InvalidOperationException("Stripe subscription could not be mapped to an AppUser.");
    }

    private PlanDescriptor ResolvePaidPlan(string planCode)
    {
        var normalized = planCode.Trim().ToLowerInvariant();
        var plan = normalized switch
        {
            "operator" => new PlanDescriptor("operator", ProductTier.Operator, _configuration["Stripe:OperatorPriceId"] ?? string.Empty),
            "commander" => new PlanDescriptor("commander", ProductTier.Commander, _configuration["Stripe:CommanderPriceId"] ?? string.Empty),
            _ => throw new InvalidOperationException("Unknown paid plan.")
        };

        if (string.IsNullOrWhiteSpace(plan.PriceId))
            throw new InvalidOperationException($"Stripe price id is not configured for {plan.ProductCode}.");

        return plan;
    }

    private PlanDescriptor ResolvePlanFromPrice(string priceId)
    {
        if (!string.IsNullOrWhiteSpace(priceId) && priceId == _configuration["Stripe:CommanderPriceId"])
            return new PlanDescriptor("commander", ProductTier.Commander, priceId);
        if (!string.IsNullOrWhiteSpace(priceId) && priceId == _configuration["Stripe:OperatorPriceId"])
            return new PlanDescriptor("operator", ProductTier.Operator, priceId);

        return new PlanDescriptor("observer", ProductTier.Observer, priceId);
    }

    private static SubscriptionStatus MapStripeStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "incomplete" => SubscriptionStatus.Incomplete,
            "trialing" => SubscriptionStatus.Trialing,
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "unpaid" => SubscriptionStatus.Unpaid,
            "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
            "paused" => SubscriptionStatus.Paused,
            _ => SubscriptionStatus.None
        };
    }

    private sealed record PlanDescriptor(string ProductCode, ProductTier Tier, string PriceId);
}

public interface IBillingService
{
    Task<CurrentSubscriptionResponse> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default);
    Task<BillingSessionResponse> CreateCheckoutSessionAsync(string planCode, CancellationToken cancellationToken = default);
    Task<BillingSessionResponse> CreatePortalSessionAsync(CancellationToken cancellationToken = default);
    Task ProcessStripeEventAsync(Event stripeEvent, CancellationToken cancellationToken = default);
    Task ReconcileSubscriptionAsync(Guid appUserId, string stripeCustomerId, string stripeSubscriptionId, string stripePriceId, string stripeStatus, DateTime? currentPeriodStartUtc, DateTime? currentPeriodEndUtc, bool cancelAtPeriodEnd, CancellationToken cancellationToken = default);
}
