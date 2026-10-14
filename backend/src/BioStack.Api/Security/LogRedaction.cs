namespace BioStack.Api.Security;

public static class LogRedaction
{
    public static string EmailDomain(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "missing";
        }

        var at = email.LastIndexOf('@');
        if (at < 1 || at == email.Length - 1)
        {
            return "invalid";
        }

        return email[(at + 1)..].Trim().ToLowerInvariant();
    }
}
