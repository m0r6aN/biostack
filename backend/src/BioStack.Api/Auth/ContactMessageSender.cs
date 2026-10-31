namespace BioStack.Api.Auth;

using System.Net;
using Azure;
using Azure.Communication.Email;

/// <summary>A visitor-submitted contact form message ready for delivery to the support inbox.</summary>
public sealed record ContactMessage(string Name, string Email, string Category, string Message);

public interface IContactMessageSender
{
    Task SendAsync(ContactMessage message, CancellationToken ct);
}

/// <summary>
/// Delivers contact-form submissions to the BioStack support inbox over Azure Communication
/// Email — the same SDK and configuration keys (<c>AzureCommunicationEmail:ConnectionString</c> /
/// <c>AzureCommunicationEmail:SenderAddress</c>) as <see cref="AzureCommunicationEmailMagicLinkDelivery"/>.
/// The visitor's address is set as ReplyTo so support can answer directly.
/// </summary>
public sealed class AzureCommunicationEmailContactMessageSender : IContactMessageSender
{
    /// <summary>App setting that overrides where contact-form messages are delivered. Unset means the default below.</summary>
    public const string SupportInboxConfigKey = "Contact:SupportInboxAddress";

    /// <summary>
    /// The address used when <see cref="SupportInboxConfigKey"/> is unset: the Google Workspace support alias
    /// (MX, SPF and DKIM live as of 2026-10-31; before that the domain had no MX record and mail bounced).
    /// </summary>
    public const string DefaultSupportInboxAddress = "support@biostack.cc";

    private readonly IConfiguration _config;
    private readonly ILogger<AzureCommunicationEmailContactMessageSender> _logger;

    public AzureCommunicationEmailContactMessageSender(
        IConfiguration config,
        ILogger<AzureCommunicationEmailContactMessageSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(ContactMessage message, CancellationToken ct)
    {
        var connectionString = _config["AzureCommunicationEmail:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("AzureCommunicationEmail:ConnectionString must be configured to send contact emails.");
        }

        var senderAddress = _config["AzureCommunicationEmail:SenderAddress"];
        if (string.IsNullOrWhiteSpace(senderAddress))
        {
            throw new InvalidOperationException("AzureCommunicationEmail:SenderAddress must be configured to send contact emails.");
        }

        var client = new EmailClient(connectionString);
        var content = new EmailContent($"[BioStack Contact] {message.Category}")
        {
            PlainText = BuildPlainTextBody(message),
            Html = BuildHtmlBody(message),
        };
        var email = new EmailMessage(senderAddress, ResolveRecipient(_config, _logger), content);
        email.ReplyTo.Add(new EmailAddress(message.Email, message.Name));

        await client.SendAsync(WaitUntil.Started, email, ct);
        _logger.LogInformation(
            "Queued BioStack contact message ({Category}) to the support inbox through Azure Communication Email",
            message.Category);
    }

    /// <summary>
    /// The configured recipient, or the default when the setting is unset. A configured value that is not a plain address
    /// is ignored (and logged) rather than sending support mail to a malformed recipient.
    /// </summary>
    public static string ResolveRecipient(IConfiguration config, ILogger logger)
    {
        var configured = config[SupportInboxConfigKey]?.Trim();
        if (string.IsNullOrEmpty(configured))
        {
            return DefaultSupportInboxAddress;
        }

        var at = configured.IndexOf('@');
        var plain = at > 0
            && at == configured.LastIndexOf('@')
            && at < configured.Length - 3
            && configured.IndexOf('.', at) > at + 1
            && !configured.Any(char.IsWhiteSpace);
        if (!plain)
        {
            logger.LogWarning("Contact:SupportInboxAddress is not a valid email address; using the default support inbox");
            return DefaultSupportInboxAddress;
        }

        return configured;
    }

    private static string BuildPlainTextBody(ContactMessage message)
        => $"""
            New contact message from the BioStack website.

            Name: {message.Name}
            Email: {message.Email}
            Category: {message.Category}

            {message.Message}
            """;

    private static string BuildHtmlBody(ContactMessage message)
    {
        var safeName = WebUtility.HtmlEncode(message.Name);
        var safeEmail = WebUtility.HtmlEncode(message.Email);
        var safeCategory = WebUtility.HtmlEncode(message.Category);
        var safeMessage = WebUtility.HtmlEncode(message.Message);

        return $"""
            <p>New contact message from the BioStack website.</p>
            <p><strong>Name:</strong> {safeName}<br />
            <strong>Email:</strong> {safeEmail}<br />
            <strong>Category:</strong> {safeCategory}</p>
            <p>{safeMessage}</p>
            """;
    }
}
