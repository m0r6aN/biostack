namespace BioStack.Api.Auth;

using Azure.Communication.Email;

/// <summary>
/// Optional Reply-To for system mail sent through Azure Communication Email (magic links, account emails, evidence alerts),
/// which is sent from a noreply address. Set <see cref="ConfigKey"/> (env <c>Email__ReplyToAddress</c>) to a mailbox that
/// receives mail, such as the support inbox. Unset, blank or malformed means no Reply-To header, which is the previous behavior.
/// The contact form does not use this: its Reply-To is the visitor.
/// </summary>
public static class OutgoingEmailReplyTo
{
    public const string ConfigKey = "Email:ReplyToAddress";

    /// <summary>The configured address, or null when there is nothing valid to set.</summary>
    public static string? Resolve(IConfiguration config)
    {
        var configured = config[ConfigKey]?.Trim();
        if (string.IsNullOrEmpty(configured))
        {
            return null;
        }

        var at = configured.IndexOf('@');
        var plain = at > 0
            && at == configured.LastIndexOf('@')
            && at < configured.Length - 3
            && configured.IndexOf('.', at) > at + 1
            && !configured.Any(char.IsWhiteSpace);
        return plain ? configured : null;
    }

    public static void Apply(EmailMessage message, IConfiguration config)
    {
        var address = Resolve(config);
        if (address is not null)
        {
            message.ReplyTo.Add(new EmailAddress(address));
        }
    }
}
