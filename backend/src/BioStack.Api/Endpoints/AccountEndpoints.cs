namespace BioStack.Api.Endpoints;

using BioStack.Api.AccountData;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Self-serve export and deletion of a person's own data (MHMDA access and deletion rights).
/// These endpoints are deliberately NOT behind RequireConsent: a person must always be able to take their data
/// out or delete it, including while their consent is out of date.
/// </summary>
public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/account")
            .WithTags("Account")
            .RequireAuthorization();

        group.MapGet("/data-summary", GetSummary).WithName("GetAccountDataSummary");
        group.MapGet("/export", Export).WithName("ExportAccountData").RequireRateLimiting("account-export");
        group.MapPost("/deletion/request", RequestDeletion).WithName("RequestAccountDeletion").RequireRateLimiting("account-deletion-request");
        group.MapPost("/deletion/confirm", ConfirmDeletion).WithName("ConfirmAccountDeletion").RequireRateLimiting("account-deletion-confirm");
    }

    public sealed record DeletionRequestBody(string? Confirmation);
    public sealed record DeletionConfirmBody(string? Token);

    private static async Task<IResult> GetSummary(
        ICurrentUserAccessor currentUser, AccountScopeResolver resolver, BioStackDbContext db, CancellationToken ct)
    {
        var scope = await resolver.ResolveAsync(currentUser.GetCurrentUserId(), ct);
        if (scope is null) return Results.NotFound();

        var role = await db.AppUsers.AsNoTracking().Where(u => u.Id == scope.UserId).Select(u => u.Role).FirstAsync(ct);
        var tables = new List<object>();
        foreach (var table in AccountDataCatalog.Tables.Where(t => t.IsExported))
        {
            tables.Add(new
            {
                name = table.Name,
                count = await table.CountAsync(db, scope, ct),
                onDeletion = table.IsDeleted ? "removed" : "kept",
                note = table.Note,
            });
        }

        return Results.Ok(new
        {
            tables,
            backupRetentionDays = AccountDataCatalog.BackupRetentionDays,
            confirmationPhrase = AccountDataCatalog.DeletionConfirmationPhrase,
            deletionAvailable = role != BioStack.Domain.Enums.UserRole.Admin,
        });
    }

    private static async Task<IResult> Export(
        string? format, HttpContext http, ICurrentUserAccessor currentUser,
        AccountScopeResolver resolver, AccountExportService exporter, CancellationToken ct)
    {
        var chosen = (format ?? "json").Trim().ToLowerInvariant();
        if (chosen is not ("json" or "csv"))
            return Results.BadRequest(new { code = "invalid_format", message = "Format must be json or csv." });

        var scope = await resolver.ResolveAsync(currentUser.GetCurrentUserId(), ct);
        if (scope is null) return Results.NotFound();

        var document = await exporter.BuildAsync(scope, ct);
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.Pragma = "no-cache";
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd");
        return chosen == "csv"
            ? Results.File(AccountExportService.ToCsvZip(document), "application/zip", $"biostack-export-{stamp}-csv.zip")
            : Results.File(AccountExportService.ToJson(document), "application/json", $"biostack-export-{stamp}.json");
    }

    private static async Task<IResult> RequestDeletion(
        DeletionRequestBody body, HttpContext http, ICurrentUserAccessor currentUser,
        AccountDeletionService deletion, CancellationToken ct)
    {
        if (!string.Equals(body?.Confirmation?.Trim(), AccountDataCatalog.DeletionConfirmationPhrase, StringComparison.Ordinal))
            return Results.BadRequest(new { code = "confirmation_required", message = $"Type {AccountDataCatalog.DeletionConfirmationPhrase} to continue." });

        var outcome = await deletion.SendConfirmationLinkAsync(
            currentUser.GetCurrentUserId(), http.Connection.RemoteIpAddress?.ToString(), ct);
        return outcome switch
        {
            DeletionRequestOutcome.LinkSent => Results.Accepted(value: new { status = "link_sent", message = "We emailed you a link to finish deleting your account. It works once and expires in 15 minutes." }),
            DeletionRequestOutcome.AdminProtected => Results.Conflict(new { code = "admin_account_protected", message = "Operator accounts can't be deleted from here. Contact BioStack to retire an operator account." }),
            DeletionRequestOutcome.TooManyRequests => Results.StatusCode(StatusCodes.Status429TooManyRequests),
            _ => Results.NotFound(),
        };
    }

    private static async Task<IResult> ConfirmDeletion(
        DeletionConfirmBody body, HttpContext http, ICurrentUserAccessor currentUser,
        AccountDeletionService deletion, CancellationToken ct)
    {
        var outcome = await deletion.ConfirmAndDeleteAsync(currentUser.GetCurrentUserId(), body?.Token, ct);
        switch (outcome.Kind)
        {
            case DeletionOutcomeKind.Deleted:
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Ok(new
                {
                    status = "deleted",
                    receiptId = outcome.ReceiptId,
                    deletedOnUtc = outcome.DeletedAtUtc,
                    backupsAgeOutByUtc = outcome.BackupsAgeOutByUtc,
                    backupRetentionDays = AccountDataCatalog.BackupRetentionDays,
                    rowsAffected = outcome.RowsAffected,
                    receiptEmailQueued = outcome.ReceiptEmailQueued,
                });
            case DeletionOutcomeKind.AdminProtected:
                return Results.Conflict(new { code = "admin_account_protected", message = "Operator accounts can't be deleted from here." });
            case DeletionOutcomeKind.BillingUnavailable:
                return Results.Json(
                    new { code = "billing_unavailable", message = "We couldn't reach our payment provider to cancel your subscription, so nothing was deleted. Your link still works. Try again in a few minutes." },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            default:
                return Results.BadRequest(new { code = "invalid_link", message = "This deletion link is invalid, expired, or already used. Request a new one from the Account data page." });
        }
    }
}
