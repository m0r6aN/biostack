namespace BioStack.Infrastructure.Keon;

/// <summary>
/// Identifies who initiated a governed effect (Lane G).
///
/// User-triggered operations MUST identify the acting user. System actors are reserved for
/// true system-initiated work (offline workers, scheduled precompute jobs). The tenant is the
/// governance boundary the actor operates within.
/// </summary>
public sealed record ReceiptActor(string ActorId, string TenantId, bool IsSystem)
{
    /// <summary>
    /// The product/consumer tenant. BioStack has no per-organization tenant model today, so the
    /// consumer surface shares a single governance tenant. This is the documented stable fallback
    /// until a real tenant entity exists; the <c>ActorId</c> still uniquely identifies the user.
    /// </summary>
    public const string ConsumerTenant = "biostack-public";

    /// <summary>The system tenant for offline/scheduled work.</summary>
    public const string SystemTenant = "biostack-system";

    /// <summary>An authenticated end user acting on the consumer surface.</summary>
    public static ReceiptActor User(Guid userId) =>
        new($"user:{userId}", ConsumerTenant, IsSystem: false);

    /// <summary>
    /// A system-initiated component (worker, scheduler). Allowed only for genuinely
    /// system-initiated operations, never as a stand-in for a missing user context.
    /// </summary>
    public static ReceiptActor System(string component) =>
        new($"system:{component}", SystemTenant, IsSystem: true);
}
