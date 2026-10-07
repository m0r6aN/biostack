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
    public const string SupportInboxAddress = "support@biostack.cc";

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
        var email = new EmailMessage(senderAddress, SupportInboxAddress, content);
        email.ReplyTo.Add(new EmailAddress(message.Email, message.Name));

        await client.SendAsync(WaitUntil.Started, email, ct);
        _logger.LogInformation(
            "Queued BioStack contact message ({Category}) from {Email} to the support inbox through Azure Communication Email",
            message.Category,
            message.Email);
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
