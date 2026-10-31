namespace BioStack.Api.AccountData;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using Azure;
using Azure.Communication.Email;

/// <summary>A transactional account email (deletion confirmation link, deletion receipt). Never marketing.</summary>
public sealed record AccountEmail(string To, string Subject, string PlainText, string Html);

public interface IAccountEmailSender
{
    Task SendAsync(AccountEmail email, CancellationToken ct);
}

/// <summary>Azure Communication Email, same keys as magic links (AzureCommunicationEmail:*, sender noreply@biostack.cc in production).</summary>
public sealed class AzureCommunicationAccountEmailSender(IConfiguration config, ILogger<AzureCommunicationAccountEmailSender> logger) : IAccountEmailSender
{
    public async Task SendAsync(AccountEmail email, CancellationToken ct)
    {
        var connectionString = config["AzureCommunicationEmail:ConnectionString"];
        var sender = config["AzureCommunicationEmail:SenderAddress"];
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(sender))
            throw new InvalidOperationException("AzureCommunicationEmail:ConnectionString and SenderAddress must be configured to send account emails.");

        var client = new EmailClient(connectionString);
        var message = new EmailMessage(sender, email.To, new EmailContent(email.Subject) { PlainText = email.PlainText, Html = email.Html });
        BioStack.Api.Auth.OutgoingEmailReplyTo.Apply(message, config);
        await client.SendAsync(WaitUntil.Started, message, ct);
        logger.LogInformation("Queued BioStack account email through Azure Communication Email");
    }
}

/// <summary>SMTP, same keys as magic links (Smtp:*).</summary>
public sealed class SmtpAccountEmailSender(IConfiguration config, ILogger<SmtpAccountEmailSender> logger) : IAccountEmailSender
{
    public async Task SendAsync(AccountEmail email, CancellationToken ct)
    {
        var host = config["Smtp:Host"];
        var from = config["Smtp:FromEmail"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Smtp:Host and Smtp:FromEmail must be configured to send account emails.");

        var port = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
        var ssl = !bool.TryParse(config["Smtp:EnableSsl"], out var s) || s;
        using var message = new MailMessage(new MailAddress(from, config["Smtp:FromName"] ?? "BioStack"), new MailAddress(email.To))
        {
            Subject = email.Subject,
            Body = email.Html,
            IsBodyHtml = true,
        };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(email.PlainText, null, "text/plain"));
        using var client = new SmtpClient(host, port) { EnableSsl = ssl };
        if (!string.IsNullOrWhiteSpace(config["Smtp:Username"]))
            client.Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"]);
        await client.SendMailAsync(message, ct);
        logger.LogInformation("Sent BioStack account email over SMTP");
    }
}

/// <summary>Development and test outbox. Nothing leaves the process.</summary>
public sealed class InMemoryAccountEmailSender : IAccountEmailSender
{
    private const int MaxMessages = 50;
    private readonly ConcurrentQueue<AccountEmail> _messages = new();

    public IReadOnlyList<AccountEmail> Sent => _messages.ToArray();

    public Task SendAsync(AccountEmail email, CancellationToken ct)
    {
        _messages.Enqueue(email);
        while (_messages.Count > MaxMessages && _messages.TryDequeue(out _)) { }
        return Task.CompletedTask;
    }
}

/// <summary>Copy for the two account emails. Plain, no marketing, no affiliate wording, no health values.</summary>
public static class AccountEmailCopy
{
    public static AccountEmail DeletionConfirmation(string to, string confirmLink, DateTime expiresAtUtc)
    {
        var plain = $"""
            You asked to delete your BioStack account.

            Deleting is permanent. It removes your profile, the items you track, your dose logs, check-ins, protocols and notes, and your sign-in details. We can't bring them back.

            To finish, open this link in the browser where you're signed in to BioStack:

            {confirmLink}

            The link works once and expires at {expiresAtUtc:u}.

            If you didn't ask for this, ignore this email. Nothing has been deleted. You may want to sign out of BioStack on devices you don't use.
            """;
        var html = $"""
            <p>You asked to delete your BioStack account.</p>
            <p><strong>Deleting is permanent.</strong> It removes your profile, the items you track, your dose logs, check-ins, protocols and notes, and your sign-in details. We can't bring them back.</p>
            <p>To finish, open this link in the browser where you're signed in to BioStack:</p>
            <p><a href="{WebUtility.HtmlEncode(confirmLink)}">Confirm account deletion</a></p>
            <p>The link works once and expires at {WebUtility.HtmlEncode(expiresAtUtc.ToString("u"))}.</p>
            <p>If you didn't ask for this, ignore this email. Nothing has been deleted. You may want to sign out of BioStack on devices you don't use.</p>
            """;
        return new AccountEmail(to, "Confirm deleting your BioStack account", plain, html);
    }

    public static AccountEmail DeletionReceipt(string to, Guid receiptId, DateTime deletedAtUtc, DateTime backupsAgeOutByUtc, string siteBase)
    {
        var contact = $"{siteBase.TrimEnd('/')}/contact";
        var plain = $"""
            Your BioStack account was deleted on {deletedAtUtc:yyyy-MM-dd} (UTC).

            What was removed: your profile(s), the items you tracked, dose logs, check-ins, protocols, timeline and notes, watchlist and alert settings, subscription records, and your sign-in details and sessions. Anything you had submitted with your email address was removed or redacted.

            What can remain:
            - Backups. Our database backups keep data for up to {AccountDataCatalog.BackupRetentionDays} days, so deleted data ages out of backups by {backupsAgeOutByUtc:yyyy-MM-dd}. We don't use backups except to recover from a failure.
            - Payments. If you had a paid plan, our payment provider (Stripe) keeps its own billing records where tax rules require it. We cancelled your subscription.
            - A small record that a deletion happened. It holds a random id, the date and a row count, and nothing that identifies you.
            - Governance receipts, which hold only a random account id, with no name, email or health values. After this deletion that id points to nothing.

            Receipt id: {receiptId}

            If you didn't ask for this, contact us right away: {contact}
            """;
        var html = $"""
            <p>Your BioStack account was deleted on {deletedAtUtc:yyyy-MM-dd} (UTC).</p>
            <p><strong>What was removed:</strong> your profile(s), the items you tracked, dose logs, check-ins, protocols, timeline and notes, watchlist and alert settings, subscription records, and your sign-in details and sessions. Anything you had submitted with your email address was removed or redacted.</p>
            <p><strong>What can remain:</strong></p>
            <ul>
            <li>Backups. Our database backups keep data for up to {AccountDataCatalog.BackupRetentionDays} days, so deleted data ages out of backups by {backupsAgeOutByUtc:yyyy-MM-dd}. We don't use backups except to recover from a failure.</li>
            <li>Payments. If you had a paid plan, our payment provider (Stripe) keeps its own billing records where tax rules require it. We cancelled your subscription.</li>
            <li>A small record that a deletion happened. It holds a random id, the date and a row count, and nothing that identifies you.</li>
            <li>Governance receipts, which hold only a random account id, with no name, email or health values. After this deletion that id points to nothing.</li>
            </ul>
            <p>Receipt id: {receiptId}</p>
            <p>If you didn't ask for this, <a href="{WebUtility.HtmlEncode(contact)}">contact us right away</a>.</p>
            """;
        return new AccountEmail(to, "Your BioStack account was deleted", plain, html);
    }
}
