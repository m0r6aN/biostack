namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using Stripe;

public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/billing")
            .WithTags("Billing")
            .RequireAuthorization();

        group.MapGet("/subscription", GetSubscription)
            .WithName("GetCurrentSubscription");

        group.MapPost("/checkout", CreateCheckout)
            .WithName("CreateBillingCheckout");

        group.MapPost("/portal", CreatePortal)
            .WithName("CreateBillingPortal");

        app.MapPost("/api/v1/billing/stripe/webhook", StripeWebhook)
            .WithTags("Billing")
            .AllowAnonymous()
            .WithMetadata(new RequestSizeLimitAttribute(1_048_576))
            .WithName("StripeBillingWebhook");

        app.MapGet("/api/v1/admin/billing/stripe/events/quarantined", GetQuarantinedStripeEvents)
            .WithTags("Billing", "Admin")
            .RequireAuthorization("AdminOnly")
            .WithName("GetQuarantinedStripeBillingEvents");
    }

    private static async Task<IResult> GetSubscription(IBillingService billingService, CancellationToken ct)
    {
        var subscription = await billingService.GetCurrentSubscriptionAsync(ct);
        return Results.Ok(subscription);
    }

    private static async Task<IResult> CreateCheckout(CreateCheckoutSessionRequest request, IBillingService billingService, CancellationToken ct)
    {
        try
        {
            var session = await billingService.CreateCheckoutSessionAsync(request.PlanCode, ct);
            return Results.Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CreatePortal(IBillingService billingService, CancellationToken ct)
    {
        try
        {
            var session = await billingService.CreatePortalSessionAsync(ct);
            return Results.Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetQuarantinedStripeEvents(IBillingService billingService, CancellationToken ct)
        => Results.Ok(await billingService.GetQuarantinedStripeEventsAsync(ct));

    private static async Task<IResult> StripeWebhook(HttpRequest request, IConfiguration configuration, IBillingService billingService, CancellationToken ct)
    {
        var secret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Results.BadRequest(new { error = "Stripe webhook secret is not configured." });
        }

        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync(ct);
        var signature = request.Headers["Stripe-Signature"].FirstOrDefault();

        try
        {
            // Signature validation remains mandatory; tolerate newer Stripe event API
            // versions so an SDK upgrade is not misreported as a bad signature.
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                secret,
                throwOnApiVersionMismatch: false);
            var result = await billingService.ProcessStripeEventAsync(stripeEvent, ct);
            return result == StripeWebhookProcessingResult.Quarantined
                ? Results.Conflict(new { error = "Stripe event was quarantined for an unapproved price and must be replayed after configuration is corrected." })
                : Results.Ok(new { status = result.ToString() });
        }
        catch (StripeException)
        {
            return Results.BadRequest(new { error = "Invalid Stripe webhook signature." });
        }
    }
}
