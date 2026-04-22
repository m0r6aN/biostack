namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
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
            .WithName("StripeBillingWebhook");
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
            var stripeEvent = EventUtility.ConstructEvent(json, signature, secret);
            await billingService.ProcessStripeEventAsync(stripeEvent, ct);
            return Results.Ok();
        }
        catch (StripeException)
        {
            return Results.BadRequest(new { error = "Invalid Stripe webhook signature." });
        }
    }
}
