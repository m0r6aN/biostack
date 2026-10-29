namespace BioStack.Api.AccountData;

using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using BioStack.Api.Endpoints;
using BioStack.Infrastructure.Keon;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed record AccountExportDocument(
    string ExportFormat,
    int ExportVersion,
    DateTime GeneratedAtUtc,
    Guid UserId,
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> Tables,
    IReadOnlyList<ExportNote> RetainedButNotDeleted,
    IReadOnlyList<ExportNote> NotIncluded);

public sealed record ExportNote(string Table, string Why);

public sealed class AccountScopeResolver(BioStackDbContext db, IConfiguration config)
{
    /// <summary>Builds the scope for one signed-in user, or null if the account no longer exists.</summary>
    public async Task<AccountScope?> ResolveAsync(Guid userId, CancellationToken ct)
    {
        var email = await db.AppUsers.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync(ct);
        if (email is null) return null;

        var identityEmails = await db.AuthIdentities.AsNoTracking()
            .Where(i => i.UserId == userId && i.Type == "email")
            .Select(i => i.ValueNormalized)
            .ToListAsync(ct);
        var emails = identityEmails.Append(email)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new AccountScope(
            userId,
            emails,
            ReceiptActor.User(userId).ActorId,
            ClaimCheckEndpoints.SubmitterKeyForUser(userId, config));
    }
}

/// <summary>Builds the downloadable copy of everything BioStack holds about one person.</summary>
public sealed class AccountExportService(BioStackDbContext db)
{
    public const string Format = "biostack-account-export";
    public const int Version = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<AccountExportDocument> BuildAsync(AccountScope scope, CancellationToken ct)
    {
        var tables = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var table in AccountDataCatalog.ExportedTables)
        {
            var rows = await table.ExportAsync(db, scope, ct);
            tables[table.Name] = rows;
            counts[table.Name] = rows.Count;
        }

        var retained = AccountDataCatalog.Tables
            .Where(t => t.Disposition == UserDataDisposition.RetainedPseudonymous)
            .Select(t => new ExportNote(t.Name, t.Note))
            .ToList();
        var notIncluded = AccountDataCatalog.Tables
            .Where(t => !t.IsExported)
            .Select(t => new ExportNote(t.Name, $"{t.Disposition}: {t.Note}"))
            .ToList();

        return new AccountExportDocument(Format, Version, DateTime.UtcNow, scope.UserId, counts, tables, retained, notIncluded);
    }

    public static byte[] ToJson(AccountExportDocument document) => JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions);

    /// <summary>One CSV per table, zipped, plus a README that says what each file is.</summary>
    public static byte[] ToCsvZip(AccountExportDocument document)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, rows) in document.Tables)
            {
                var entry = zip.CreateEntry($"{name}.csv", CompressionLevel.Optimal);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                WriteCsv(writer, rows);
            }

            var readme = zip.CreateEntry("README.txt");
            using var readmeWriter = new StreamWriter(readme.Open(), new UTF8Encoding(false));
            readmeWriter.Write(
                $"BioStack account export (format {document.ExportFormat} v{document.ExportVersion}), generated {document.GeneratedAtUtc:u}.\n"
                + "One CSV per table of data BioStack holds about you. Dates are UTC. Empty files mean nothing is stored in that table.\n"
                + "Text cells that begin with = + - or @ are prefixed with an apostrophe so spreadsheets don't run them as formulas.\n"
                + "The JSON export has the same data plus notes on what is retained or not included.\n");
        }

        return buffer.ToArray();
    }

    private static void WriteCsv(StreamWriter writer, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return;
        var columns = rows[0].Keys.ToList();
        writer.Write(string.Join(',', columns.Select(Escape)));
        writer.Write("\r\n");
        foreach (var row in rows)
        {
            writer.Write(string.Join(',', columns.Select(c => Escape(CellText(row.GetValueOrDefault(c))))));
            writer.Write("\r\n");
        }
    }

    private static string CellText(object? value) => value switch
    {
        null => string.Empty,
        string s => GuardFormula(s),
        DateTime d => d.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    private static string GuardFormula(string s)
        => s.Length > 0 && s[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + s : s;

    private static string Escape(string value)
        => value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
}
