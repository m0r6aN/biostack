namespace BioStack.Infrastructure.Logging;

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

/// <summary>
/// One JSON object per line on stdout. Container Apps ships stdout to Log Analytics
/// (<c>ContainerAppConsoleLogs</c>), where the alert rules in
/// <c>infra/azure/monitoring-alerts.bicep</c> query the <c>level</c> field. Every string
/// passes through <see cref="LogRedactor"/>. Scopes are not written because they can carry
/// request paths and query strings.
/// </summary>
public sealed class RedactingJsonConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "biostack-json";

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
    };

    public RedactingJsonConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options)
        : base(FormatterName)
    {
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null && logEntry.Exception is null)
        {
            return;
        }

        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, WriterOptions))
        {
            json.WriteStartObject();
            json.WriteString("time", DateTimeOffset.UtcNow.ToString("O"));
            json.WriteString("level", logEntry.LogLevel.ToString());
            json.WriteString("category", logEntry.Category);
            if (logEntry.EventId.Id != 0 || logEntry.EventId.Name is not null)
            {
                json.WriteNumber("eventId", logEntry.EventId.Id);
                if (logEntry.EventId.Name is { } name) json.WriteString("eventName", name);
            }

            json.WriteString("message", LogRedactor.Redact(message));

            if (logEntry.Exception is { } ex)
            {
                json.WriteString("exceptionType", ex.GetType().FullName);
                json.WriteString("exception", LogRedactor.Redact(ex.ToString()));
            }

            if (logEntry.State is IReadOnlyList<KeyValuePair<string, object?>> properties && properties.Count > 0)
            {
                json.WriteStartObject("props");
                foreach (var (key, value) in properties)
                {
                    if (key == "{OriginalFormat}")
                    {
                        continue;
                    }

                    switch (value)
                    {
                        case null:
                            json.WriteNull(key);
                            break;
                        case bool b:
                            json.WriteBoolean(key, b);
                            break;
                        case int or long or short or byte or uint or ulong or ushort:
                            json.WriteNumber(key, Convert.ToInt64(value));
                            break;
                        case double or float or decimal:
                            json.WriteNumber(key, Convert.ToDouble(value));
                            break;
                        default:
                            json.WriteString(key, LogRedactor.Redact(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)));
                            break;
                    }
                }
                json.WriteEndObject();
            }

            json.WriteEndObject();
        }

        textWriter.Write(System.Text.Encoding.UTF8.GetString(buffer.ToArray()));
        textWriter.Write(Environment.NewLine);
    }
}

public static class BioStackLoggingExtensions
{
    /// <summary>Console logging as redacted single-line JSON (<see cref="RedactingJsonConsoleFormatter"/>).</summary>
    public static ILoggingBuilder AddBioStackJsonConsole(this ILoggingBuilder logging)
    {
        logging.AddConsole(options => options.FormatterName = RedactingJsonConsoleFormatter.FormatterName);
        logging.AddConsoleFormatter<RedactingJsonConsoleFormatter, ConsoleFormatterOptions>();
        return logging;
    }
}
