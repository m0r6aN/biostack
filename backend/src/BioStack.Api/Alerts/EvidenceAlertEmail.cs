namespace BioStack.Api.Alerts;

using System.Net;
using System.Net.Mail;
using Azure;
using Azure.Communication.Email;

/// <summary><paramref name="UnsubscribeUrl"/> is the page a person clicks; <paramref name="OneClickUrl"/> is the RFC 8058 POST target for List-Unsubscribe.</summary>
public sealed record AlertEmail(string To, string Subject, string PlainText, string Html, string UnsubscribeUrl, string OneClickUrl);

public interface IEvidenceAlertEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(AlertEmail email, CancellationToken ct);
}

/// <summary>
/// Voice guide §4 alert copy. Every email names why it was sent, carries a one-click
/// unsubscribe link (plus List-Unsubscribe headers where the transport allows), and never
/// includes a dose: it links to the page instead.
/// </summary>
public static class EvidenceAlertCopy
{
    public const string Footer = "BioStack grades evidence; it doesn't tell you what to take.";

    public static string UnsubscribePage(string siteBase, string token) => $"{siteBase}/u/{token}";
    public static string OneClickUnsubscribe(string siteBase, string token) => $"{siteBase}/api/v1/alerts/unsubscribe/{token}";

    public static AlertEmail VerdictChanged(string to, ClaimChangeEvent e, string siteBase, string token)
    {
        var changelog = $"{siteBase}/knowledge/{e.CompoundSlug}/changelog";
        var subject = $"{e.CompoundName}: the verdict moved ({e.OldVerdict} → {e.NewVerdict})";
        var opener = $"The evidence on \"{e.Statement}\" changed. Here's what moved it: {e.Reason} Full changelog → {changelog}";
        return Build(to, subject, [opener], e.CompoundName, siteBase, token);
    }

    public static AlertEmail StudyBundle(string to, IReadOnlyList<ClaimChangeEvent> events, string siteBase, string token)
    {
        var compound = events.Select(e => e.CompoundName).Distinct().Count() == 1 ? events[0].CompoundName : "Your watchlist";
        var subject = $"{compound}: new study added to the record";
        var lines = events.Select(e => e.AlertType == EvidenceAlertPlanner.VerdictChange
            ? $"The evidence on \"{e.Statement}\" ({e.CompoundName}) changed. Verdict moved to {e.NewVerdict} (was {e.OldVerdict}). {e.Reason} {siteBase}/knowledge/{e.CompoundSlug}/changelog"
            : $"We added new evidence to the {e.CompoundName} record for \"{e.Statement}\". Verdict: unchanged ({e.NewVerdict}). {siteBase}/knowledge/{e.CompoundSlug}/claims/{e.ClaimId}").ToList();
        return Build(to, subject, lines, string.Join(", ", events.Select(e => e.CompoundName).Distinct()), siteBase, token);
    }

    private static AlertEmail Build(string to, string subject, IReadOnlyList<string> lines, string watched, string siteBase, string token)
    {
        var unsubscribeUrl = UnsubscribePage(siteBase, token);
        var footer = $"You're getting this because you watch {watched}. Stop these emails in one click → {unsubscribeUrl}. {Footer}";
        var text = string.Join("\n\n", lines.Append(footer));
        var html = string.Concat(lines.Select(l => $"<p>{WebUtility.HtmlEncode(l)}</p>"))
            + $"<p style=\"color:#666;font-size:12px\">You're getting this because you watch {WebUtility.HtmlEncode(watched)}. "
            + $"<a href=\"{WebUtility.HtmlEncode(unsubscribeUrl)}\">Stop these emails in one click</a>. {WebUtility.HtmlEncode(Footer)}</p>";
        return new AlertEmail(to, subject, text, html, unsubscribeUrl, OneClickUnsubscribe(siteBase, token));
    }
}

/// <summary>Azure Communication Email, same keys as magic links (AzureCommunicationEmail:*).</summary>
public sealed class AzureCommunicationAlertEmailSender(IConfiguration config) : IEvidenceAlertEmailSender
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(config["AzureCommunicationEmail:ConnectionString"]) && !string.IsNullOrWhiteSpace(Sender);
    private string? Sender => config["EvidenceAlerts:SenderAddress"] ?? config["AzureCommunicationEmail:SenderAddress"];

    public async Task SendAsync(AlertEmail email, CancellationToken ct)
    {
        var client = new EmailClient(config["AzureCommunicationEmail:ConnectionString"]);
        var message = new EmailMessage(Sender, email.To, new EmailContent(email.Subject) { PlainText = email.PlainText, Html = email.Html });
        message.Headers.Add("List-Unsubscribe", $"<{email.OneClickUrl}>");
        message.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");
        await client.SendAsync(WaitUntil.Started, message, ct);
    }
}

/// <summary>SMTP, same keys as magic links (Smtp:*).</summary>
public sealed class SmtpAlertEmailSender(IConfiguration config) : IEvidenceAlertEmailSender
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(config["Smtp:Host"]) && !string.IsNullOrWhiteSpace(config["Smtp:FromEmail"]);

    public async Task SendAsync(AlertEmail email, CancellationToken ct)
    {
        var port = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
        var ssl = !bool.TryParse(config["Smtp:EnableSsl"], out var s) || s;
        using var client = new SmtpClient(config["Smtp:Host"], port) { EnableSsl = ssl };
        if (!string.IsNullOrWhiteSpace(config["Smtp:Username"]))
            client.Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"]);
        using var message = new MailMessage(new MailAddress(config["Smtp:FromEmail"]!, config["Smtp:FromName"] ?? "BioStack"), new MailAddress(email.To))
        {
            Subject = email.Subject, Body = email.Html, IsBodyHtml = true,
        };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(email.PlainText, null, "text/plain"));
        message.Headers.Add("List-Unsubscribe", $"<{email.OneClickUrl}>");
        message.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");
        await client.SendMailAsync(message, ct);
    }
}

/// <summary>No transport configured: alerts stay in-app only.</summary>
public sealed class NoAlertEmailSender : IEvidenceAlertEmailSender
{
    public bool IsConfigured => false;
    public Task SendAsync(AlertEmail email, CancellationToken ct) => Task.CompletedTask;
}
