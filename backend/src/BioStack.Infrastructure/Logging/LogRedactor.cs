namespace BioStack.Infrastructure.Logging;

using System.Text.RegularExpressions;

/// <summary>
/// Ledger gate G5: removes secrets and personal data from log text before it leaves the process.
/// Applied by <see cref="RedactingJsonConsoleFormatter"/> to the message, every structured
/// property and the exception text. Patterns are deliberately broad. A redacted log line is
/// better than a leaked one.
/// </summary>
public static partial class LogRedactor
{
    public const string Email = "[email]";
    public const string Ip = "[ip]";
    public const string Jwt = "[jwt]";
    public const string Secret = "[redacted]";

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var text = JwtPattern().Replace(value, Jwt);
        text = BearerPattern().Replace(text, "Bearer " + Secret);
        text = StripeKeyPattern().Replace(text, Secret);
        text = KeyValueSecretPattern().Replace(text, m => $"{m.Groups["key"].Value}{m.Groups["sep"].Value}{Secret}");
        text = EmailPattern().Replace(text, Email);
        text = Ipv4Pattern().Replace(text, Ip);
        text = Ipv6Pattern().Replace(text, Ip);
        return text;
    }

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]{5,}\.[A-Za-z0-9_-]{5,}\.[A-Za-z0-9_-]+")]
    private static partial Regex JwtPattern();

    [GeneratedRegex(@"(?i)\bbearer\s+[A-Za-z0-9._~+/=-]{8,}")]
    private static partial Regex BearerPattern();

    [GeneratedRegex(@"\b(?:(?:sk|rk|pk)_(?:live|test)_[A-Za-z0-9]{8,}|whsec_[A-Za-z0-9]{8,})")]
    private static partial Regex StripeKeyPattern();

    // password=..., Secret: ..., token=..., AccountKey=...;, ?code=...&sig=...
    [GeneratedRegex(@"(?i)\b(?<key>password|pwd|passwd|secret|client[_-]?secret|token|access[_-]?token|refresh[_-]?token|api[_-]?key|access[_-]?key|account[_-]?key|shared[_-]?access[_-]?key|sig|signature|code|salt)(?<sep>\s*[=:]\s*)(?!\[redacted\])[^\s;&,""'}]+")]
    private static partial Regex KeyValueSecretPattern();

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)\b")]
    private static partial Regex Ipv4Pattern();

    // Compressed (contains "::"), leading "::1", or uncompressed with at least four groups, so
    // clock times like 12:30:45 are left alone.
    [GeneratedRegex(@"(?i)(?:\b(?:[0-9a-f]{1,4}:){1,6}:(?:[0-9a-f]{1,4}(?::[0-9a-f]{1,4}){0,6})?|(?<![\w:])::[0-9a-f]{1,4}(?::[0-9a-f]{1,4}){0,6}|\b(?:[0-9a-f]{1,4}:){3,7}[0-9a-f]{1,4})\b")]
    private static partial Regex Ipv6Pattern();
}
