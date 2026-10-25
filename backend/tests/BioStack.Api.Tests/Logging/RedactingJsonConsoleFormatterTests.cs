namespace BioStack.Api.Tests.Logging;

using System.Text.Json;
using BioStack.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>Ledger gate G5: structured logs never carry secrets or personal data.</summary>
public sealed class RedactingJsonConsoleFormatterTests
{
    // Fake credentials are split into concatenated constants so secret scanners don't flag the fixtures.
    [Theory]
    [InlineData("magic link sent to jane.doe+x@example.com", "magic link sent to [email]")]
    [InlineData("client 203.0.113.42 rate limited", "client [ip] rate limited")]
    [InlineData("client 2001:db8:85a3::8a2e:370:7334 seen", "client [ip] seen")]
    [InlineData("loopback ::1 and fe80::1ff:fe23:4567:890a", "loopback [ip] and [ip]")]
    [InlineData("Authorization: " + "Bearer " + "abcdefghijklmnop123", "Authorization: Bearer [redacted]")]
    [InlineData("token " + "eyJ" + "hbGciOiJIUzI1NiJ9" + "." + "eyJ" + "zdWIiOiIxMjMifQ" + "." + "c2lnbmF0dXJl", "token [jwt]")]
    [InlineData("Host=db;Username=app;Password=hunter2!;SSL Mode=Require", "Host=db;Username=app;Password=[redacted];SSL Mode=Require")]
    [InlineData("GET /auth/verify?code=abc123&sig=deadbeef", "GET /auth/verify?code=[redacted]&sig=[redacted]")]
    [InlineData("stripe key " + "sk_" + "live_" + "51Habcdefghijk and " + "whsec_" + "1234567890ab", "stripe key [redacted] and [redacted]")]
    [InlineData("AccountKey=Zm9vYmFy==;EndpointSuffix=core.windows.net", "AccountKey=[redacted];EndpointSuffix=core.windows.net")]
    [InlineData("salt: 0123456789abcdef0123456789abcdef", "salt: [redacted]")]
    public void Redact_Removes_Secrets_And_Personal_Data(string input, string expected)
        => Assert.Equal(expected, LogRedactor.Redact(input));

    [Theory]
    [InlineData("Refresh complete — Scanned=157 Created=0 Updated=3 statusCode=500")]
    [InlineData("Applied migration 20261025020000_AddKnowledgeEntryRetirement")]
    [InlineData("Refresh started at 12:30:45 UTC")]
    [InlineData("GET /api/v1/knowledge/compounds/bpc-157 responded 200 in 12.5 ms")]
    public void Redact_Leaves_Ordinary_Operational_Text_Alone(string input)
        => Assert.Equal(input, LogRedactor.Redact(input));

    [Fact]
    public void Formatter_Writes_One_Redacted_Json_Line_With_Typed_Properties()
    {
        var line = Format(LogLevel.Warning, "Login for {Email} from {Ip} failed {Count} times",
            "jane@example.com", "198.51.100.7", 3);

        Assert.DoesNotContain('\n', line.TrimEnd());
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;
        Assert.Equal("Warning", root.GetProperty("level").GetString());
        Assert.Equal("Test.Category", root.GetProperty("category").GetString());
        Assert.Equal("Login for [email] from [ip] failed 3 times", root.GetProperty("message").GetString());
        var props = root.GetProperty("props");
        Assert.Equal("[email]", props.GetProperty("Email").GetString());
        Assert.Equal("[ip]", props.GetProperty("Ip").GetString());
        Assert.Equal(3, props.GetProperty("Count").GetInt64());
        Assert.False(props.TryGetProperty("{OriginalFormat}", out _));
        Assert.DoesNotContain("jane@example.com", line);
        Assert.DoesNotContain("198.51.100.7", line);
    }

    [Fact]
    public void Formatter_Redacts_Exception_Text()
    {
        var ex = new InvalidOperationException("connect failed: Password=s3cret; user bob@example.com");
        var line = Format(LogLevel.Error, "Database initialization failed", exception: ex);

        using var doc = JsonDocument.Parse(line);
        Assert.Equal("Error", doc.RootElement.GetProperty("level").GetString());
        Assert.Equal(typeof(InvalidOperationException).FullName, doc.RootElement.GetProperty("exceptionType").GetString());
        var exception = doc.RootElement.GetProperty("exception").GetString()!;
        Assert.Contains("Password=[redacted]", exception);
        Assert.DoesNotContain("s3cret", line);
        Assert.DoesNotContain("bob@example.com", line);
    }

    private static string Format(LogLevel level, string template, params object?[] args)
        => Format(level, template, null, args);

    private static string Format(LogLevel level, string template, Exception? exception, params object?[] args)
    {
        var formatter = new RedactingJsonConsoleFormatter(new StaticOptions());
        var state = new FormattedLogValuesProxy(template, args);
        var entry = new LogEntry<FormattedLogValuesProxy>(level, "Test.Category", new EventId(0), state, exception, (s, _) => s.ToString());
        using var writer = new StringWriter();
        formatter.Write(entry, null, writer);
        return writer.ToString();
    }

    /// <summary>Mirrors the framework's message-template state: properties plus {OriginalFormat}.</summary>
    private sealed class FormattedLogValuesProxy(string template, object?[] args) : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private readonly List<KeyValuePair<string, object?>> _values = Build(template, args);

        private static List<KeyValuePair<string, object?>> Build(string template, object?[] args)
        {
            var names = System.Text.RegularExpressions.Regex.Matches(template, @"\{(\w+)\}").Select(m => m.Groups[1].Value).ToList();
            var list = names.Select((n, i) => new KeyValuePair<string, object?>(n, args[i])).ToList();
            list.Add(new("{OriginalFormat}", template));
            return list;
        }

        public override string ToString()
        {
            var text = template;
            foreach (var (key, value) in _values.Where(v => v.Key != "{OriginalFormat}")) text = text.Replace("{" + key + "}", Convert.ToString(value));
            return text;
        }

        public KeyValuePair<string, object?> this[int index] => _values[index];
        public int Count => _values.Count;
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _values.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class StaticOptions : IOptionsMonitor<ConsoleFormatterOptions>
    {
        public ConsoleFormatterOptions CurrentValue { get; } = new();
        public ConsoleFormatterOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<ConsoleFormatterOptions, string?> listener) => null;
    }
}
