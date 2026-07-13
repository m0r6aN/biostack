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
            ProductContract.Current.Features
                .Where(feature => feature.Key != FeatureCodes.ActiveCompounds)
                .ToDictionary(
                    feature => feature.Key,
                    feature => ProductContract.Current.IsFeatureEnabled(feature.Key, tier)),
            ProductContract.Current.Features
                .Where(feature => feature.Value.Limits.Count > 0)
                .ToDictionary(
                    feature => feature.Key,
                    feature => ProductContract.Current.GetLimit(feature.Key, tier)),
            ProductContract.Current.Version);
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
            SuccessUrl = GetConfiguredUrl("Stripe:CheckoutSuccessUrl", "/billing?checkout=success"),
            CancelUrl = GetConfiguredUrl("Stripe:CheckoutCancelUrl", "/billing?checkout=cancelled"),
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
            ReturnUrl = GetConfiguredUrl("Stripe:PortalReturnUrl", "/billing")
        }, cancellationToken: cancellationToken);

        return new BillingSessionResponse(session.Url);
    }

    public async Task<IReadOnlyList<StripeWebhookReceiptResponse>> GetQuarantinedStripeEventsAsync(CancellationToken cancellationToken = default)
        => await _db.StripeWebhookEvents
            .AsNoTracking()
            .Where(receipt => receipt.ProcessingStatus == StripeWebhookProcessingStatuses.Quarantined)
            .OrderByDescending(receipt => receipt.LastAttemptAtUtc)
            .Select(receipt => new StripeWebhookReceiptResponse(
                receipt.StripeEventId,
                receipt.EventType,
                receipt.ProcessingStatus,
                receipt.FailureCode,
                receipt.AttemptCount,
                receipt.LastAttemptAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<StripeWebhookProcessingResult> ProcessStripeEventAsync(Event stripeEvent, CancellationToken cancellationToken = default)
    {
        var receipt = await _db.StripeWebhookEvents
            .SingleOrDefaultAsync(e => e.StripeEventId == stripeEvent.Id, cancellationToken);
        if (receipt?.ProcessingStatus == StripeWebhookProcessingStatuses.Processed)
            return StripeWebhookProcessingResult.AlreadyProcessed;

        var now = DateTime.UtcNow;

        try
        {
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
                case "invoice.payment_succeeded":
                    await ProcessPaymentSucceededAsync(stripeEvent, cancellationToken);
                    break;
            }
        }
        catch (UnknownStripePriceException)
        {
            receipt ??= new StripeWebhookEvent
            {
                Id = Guid.NewGuid(),
                StripeEventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                AttemptCount = 0,
            };
            if (_db.Entry(receipt).State == EntityState.Detached)
                _db.StripeWebhookEvents.Add(receipt);

            receipt.ProcessingStatus = StripeWebhookProcessingStatuses.Quarantined;
            receipt.FailureCode = "unknown_stripe_price";
            receipt.AttemptCount++;
            receipt.LastAttemptAtUtc = now;
            receipt.ProcessedAtUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
            return StripeWebhookProcessingResult.Quarantined;
        }

        receipt ??= new StripeWebhookEvent
        {
            Id = Guid.NewGuid(),
            StripeEventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            AttemptCount = 0,
        };
        if (_db.Entry(receipt).State == EntityState.Detached)
            _db.StripeWebhookEvents.Add(receipt);

        receipt.ProcessingStatus = StripeWebhookProcessingStatuses.Processed;
        receipt.FailureCode = null;
        receipt.AttemptCount++;
        receipt.LastAttemptAtUtc = now;
        receipt.ProcessedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        return StripeWebhookProcessingResult.Processed;
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
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
        var now = DateTime.UtcNow;
        PlanDescriptor plan;
        try
        {
            plan = ResolvePlanFromPrice(stripePriceId);
        }
        catch (UnknownStripePriceException)
        {
            if (subscription is not null)
            {
                subscription.ProductCode = "observer";
                subscription.Tier = ProductTier.Observer;
                subscription.StripeCustomerId = stripeCustomerId;
                subscription.StripePriceId = stripePriceId;
                subscription.Status = MapStripeStatus(stripeStatus);
                subscription.CurrentPeriodStartUtc = currentPeriodStartUtc;
                subscription.CurrentPeriodEndUtc = currentPeriodEndUtc;
                subscription.CancelAtPeriodEnd = cancelAtPeriodEnd;
                subscription.UpdatedAtUtc = now;
                await _db.SaveChangesAsync(cancellationToken);
            }

            throw;
        }

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

    private async Task ProcessPaymentSucceededAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscriptionId = stripeEvent.Data.Object switch
        {
            Invoice invoice => invoice.Parent?.SubscriptionDetails?.SubscriptionId,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(subscriptionId))
            return;

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId, cancellationToken);
        if (subscription is null || subscription.CurrentPeriodEndUtc is not null && subscription.CurrentPeriodEndUtc <= DateTime.UtcNow)
            return;

        subscription.Status = SubscriptionStatus.Active;
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
        ProductPlanContract definition;
        try
        {
            definition = ProductContract.Current.GetPlan(normalized);
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("Unknown paid plan.");
        }

        if (string.IsNullOrWhiteSpace(definition.StripePriceConfigurationKey))
            throw new InvalidOperationException("Observer does not require checkout.");

        var plan = new PlanDescriptor(
            definition.Code,
            ProductContract.Current.GetTier(definition),
            _configuration[definition.StripePriceConfigurationKey] ?? string.Empty);

        if (string.IsNullOrWhiteSpace(plan.PriceId))
            throw new InvalidOperationException($"Stripe price id is not configured for {plan.ProductCode}.");

        return plan;
    }

    private PlanDescriptor ResolvePlanFromPrice(string priceId)
    {
        foreach (var definition in ProductContract.Current.Billing.Plans.Where(plan => !string.IsNullOrWhiteSpace(plan.StripePriceConfigurationKey)))
        {
            if (!string.IsNullOrWhiteSpace(priceId) && priceId == _configuration[definition.StripePriceConfigurationKey!])
            {
                return new PlanDescriptor(definition.Code, ProductContract.Current.GetTier(definition), priceId);
            }
        }

        throw new UnknownStripePriceException(priceId);
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

    private string GetConfiguredUrl(string configurationKey, string fallbackPath)
    {
        var configured = _configuration[configurationKey];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var frontendUrl = string.IsNullOrWhiteSpace(_configuration["FrontendUrl"])
            ? "http://localhost:3043"
            : _configuration["FrontendUrl"]!.TrimEnd('/');
        return $"{frontendUrl}{fallbackPath}";
    }

    private sealed record PlanDescriptor(string ProductCode, ProductTier Tier, string PriceId);
}

public interface IBillingService
{
    Task<CurrentSubscriptionResponse> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default);
    Task<BillingSessionResponse> CreateCheckoutSessionAsync(string planCode, CancellationToken cancellationToken = default);
    Task<BillingSessionResponse> CreatePortalSessionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StripeWebhookReceiptResponse>> GetQuarantinedStripeEventsAsync(CancellationToken cancellationToken = default);
    Task<StripeWebhookProcessingResult> ProcessStripeEventAsync(Event stripeEvent, CancellationToken cancellationToken = default);
    Task ReconcileSubscriptionAsync(Guid appUserId, string stripeCustomerId, string stripeSubscriptionId, string stripePriceId, string stripeStatus, DateTime? currentPeriodStartUtc, DateTime? currentPeriodEndUtc, bool cancelAtPeriodEnd, CancellationToken cancellationToken = default);
}

public enum StripeWebhookProcessingResult
{
    Processed,
    AlreadyProcessed,
    Quarantined
}

public sealed class UnknownStripePriceException(string? priceId)
    : InvalidOperationException($"Stripe price '{priceId ?? "<missing>"}' is not present in the approved product contract configuration.");

public sealed record StripeWebhookReceiptResponse(
    string StripeEventId,
    string EventType,
    string ProcessingStatus,
    string? FailureCode,
    int AttemptCount,
    DateTime LastAttemptAtUtc);
