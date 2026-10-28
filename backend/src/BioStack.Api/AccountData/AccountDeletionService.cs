namespace BioStack.Api.AccountData;

using BioStack.Api.Auth;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public enum DeletionRequestOutcome { LinkSent, AccountNotFound, AdminProtected, TooManyRequests }

public enum DeletionOutcomeKind { Deleted, InvalidLink, AccountNotFound, AdminProtected, BillingUnavailable }

public sealed record DeletionOutcome(
    DeletionOutcomeKind Kind,
    Guid? ReceiptId = null,
    DateTime? DeletedAtUtc = null,
    DateTime? BackupsAgeOutByUtc = null,
    int RowsAffected = 0,
    bool ReceiptEmailQueued = false);

/// <summary>
/// Self-serve account deletion.
/// Re-authentication: a typed confirmation phrase AND a fresh single-use link emailed to the account address,
/// which only works from a browser that is already signed in. The session proves the browser, the email proves
/// the mailbox owner, so a stolen session cookie or an unattended browser alone cannot erase an account.
/// </summary>
public sealed class AccountDeletionService(
    BioStackDbContext db,
    AccountScopeResolver scopeResolver,
    IStripeCustomerCloser stripe,
    IAccountEmailSender email,
    IConfiguration config,
    IWebHostEnvironment environment,
    ILogger<AccountDeletionService> logger)
{
    public const string ChallengeType = "account_deletion";
    private const string EmailIdentityType = "email";
    private const string EmailChannel = "email";
    public const int MaxLinksPerHour = 3;
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(15);

    public async Task<DeletionRequestOutcome> SendConfirmationLinkAsync(Guid userId, string? ipAddress, CancellationToken ct)
    {
        var user = await db.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return DeletionRequestOutcome.AccountNotFound;
        if (user.Role == UserRole.Admin) return DeletionRequestOutcome.AdminProtected;

        var now = DateTime.UtcNow;
        var address = user.Email.Trim().ToLowerInvariant();
        var identity = await db.AuthIdentities.FirstOrDefaultAsync(i => i.UserId == userId && i.Type == EmailIdentityType, ct);
        if (identity is null)
        {
            // Legacy accounts may have no sign-in identity row yet. Sign-in creates the same row on demand.
            identity = new AuthIdentity { Id = Guid.NewGuid(), UserId = userId, Type = EmailIdentityType, ValueNormalized = address, CreatedAtUtc = now };
            db.AuthIdentities.Add(identity);
        }

        // Per-account cap, independent of how many sessions the account has: no mail flood to the owner.
        var recent = await db.AuthChallenges.CountAsync(c =>
            c.IdentityId == identity.Id && c.ChallengeType == ChallengeType && c.CreatedAtUtc > now.AddHours(-1), ct);
        if (recent >= MaxLinksPerHour) return DeletionRequestOutcome.TooManyRequests;

        await db.AuthChallenges
            .Where(c => c.IdentityId == identity.Id && c.ChallengeType == ChallengeType && c.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(u => u.SetProperty(c => c.ConsumedAtUtc, now), ct);

        var rawToken = AuthSessionIssuer.GenerateToken();
        var challenge = new AuthChallenge
        {
            Id = Guid.NewGuid(),
            IdentityId = identity.Id,
            Channel = EmailChannel,
            ChallengeType = ChallengeType,
            TokenHash = AuthSessionIssuer.HashSecret(rawToken),
            ExpiresAtUtc = now.Add(LinkLifetime),
            CreatedAtUtc = now,
            IpAddress = ipAddress,
            RedirectPath = "/account/data",
        };
        db.AuthChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);

        var frontend = (config["FrontendUrl"] ?? config["Auth:FrontendUrl"] ?? "http://localhost:3043").TrimEnd('/');
        var delimiter = environment.IsDevelopment() ? "?token=" : "#token=";
        var link = $"{frontend}/account/data/delete/confirm{delimiter}{Uri.EscapeDataString(rawToken)}";
        await email.SendAsync(AccountEmailCopy.DeletionConfirmation(user.Email, link, challenge.ExpiresAtUtc), ct);
        return DeletionRequestOutcome.LinkSent;
    }

    public async Task<DeletionOutcome> ConfirmAndDeleteAsync(Guid userId, string? token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return new DeletionOutcome(DeletionOutcomeKind.InvalidLink);

        var scope = await scopeResolver.ResolveAsync(userId, ct);
        if (scope is null) return new DeletionOutcome(DeletionOutcomeKind.AccountNotFound);

        var user = await db.AppUsers.AsNoTracking().FirstAsync(u => u.Id == userId, ct);
        if (user.Role == UserRole.Admin) return new DeletionOutcome(DeletionOutcomeKind.AdminProtected);

        // Claim the single-use link atomically. It must belong to THIS user and to this purpose.
        var tokenHash = AuthSessionIssuer.HashSecret(token);
        var now = DateTime.UtcNow;
        var identityIds = db.AuthIdentities.Where(i => i.UserId == userId).Select(i => i.Id);
        var claimed = await db.AuthChallenges
            .Where(c => c.TokenHash == tokenHash && c.ChallengeType == ChallengeType && c.ConsumedAtUtc == null
                && c.ExpiresAtUtc > now && identityIds.Contains(c.IdentityId))
            .ExecuteUpdateAsync(u => u.SetProperty(c => c.ConsumedAtUtc, now).SetProperty(c => c.AttemptCount, c => c.AttemptCount + 1), ct);
        if (claimed != 1) return new DeletionOutcome(DeletionOutcomeKind.InvalidLink);

        // Billing first: deleting the Stripe customer cancels the subscription. If Stripe can't be reached we stop
        // before anything is erased, so billing never keeps running for an account that no longer exists.
        var customerIds = await db.Subscriptions.AsNoTracking()
            .Where(s => s.AppUserId == userId && s.StripeCustomerId != string.Empty)
            .Select(s => s.StripeCustomerId)
            .ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(user.StripeCustomerId)) customerIds.Add(user.StripeCustomerId);
        customerIds = customerIds.Distinct(StringComparer.Ordinal).ToList();
        if (customerIds.Count > 0)
        {
            try
            {
                if (!stripe.IsConfigured) throw new InvalidOperationException("Stripe is not configured.");
                foreach (var customerId in customerIds) await stripe.DeleteCustomerAsync(customerId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "AccountDeletionBillingUnavailable: nothing was deleted");
                await db.AuthChallenges.Where(c => c.TokenHash == tokenHash && c.ChallengeType == ChallengeType)
                    .ExecuteUpdateAsync(u => u.SetProperty(c => c.ConsumedAtUtc, (DateTime?)null), CancellationToken.None);
                return new DeletionOutcome(DeletionOutcomeKind.BillingUnavailable);
            }
        }

        var deletedOn = DateTime.UtcNow.Date;
        var ageOutBy = deletedOn.AddDays(AccountDataCatalog.BackupRetentionDays);
        var record = new AccountDeletionRecord
        {
            Id = Guid.NewGuid(),
            DeletedOnUtc = deletedOn,
            BackupsAgeOutByUtc = ageOutBy,
            ProcedureVersion = AccountDataCatalog.ProcedureVersion,
        };

        var rows = 0;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            foreach (var table in AccountDataCatalog.DeletedTables)
            {
                rows += await table.DeleteAsync(db, scope, ct);
            }

            record.RowsAffected = rows;
            db.ChangeTracker.Clear();
            db.AccountDeletionRecords.Add(record);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        logger.LogInformation("AccountDeletionCompleted RowsAffected={RowsAffected} ProcedureVersion={ProcedureVersion}", rows, AccountDataCatalog.ProcedureVersion);

        var queued = false;
        try
        {
            var frontend = (config["FrontendUrl"] ?? config["Auth:FrontendUrl"] ?? "http://localhost:3043").TrimEnd('/');
            await email.SendAsync(AccountEmailCopy.DeletionReceipt(user.Email, record.Id, deletedOn, ageOutBy, frontend), ct);
            queued = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The deletion already happened and cannot be undone. The on-screen receipt still shows everything.
            logger.LogWarning(ex, "AccountDeletionReceiptEmailFailed");
        }

        return new DeletionOutcome(DeletionOutcomeKind.Deleted, record.Id, deletedOn, ageOutBy, rows, queued);
    }
}
