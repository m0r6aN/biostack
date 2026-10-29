namespace BioStack.Domain.Entities;

/// <summary>
/// Append-only history of a person's consent choices. <see cref="AppUser"/> keeps only the latest
/// value, so without this table accepting a newer version would erase proof of the earlier one.
/// One row per accepted or declined choice; repeating the same choice for the same version adds nothing.
/// Cascades with the account, so it is exported and deleted with everything else the person owns.
/// </summary>
public sealed class ConsentEvent
{
    public const string Accepted = "accepted";
    public const string Declined = "declined";

    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary><see cref="Accepted"/> or <see cref="Declined"/>.</summary>
    public string EventType { get; set; } = Accepted;

    /// <summary>The server-pinned disclosure version the choice was made against, e.g. bio-observational-v2.</summary>
    public string Version { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
