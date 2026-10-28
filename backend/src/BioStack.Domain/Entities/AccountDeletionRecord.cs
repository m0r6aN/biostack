namespace BioStack.Domain.Entities;

/// <summary>
/// Minimal, non-identifying proof that a self-serve account deletion ran (MHMDA deletion path).
/// Deliberately has NO user id, email, profile id, session or any other link back to the person:
/// a random id, the UTC calendar day (never a time of day), a total row count and the day the
/// 35-day Postgres backup window is expected to have aged the data out. It exists so the operator
/// can show that deletions are happening and when their backups clear, and nothing more.
/// This table is intentionally NOT cascaded from <see cref="AppUser"/> because it holds no user key.
/// </summary>
public sealed class AccountDeletionRecord
{
    public Guid Id { get; set; }

    /// <summary>Midnight UTC of the deletion day. The time of day is not stored.</summary>
    public DateTime DeletedOnUtc { get; set; }

    /// <summary>Total rows removed or irreversibly anonymized across all tables.</summary>
    public int RowsAffected { get; set; }

    /// <summary>Midnight UTC of the day after which the Postgres PITR backups no longer hold the data.</summary>
    public DateTime BackupsAgeOutByUtc { get; set; }

    /// <summary>Which version of the deletion procedure ran (bumped when the table list changes).</summary>
    public int ProcedureVersion { get; set; }
}
