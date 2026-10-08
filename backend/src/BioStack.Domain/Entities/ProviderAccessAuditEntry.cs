namespace BioStack.Domain.Entities;

/// <summary>
/// PR-PROV-001: append-only audit trail for admin mutations of a
/// <see cref="ProviderAccessRequest"/>. One row is written per status/owner-changing
/// <c>PATCH</c> to the admin provider-access queue. No PII beyond what
/// <see cref="ProviderAccessRequest"/> already stores (no email/name/organization/role here),
/// and this table is never exposed on any public endpoint.
/// </summary>
public sealed class ProviderAccessAuditEntry
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }

    /// <summary>The authenticated admin principal's user id (from the "sub"/NameIdentifier claim).</summary>
    public Guid ActorId { get; set; }

    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string? FromOwner { get; set; }
    public string? ToOwner { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
