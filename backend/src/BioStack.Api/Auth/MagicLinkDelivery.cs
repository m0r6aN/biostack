namespace BioStack.Api.Auth;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;

public sealed record DevMagicLinkMessage(
    string Contact,
    string Link,
    string RedirectPath,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc
);

public interface IMagicLinkDelivery
{
    Task SendAsync(string contact, string magicLink, string redirectPath, DateTime expiresAtUtc, CancellationToken ct);
}

public interface IDevMagicLinkInbox
{
    IReadOnlyCollection<DevMagicLinkMessage> Latest();
}

public sealed class InMemoryMagicLinkDelivery : IMagicLinkDelivery, IDevMagicLinkInbox
{
    private const int MaxMessages = 25;
    private readonly ConcurrentQueue<DevMagicLinkMessage> _messages = new();
    private readonly ILogger<InMemoryMagicLinkDelivery> _logger;

    public InMemoryMagicLinkDelivery(ILogger<InMemoryMagicLinkDelivery> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string contact, string magicLink, string redirectPath, DateTime expiresAtUtc, CancellationToken ct)
    {
        _messages.Enqueue(new DevMagicLinkMessage(contact, magicLink, redirectPath, DateTime.UtcNow, expiresAtUtc));

        while (_messages.Count > MaxMessages && _messages.TryDequeue(out _))
        {
        }

        _logger.LogInformation("BioStack magic link for {Contact}: {MagicLink}", contact, magicLink);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<DevMagicLinkMessage> Latest()
        => _messages.Reverse().ToArray();
}

public sealed class SmtpMagicLinkDelivery : IMagicLinkDelivery
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpMagicLinkDelivery> _logger;

    public SmtpMagicLinkDelivery(
        IConfiguration config,
        ILogger<SmtpMagicLinkDelivery> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string contact, string magicLink, string redirectPath, DateTime expiresAtUtc, CancellationToken ct)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Smtp:Host must be configured to send magic link emails.");
        }

        var fromEmail = _config["Smtp:FromEmail"];
        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("Smtp:FromEmail must be configured to send magic link emails.");
        }

        var port = int.TryParse(_config["Smtp:Port"], out var configuredPort)
            ? configuredPort
            : 587;
        var enableSsl = !bool.TryParse(_config["Smtp:EnableSsl"], out var configuredSsl) || configuredSsl;
        var fromName = _config["Smtp:FromName"] ?? "BioStack";
        var subject = _config["Smtp:MagicLinkSubject"] ?? "Your BioStack sign-in link";

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = BuildHtmlBody(magicLink, expiresAtUtc),
            IsBodyHtml = true,
        };
        message.To.Add(new MailAddress(contact));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
        };

        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        await client.SendMailAsync(message, ct);
        _logger.LogInformation("Sent BioStack magic link email to {Contact}", contact);
    }

    private static string BuildHtmlBody(string magicLink, DateTime expiresAtUtc)
    {
        var safeLink = WebUtility.HtmlEncode(magicLink);
        var safeExpiresAt = WebUtility.HtmlEncode(expiresAtUtc.ToString("u"));

        return $"""
            <p>Use this private link to sign in to BioStack:</p>
            <p><a href="{safeLink}">Sign in to BioStack</a></p>
            <p>This link expires at {safeExpiresAt}.</p>
            <p>If you did not request this link, you can ignore this email.</p>
            """;
    }
}
