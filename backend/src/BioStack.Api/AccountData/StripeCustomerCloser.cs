namespace BioStack.Api.AccountData;

using Stripe;

/// <summary>
/// Ends a person's billing relationship at Stripe before their account is deleted. Deleting a Stripe
/// customer cancels its active subscriptions immediately, so billing cannot continue for an account that no
/// longer exists. Stripe keeps its own invoice and tax records; BioStack cannot erase those.
/// </summary>
public interface IStripeCustomerCloser
{
    /// <summary>False when Stripe is not configured on this deployment.</summary>
    bool IsConfigured { get; }

    /// <summary>Deletes the customer. A customer that no longer exists counts as success. Throws on any other failure.</summary>
    Task DeleteCustomerAsync(string customerId, CancellationToken ct);
}

public sealed class StripeCustomerCloser(IConfiguration config) : IStripeCustomerCloser
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(config["Stripe:SecretKey"]);

    public async Task DeleteCustomerAsync(string customerId, CancellationToken ct)
    {
        try
        {
            await new CustomerService().DeleteAsync(customerId, cancellationToken: ct);
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            // Already gone at Stripe: nothing left to cancel.
        }
    }
}
