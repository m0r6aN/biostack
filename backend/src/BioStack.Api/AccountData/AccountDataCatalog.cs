namespace BioStack.Api.AccountData;

using BioStack.Domain.Entities;
using BioStack.Domain.Entities.Graph;
using BioStack.Infrastructure.Persistence.Entities;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The single source of truth for which tables hold a person's data, what the export includes, and how
/// deletion treats each one. Order matters: <see cref="Tables"/> is also the deletion order, children first,
/// so Restrict foreign keys (ProtocolItem to CompoundRecord, Protocol parent and origin) never block it.
/// Every entity in <see cref="BioStackDbContext"/> must appear exactly once; AccountDataCatalogCompletenessTests
/// fails the build otherwise.
/// </summary>
public static class AccountDataCatalog
{
    /// <summary>Bump when the table list or a deletion rule changes (stored on the non-identifying deletion record).</summary>
    public const int ProcedureVersion = 1;

    /// <summary>Postgres point-in-time-restore retention. Shown to the user and used for the backup age-out date.</summary>
    public const int BackupRetentionDays = 35;

    public const string DeletionConfirmationPhrase = "DELETE MY ACCOUNT";

    private static IQueryable<Guid> ProfileIds(BioStackDbContext db, AccountScope s)
        => db.PersonProfiles.Where(p => p.OwnerId == s.UserId).Select(p => p.Id);

    private static IQueryable<Guid> ProtocolIds(BioStackDbContext db, AccountScope s)
    {
        var profileIds = ProfileIds(db, s);
        return db.Protocols.Where(p => profileIds.Contains(p.PersonId)).Select(p => p.Id);
    }

    private static IQueryable<Guid> IdentityIds(BioStackDbContext db, AccountScope s)
        => db.AuthIdentities.Where(i => i.UserId == s.UserId).Select(i => i.Id);

    public static IReadOnlyList<AccountDataTable> Tables { get; } =
    [
        // ── The person's own entries (deepest children first) ────────────────────────────
        new ScopedTable<DoseLogEntry>("DoseLogEntries", UserDataDisposition.UserScoped,
            "Dose logs the user entered. Amounts are the user's own numbers.",
            (db, s) => { var p = ProfileIds(db, s); return db.DoseLogEntries.Where(d => p.Contains(d.PersonId)); }),
        new ScopedTable<CheckIn>("CheckIns", UserDataDisposition.UserScoped,
            "Check-ins, including side-effect and mood text the user typed.",
            (db, s) => { var p = ProfileIds(db, s); return db.CheckIns.Where(c => p.Contains(c.PersonId)); }),
        new ScopedTable<ProtocolComputationRecord>("ProtocolComputationRecords", UserDataDisposition.UserScoped,
            "Saved computation snapshots for the user's protocols.",
            (db, s) => { var p = ProtocolIds(db, s); return db.ProtocolComputationRecords.Where(r => p.Contains(r.ProtocolId)); }),
        new ScopedTable<ProtocolReviewCompletedEvent>("ProtocolReviewCompletedEvents", UserDataDisposition.UserScoped,
            "Protocol review completion events.",
            (db, s) => { var p = ProtocolIds(db, s); return db.ProtocolReviewCompletedEvents.Where(r => p.Contains(r.ProtocolId)); }),
        new ScopedTable<ProtocolItem>("ProtocolItems", UserDataDisposition.UserScoped,
            "Items inside saved protocols, with the snapshot of each item as it was when saved.",
            (db, s) => { var p = ProtocolIds(db, s); return db.ProtocolItems.Where(i => p.Contains(i.ProtocolId)); }),
        new ScopedTable<ProtocolRun>("ProtocolRuns", UserDataDisposition.UserScoped,
            "Protocol runs the user started.",
            (db, s) => { var p = ProfileIds(db, s); return db.ProtocolRuns.Where(r => p.Contains(r.PersonId)); }),
        new ScopedTable<Protocol>("Protocols", UserDataDisposition.UserScoped,
            "Saved protocols and their version history.",
            (db, s) => { var p = ProfileIds(db, s); return db.Protocols.Where(x => p.Contains(x.PersonId)); },
            delete: async (db, s, ct) =>
            {
                var own = ProtocolIds(db, s);
                // Parent, origin and evolved-from links are Restrict: clear every reference to these
                // protocols first (own versions point at each other; no other user should point here).
                await db.Protocols
                    .Where(x => own.Contains(x.Id) || (x.ParentProtocolId != null && own.Contains(x.ParentProtocolId.Value))
                        || (x.OriginProtocolId != null && own.Contains(x.OriginProtocolId.Value)))
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(x => x.ParentProtocolId, (Guid?)null)
                        .SetProperty(x => x.OriginProtocolId, (Guid?)null)
                        .SetProperty(x => x.EvolvedFromRunId, (Guid?)null), ct);
                return await db.Protocols.Where(x => own.Contains(x.Id)).ExecuteDeleteAsync(ct);
            }),
        new ScopedTable<CatalogLookup>("CatalogLookups", UserDataDisposition.UserScoped,
            "Names you asked BioStack to look up in its own library, and what it found. Stays inside your account.",
            (db, s) => db.CatalogLookups.Where(l => l.UserId == s.UserId)),
        new ScopedTable<CompoundRecord>("CompoundRecords", UserDataDisposition.UserScoped,
            "Everything I'm Taking items, including custom and unverified items exactly as typed.",
            (db, s) => { var p = ProfileIds(db, s); return db.CompoundRecords.Where(c => p.Contains(c.PersonId)); }),
        new ScopedTable<ProtocolPhase>("ProtocolPhases", UserDataDisposition.UserScoped,
            "Phases the user defined for protocols.",
            (db, s) => { var p = ProfileIds(db, s); return db.ProtocolPhases.Where(x => p.Contains(x.PersonId)); }),
        new ScopedTable<TimelineEvent>("TimelineEvents", UserDataDisposition.UserScoped,
            "Timeline entries.",
            (db, s) => { var p = ProfileIds(db, s); return db.TimelineEvents.Where(x => p.Contains(x.PersonId)); }),
        new ScopedTable<ProfileGoal>("ProfileGoals", UserDataDisposition.UserScoped,
            "Goals chosen on a profile.",
            (db, s) => { var p = ProfileIds(db, s); return db.ProfileGoals.Where(x => p.Contains(x.ProfileId)); }),
        new ScopedTable<PersonProfile>("PersonProfiles", UserDataDisposition.UserScoped,
            "Profiles the user owns (name, age, weight, notes). The owner link is SetNull, so profiles are deleted explicitly, never left orphaned.",
            (db, s) => db.PersonProfiles.Where(p => p.OwnerId == s.UserId)),

        // ── Keyed by user id with no foreign key ────────────────────────────────────────
        new ScopedTable<EvidenceAlertLogEntry>("EvidenceAlertLog", UserDataDisposition.UserScoped,
            "Evidence alerts sent or shown to the user.",
            (db, s) => db.EvidenceAlertLog.Where(x => x.UserId == s.UserId)),
        new ScopedTable<EvidenceAlertPreference>("EvidenceAlertPreferences", UserDataDisposition.UserScoped,
            "Alert frequency and email preference.",
            (db, s) => db.EvidenceAlertPreferences.Where(x => x.UserId == s.UserId),
            omitFromExport: ["UnsubscribeToken"]),
        new ScopedTable<EvidenceWatch>("EvidenceWatches", UserDataDisposition.UserScoped,
            "Compounds and claims the user watches.",
            (db, s) => db.EvidenceWatches.Where(x => x.UserId == s.UserId)),

        // ── Billing ─────────────────────────────────────────────────────────────────────
        new ScopedTable<Subscription>("Subscriptions", UserDataDisposition.UserScoped,
            "Subscription state mirrored from Stripe. Stripe keeps its own billing records where tax rules require it.",
            (db, s) => db.Subscriptions.Where(x => x.AppUserId == s.UserId)),

        // ── Rows linked by key or email rather than a foreign key ───────────────────────
        new ScopedTable<ClaimSubmission>("ClaimSubmissions", UserDataDisposition.UserScoped,
            "Claims the user submitted while signed in. The stored submitter key is a salted hash of the user id; "
            + "it can be recomputed on the server, so these rows are treated as the user's. Submissions made while signed out cannot be linked and stay anonymous.",
            (db, s) => db.ClaimSubmissions.Where(x => x.SubmitterKey == s.SubmitterKey),
            omitFromExport: ["SubmitterKey"]),
        new ScopedTable<LeadCapture>("LeadCaptures", UserDataDisposition.UserScoped,
            "Email-capture rows for the account's email address(es).",
            (db, s) => db.LeadCaptures.Where(x => s.Emails.Contains(x.Email.ToLower()))),
        new ScopedTable<ProviderAccessRequest>("ProviderAccessRequests", UserDataDisposition.UserScoped,
            "Provider-access requests made with the account's email. Anonymized, not deleted: the request row stays as a redacted shell so its audit trail remains consistent.",
            (db, s) => db.ProviderAccessRequests.Where(x => s.Emails.Contains(x.Email.ToLower())),
            delete: async (db, s, ct) =>
            {
                var rows = await db.ProviderAccessRequests.Where(x => s.Emails.Contains(x.Email.ToLower())).ToListAsync(ct);
                foreach (var row in rows) row.AnonymizeForRetention();
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();
                return rows.Count;
            }),
        new ScopedTable<ProviderAccessAuditEntry>("ProviderAccessAuditEntries", UserDataDisposition.UserScoped,
            "Status changes this user made as an operator. On deletion the actor id is overwritten with an empty id; the entry itself stays.",
            (db, s) => db.ProviderAccessAuditEntries.Where(x => x.ActorId == s.UserId),
            delete: (db, s, ct) => db.ProviderAccessAuditEntries.Where(x => x.ActorId == s.UserId)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.ActorId, Guid.Empty), ct)),

        // ── Sign-in artifacts ───────────────────────────────────────────────────────────
        new ScopedTable<AuthChallenge>("AuthChallenges", UserDataDisposition.UserScoped,
            "Sign-in and deletion-confirmation links that were issued (the link secret is stored only as a hash and is never exported).",
            (db, s) => { var i = IdentityIds(db, s); return db.AuthChallenges.Where(c => i.Contains(c.IdentityId)); },
            omitFromExport: ["TokenHash"]),
        new ScopedTable<PasskeyCredential>("PasskeyCredentials", UserDataDisposition.UserScoped,
            "Passkeys: name and dates only. Key material is never exported.",
            (db, s) => { var i = IdentityIds(db, s); return db.PasskeyCredentials.Where(c => i.Contains(c.IdentityId)); },
            omitFromExport: ["CredentialId", "PublicKey", "UserHandle"]),
        new ScopedTable<PasskeyOperationChallenge>("PasskeyOperationChallenges", UserDataDisposition.UserScoped,
            "Short-lived passkey ceremonies for this user (options and request hash are never exported).",
            (db, s) => db.PasskeyOperationChallenges.Where(c => c.UserId == s.UserId),
            omitFromExport: ["OptionsJson", "RequestIdHash"]),
        new ScopedTable<Session>("Sessions", UserDataDisposition.UserScoped,
            "Signed-in sessions (when and from where). Every session is revoked and then removed on deletion.",
            (db, s) => db.Sessions.Where(x => x.UserId == s.UserId),
            omitFromExport: ["TokenHash"],
            delete: async (db, s, ct) =>
            {
                var now = DateTime.UtcNow;
                await db.Sessions.Where(x => x.UserId == s.UserId && x.RevokedAtUtc == null)
                    .ExecuteUpdateAsync(u => u.SetProperty(x => x.RevokedAtUtc, now), ct);
                return await db.Sessions.Where(x => x.UserId == s.UserId).ExecuteDeleteAsync(ct);
            }),
        new ScopedTable<AuthIdentity>("AuthIdentities", UserDataDisposition.UserScoped,
            "The email address(es) used to sign in.",
            (db, s) => db.AuthIdentities.Where(i => i.UserId == s.UserId)),

        new ScopedTable<UserNotice>("UserNotices", UserDataDisposition.UserScoped,
            "Notes shown to you inside BioStack, for example a library match waiting for your answer.",
            (db, s) => db.UserNotices.Where(n => n.UserId == s.UserId)),
        new ScopedTable<ConsentEvent>("ConsentEvents", UserDataDisposition.UserScoped,
            "Your consent history: each time you accepted or declined, which version, and when.",
            (db, s) => db.ConsentEvents.Where(e => e.UserId == s.UserId)),

        // ── Governance spine: append-only, so it is kept, not edited ────────────────────
        new ScopedTable<SpineEntry>("SpineEntries", UserDataDisposition.RetainedPseudonymous,
            "Governance receipts. Each is signed into an append-only hash chain, so editing or removing one would break every later entry. "
            + "They hold only the pseudonymous actor id \"user:<id>\", receipt hashes and subject URIs, no name, email or health values. "
            + "After deletion that id maps to nothing. They are exported but not deleted.",
            (db, s) => db.SpineEntries.Where(e => e.ActorId == s.ActorId)),

        // ── The account itself (last) ───────────────────────────────────────────────────
        new ScopedTable<AppUser>("AppUsers", UserDataDisposition.UserScoped,
            "The account: email, display name, role, consent record, billing customer id.",
            (db, s) => db.AppUsers.Where(u => u.Id == s.UserId)),

        // ── Not about any one person ────────────────────────────────────────────────────
        new ReferenceTable("ClaimCheckQueries", typeof(ClaimCheckQuery), UserDataDisposition.AnonymousByDesign,
            "Claim-checker queries stored as a hash with the matched claim. No user id, no raw text."),
        new ReferenceTable("AccountDeletionRecords", typeof(AccountDeletionRecord), UserDataDisposition.AnonymousByDesign,
            "The minimal deletion record: random id, UTC day, row total, backup age-out day. No user key."),
        new ReferenceTable("StripeWebhookEvents", typeof(StripeWebhookEvent), UserDataDisposition.GlobalReference,
            "Stripe event ids and processing status for idempotency. No customer or user data."),
        new ReferenceTable("SpineChainCheckpoints", typeof(SpineChainCheckpoint), UserDataDisposition.GlobalReference,
            "Signed checkpoints over the spine hash chain. No actor or user data."),
        new ReferenceTable("InteractionFlags", typeof(InteractionFlag), UserDataDisposition.GlobalReference,
            "Product interaction content."),
        new ReferenceTable("CompoundInteractionHints", typeof(CompoundInteractionHint), UserDataDisposition.GlobalReference,
            "Product interaction content."),
        new ReferenceTable("KnowledgeEntries", typeof(KnowledgeEntry), UserDataDisposition.GlobalReference,
            "Evidence library content."),
        new ReferenceTable("KnowledgeSourceIntakeRequests", typeof(KnowledgeSourceIntakeRequest), UserDataDisposition.GlobalReference,
            "Operator source-intake queue for the evidence library."),
        new ReferenceTable("StagedTranscriptCandidateReviews", typeof(StagedTranscriptCandidateReviewEntity), UserDataDisposition.GlobalReference,
            "Operator review queue for the evidence library."),
        new ReferenceTable("CompoundGraphArtifacts", typeof(CompoundGraphArtifact), UserDataDisposition.GlobalReference,
            "Knowledge-graph content."),
        new ReferenceTable("CompoundGraphRelationships", typeof(CompoundGraphRelationship), UserDataDisposition.GlobalReference,
            "Knowledge-graph content."),
        new ReferenceTable("CompoundGraphFindings", typeof(CompoundGraphFinding), UserDataDisposition.GlobalReference,
            "Knowledge-graph content."),
    ];

    public static IEnumerable<AccountDataTable> ExportedTables => Tables.Where(t => t.IsExported);
    public static IEnumerable<AccountDataTable> DeletedTables => Tables.Where(t => t.IsDeleted);
}
