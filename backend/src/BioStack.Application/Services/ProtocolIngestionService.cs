namespace BioStack.Application.Services;

using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using BioStack.Contracts.Requests;
using Microsoft.Extensions.Logging;

public sealed class ProtocolIngestionService : IProtocolIngestionService
{
    private static readonly TimeSpan IngestionCacheTtl = TimeSpan.FromDays(7);

    private readonly IEnumerable<IProtocolTextExtractor> _extractors;
    private readonly IProtocolNormalizationService _normalizationService;
    private readonly IProtocolFingerprintService _fingerprintService;
    private readonly IProtocolAnalysisCache _cache;
    private readonly ILogger<ProtocolIngestionService> _logger;

    public ProtocolIngestionService(
        IEnumerable<IProtocolTextExtractor> extractors,
        IProtocolNormalizationService normalizationService,
        IProtocolFingerprintService fingerprintService,
        IProtocolAnalysisCache cache,
        ILogger<ProtocolIngestionService> logger)
    {
        _extractors = extractors;
        _normalizationService = normalizationService;
        _fingerprintService = fingerprintService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProtocolIngestionResult> IngestAsync(ProtocolIngestionRequest request, CancellationToken cancellationToken = default)
    {
        var ingestionFingerprint = _fingerprintService.GetIngestionFingerprint(request);
        var ingestionKey = _fingerprintService.GetIngestionCacheKey(request, ingestionFingerprint);
        var cached = await _cache.GetIngestionAsync(ingestionKey, cancellationToken);
        if (cached is not null)
        {
            return Map(cached);
        }

        var extractor = _extractors.FirstOrDefault(candidate => candidate.CanHandle(request))
            ?? throw new ProtocolIngestionException("This input type is not supported yet.");

        var extraction = await extractor.ExtractAsync(request, cancellationToken);
        var normalizedText = _normalizationService.NormalizeExtractedText(extraction.ExtractedText);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            var warning = extraction.Warnings.FirstOrDefault() ?? "We could not extract readable protocol text from that source.";
            throw new ProtocolIngestionException(warning);
        }

        var parseFingerprint = _fingerprintService.GetNormalizedTextHash(normalizedText);
        var result = new ProtocolIngestionResult(
            extraction.ExtractedText,
            normalizedText,
            request.InputType,
            request.SourceName,
            extraction.Warnings,
            extraction.Artifacts,
            extraction.LowConfidence,
            ingestionFingerprint,
            parseFingerprint);

        await _cache.SetIngestionAsync(
            ingestionKey,
            new IngestionCacheDto(
                result.ExtractedText,
                result.NormalizedText,
                result.InputType,
                result.SourceName,
                result.Warnings.ToList(),
                result.Artifacts.Select(artifact => new ProtocolIngestionArtifactDto(artifact.Kind, artifact.Label, artifact.Preview)).ToList(),
                result.LowConfidence,
                result.IngestionFingerprint,
                result.ParseFingerprint),
            IngestionCacheTtl,
            cancellationToken);

        _logger.LogInformation(
            "Protocol ingestion complete. InputType={InputType} SourceName={SourceName} IngestionKey={IngestionKey}",
            request.InputType,
            request.SourceName,
            ingestionKey);

        return result;
    }

    private static ProtocolIngestionResult Map(IngestionCacheDto cached) =>
        new(
            cached.ExtractedText,
            cached.NormalizedText,
            cached.InputType,
            cached.SourceName,
            cached.Warnings,
            cached.Artifacts.Select(artifact => new ProtocolIngestionArtifact(artifact.Kind, artifact.Label, artifact.Preview)).ToList(),
            cached.LowConfidence,
            cached.IngestionFingerprint,
            cached.ParseFingerprint);
}

public interface IProtocolIngestionService
{
    Task<ProtocolIngestionResult> IngestAsync(ProtocolIngestionRequest request, CancellationToken cancellationToken = default);
}

public interface IProtocolTextExtractor
{
    bool CanHandle(ProtocolIngestionRequest request);
    Task<ProtocolExtractionResult> ExtractAsync(ProtocolIngestionRequest request, CancellationToken cancellationToken = default);
}

public interface IProtocolOcrService
{
    Task<ProtocolOcrResult> ExtractAsync(byte[] imageBytes, string? sourceName, CancellationToken cancellationToken = default);
}

public sealed class PlainTextProtocolExtractor : IProtocolTextExtractor
{
    public bool CanHandle(ProtocolIngestionRequest request) =>
        request.InputType == ProtocolInputType.Paste;

    public Task<ProtocolExtractionResult> ExtractAsync(ProtocolIngestionRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ProtocolExtractionResult(
            request.InputText ?? string.Empty,
            Array.Empty<string>(),
            Array.Empty<ProtocolIngestionArtifact>(),
            false));
    }
}

public sealed class PdfProtocolExtractor : IProtocolTextExtractor
{
    // Decompression-bomb guard for FlateDecode streams (per-stream and total).
    private const long MaxInflatedStreamBytes = 16L * 1024 * 1024;
    private const long MaxTotalInflatedBytes = 64L * 1024 * 1024;

    private static readonly Encoding PdfBytes = Encoding.GetEncoding("ISO-8859-1");

    // Locates stream payloads together with the object dictionary that precedes
    // them, so /Filter /FlateDecode and /Subtype /Image can be detected. The
    // optional newline after the "stream" keyword is tolerated: several
    // real-world producers write "stream\n" only. ISO-8859-1 is a byte-for-byte
    // encoding, so capture indices in the decoded string map 1:1 onto positions
    // in the original byte array.
    private static readonly Regex ContentStreamRegex = new(
        @"(?:(?<dict><<(?:[^<>]|<<[^<>]*>>)*>>)[\s\r\n]*)?\bstream(?<eol>\r?\n)(?<data>.*?)[\r\n]*\bendstream",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public bool CanHandle(ProtocolIngestionRequest request) =>
        request.InputType == ProtocolInputType.FileUpload &&
        ProtocolExtractorSupport.MatchesContentTypeOrExtension(request, [".pdf"], ["application/pdf"]);

    public Task<ProtocolExtractionResult> ExtractAsync(ProtocolIngestionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SourceBytes is null || request.SourceBytes.Length == 0)
        {
            throw new ProtocolIngestionException("The uploaded PDF was empty.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var warnings = new List<string>();
        var artifacts = new List<ProtocolIngestionArtifact>();
        var decoded = PdfBytes.GetString(request.SourceBytes);
        cancellationToken.ThrowIfCancellationRequested();
        var pageCount = Regex.Matches(decoded, @"/Type\s*/Page\b", RegexOptions.IgnoreCase).Count;
        cancellationToken.ThrowIfCancellationRequested();
        var extractedLines = ExtractPdfText(request.SourceBytes, decoded, warnings, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var (line, index) in extractedLines.Select((line, index) => (line, index)))
        {
            var preview = ProtocolExtractorSupport.CreatePreview(line);
            if (!string.IsNullOrWhiteSpace(preview))
            {
                artifacts.Add(new ProtocolIngestionArtifact("page", $"Block {index + 1}", preview));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var text = string.Join(Environment.NewLine, extractedLines).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ProtocolIngestionException("This PDF did not expose readable text. Try a clearer source file or a direct image scan.");
        }

        if (pageCount > 0 && text.Length / pageCount < 120)
        {
            warnings.Add("This PDF appears to be image-heavy or low text density. Review the extracted protocol carefully.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ProtocolExtractionResult(
            text,
            warnings,
            artifacts,
            warnings.Count > 0));
    }

    // Builds the text pool from the document's content streams in order,
    // inflating FlateDecode payloads first (the encoding virtually every
    // real-world producer uses, including Word and Google Docs exports).
    // Image streams are skipped so binary pixel data never competes with text
    // operators. Documents with no locatable stream bodies fall back to the
    // historic whole-file scan.
    private static IReadOnlyList<string> ExtractPdfText(byte[] sourceBytes, string content, ICollection<string> warnings, CancellationToken cancellationToken)
    {
        var streamTexts = new List<string>();
        var totalInflated = 0L;
        var undecodedStreams = 0;
        var truncatedStreams = 0;
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Match match in ContentStreamRegex.Matches(content))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dictionary = match.Groups["dict"].Value;
            if (dictionary.Contains("/Image", StringComparison.Ordinal))
            {
                continue;
            }

            var data = match.Groups["data"];
            string streamText;
            if (dictionary.Contains("FlateDecode", StringComparison.Ordinal))
            {
                if (totalInflated >= MaxTotalInflatedBytes)
                {
                    truncatedStreams++;
                    continue;
                }

                var budget = Math.Min(MaxInflatedStreamBytes, MaxTotalInflatedBytes - totalInflated);
                if (!TryInflateFlate(sourceBytes, data.Index, data.Length, budget, out streamText, out var hitBudget))
                {
                    undecodedStreams++;
                    continue;
                }

                totalInflated += streamText.Length;
                if (hitBudget)
                {
                    truncatedStreams++;
                }
            }
            else
            {
                streamText = data.Value;
            }

            streamTexts.Add(streamText);
        }

        if (undecodedStreams > 0)
        {
            warnings.Add("This PDF contains compressed streams that could not be fully decoded. Review the extracted protocol carefully.");
        }

        if (truncatedStreams > 0)
        {
            warnings.Add("This PDF contains compressed streams that were truncated during decoding. Review the extracted protocol carefully.");
        }

        if (streamTexts.Count == 0)
        {
            // Fallback: no locatable stream bodies (unusual or damaged layout).
            // Preserve the historic behavior of scanning the whole document.
            streamTexts.Add(content);
        }

        return streamTexts
            .SelectMany(streamText => ExtractTextOperators(streamText, cancellationToken))
            .ToList();
    }

    private static bool TryInflateFlate(byte[] sourceBytes, int offset, int length, long budget, out string text, out bool hitBudget)
    {
        text = string.Empty;
        hitBudget = false;
        if (length <= 0 || offset < 0 || offset + length > sourceBytes.Length)
        {
            return false;
        }

        // Spec-compliant FlateDecode payloads carry a zlib header; some
        // real-world producers omit it (raw deflate) or write a broken header,
        // so retry without the header when the first attempt yields nothing.
        using var output = new MemoryStream();
        using (var input = new MemoryStream(sourceBytes, offset, length, writable: false))
        {
            InflateInto(input, output, budget, mode: 0, ref hitBudget); // zlib
            if (output.Length == 0)
            {
                input.Position = 0;
                InflateInto(input, output, budget, mode: 1, ref hitBudget); // raw deflate from offset 0
            }

            if (output.Length == 0 && length > 2)
            {
                input.Position = 2;
                hitBudget = false;
                InflateInto(input, output, budget, mode: 1, ref hitBudget); // raw deflate past bad header
            }
        }

        if (output.Length == 0)
        {
            return false;
        }

        text = PdfBytes.GetString(output.GetBuffer(), 0, (int)output.Length);
        return true;
    }

    // mode 0 = zlib-wrapped deflate (spec-compliant FlateDecode),
    // mode 1 = raw deflate stream (missing or damaged zlib header).
    private static void InflateInto(Stream input, MemoryStream output, long budget, int mode, ref bool hitBudget)
    {
        var buffer = new byte[81920];
        int read;
        using Stream decompressor = mode == 1
            ? new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true)
            : new ZLibStream(input, CompressionMode.Decompress, leaveOpen: true);

        try
        {
            while ((read = decompressor.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (output.Length + read > budget)
                {
                    hitBudget = true;
                    return;
                }

                output.Write(buffer, 0, read);
            }
        }
        catch (InvalidDataException)
        {
            // Corrupt or truncated payload: keep whatever decompressed cleanly.
        }
    }

    private static IEnumerable<string> ExtractTextOperators(string content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directText = Regex.Matches(content, @"\((?<text>(?:\\\)|\\\(|\\\\|[^\)])+)\)\s*Tj", RegexOptions.Singleline)
            .Select(match => DecodePdfString(match.Groups["text"].Value))
            .ToList();

        cancellationToken.ThrowIfCancellationRequested();
        var arrayText = Regex.Matches(content, @"\[(?<items>.*?)\]\s*TJ", RegexOptions.Singleline)
            .Select(match => string.Join(" ", Regex.Matches(match.Groups["items"].Value, @"\((?<text>(?:\\\)|\\\(|\\\\|[^\)])+)\)")
                .Select(inner => DecodePdfString(inner.Groups["text"].Value))))
            .ToList();

        cancellationToken.ThrowIfCancellationRequested();
        var extractedText = directText
            .Concat(arrayText)
            .Select(value => Regex.Replace(value, @"\s+", " ").Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
        cancellationToken.ThrowIfCancellationRequested();
        return extractedText;
    }

    private static string DecodePdfString(string value)
    {
        return value
            .Replace(@"\(", "(")
            .Replace(@"\)", ")")
            .Replace(@"\n", " ")
            .Replace(@"\r", " ")
            .Replace(@"\t", " ")
            .Replace(@"\\", "\\");
    }
}

public sealed class DocxProtocolExtractor : IProtocolTextExtractor
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public bool CanHandle(ProtocolIngestionRequest request) =>
        request.InputType == ProtocolInputType.FileUpload &&
        ProtocolExtractorSupport.MatchesContentTypeOrExtension(
            request,
            [".docx"],
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"]);

    public Task<ProtocolExtractionResult> ExtractAsync(ProtocolIngestionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SourceBytes is null || request.SourceBytes.Length == 0)
        {
            throw new ProtocolIngestionException("The uploaded DOCX was empty.");
        }

        try
        {
            using var stream = new MemoryStream(request.SourceBytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var entry = archive.GetEntry("word/document.xml")
                ?? throw new ProtocolIngestionException("The DOCX file did not contain a readable document body.");

            using var entryStream = entry.Open();
            var document = XDocument.Load(entryStream);
            var blocks = new List<string>();

            foreach (var element in document.Root?.Descendants(W + "body").Elements() ?? Enumerable.Empty<XElement>())
            {
                if (element.Name == W + "p")
                {
                    var text = string.Concat(element.Descendants(W + "t").Select(node => node.Value)).Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        blocks.Add(text);
                    }

                    continue;
                }

                if (element.Name == W + "tbl")
                {
                    foreach (var row in element.Descendants(W + "tr"))
                    {
                        var cells = row.Descendants(W + "tc")
                            .Select(cell => string.Concat(cell.Descendants(W + "t").Select(node => node.Value)).Trim())
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .ToList();

                        if (cells.Count > 0)
                        {
                            blocks.Add(string.Join(" | ", cells));
                        }
                    }
                }
            }

            return Task.FromResult(new ProtocolExtractionResult(
                string.Join(Environment.NewLine, blocks),
                Array.Empty<string>(),
                Array.Empty<ProtocolIngestionArtifact>(),
                false));
        }
        catch (ProtocolIngestionException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            throw new ProtocolIngestionException("The DOCX file appears to be corrupted or is not a valid Word document.");
        }
        catch (XmlException)
        {
            throw new ProtocolIngestionException("The DOCX file contains malformed content and could not be read.");
        }
    }
}

public sealed class SpreadsheetProtocolExtractor : IProtocolTextExtractor
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationship = "http://schemas.openxmlformats.org/package/2006/relationships";

    private const string MalformedContentMessage = "The spreadsheet contains malformed content and could not be read.";

    // Excel's last column is XFD (16384 columns); cell references past it are ignored.
    private const int MaxColumns = 16384;

    private const string DoseUnits = @"mcg|micrograms?|ug|\u03bcg|\u00b5g|mg|milligrams?";

    // Private copies of ProtocolParser's dose/frequency/duration/cycle patterns (the parser is
    // untouched by BIO-ANALYZER-004). Keep in sync with ProtocolParser.
    private static readonly Regex DosePattern = new(
        @"(?<dose>(?:\d+(?:\.\d+)?|\.\d+))\s*(?<unit>" + DoseUnits + @")\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Keep in sync with ProtocolParser.
    private static readonly Regex DurationPattern = new(
        @"(?<duration>\b\d+(?:\.\d+)?\s*(?:day|days|week|weeks|month|months|cycle|cycles)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Keep in sync with ProtocolParser.
    private static readonly Regex FrequencyPattern = new(
        @"\b(?<frequency>daily|twice daily|once daily|weekly|twice weekly|three times weekly|3x weekly|2x weekly|every other day|eod|morning|nightly|evening|pre[-\s]?workout|post[-\s]?workout)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Keep in sync with ProtocolParser.
    private static readonly Regex CyclePattern = new(
        @"\b\d+\s*(?:w|wk|wks|week|weeks|d|day|days|month|months)\b\s*(?:on|off)\b.*?\b(?:on|off)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // A number, an optional dose unit, a range separator or "to", then another number
    // ("250-500mcg", "250mcg-500mcg", "0.5 to 1 mg"). Deliberately fail-safe: may also match
    // dates or "(days 1-5)", whose dose is then omitted.
    private static readonly Regex RangePattern = new(
        @"(?:\d+(?:\.\d+)?|\.\d+)\s*(?:(?:" + DoseUnits + @")\b)?\s*(?:[-\u2013\u2014]|\bto\b)\s*(?:\d|\.\d)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DigitCommaPattern = new(@"\d,\d", RegexOptions.Compiled);
    private static readonly Regex BareNumberPattern = new(@"^(?:\d+(?:\.\d+)?|\.\d+)$", RegexOptions.Compiled);
    private static readonly Regex UnitOnlyPattern = new("^(?:" + DoseUnits + ")$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CellReferencePattern = new(@"^(?<column>[A-Za-z]{1,3})[0-9]+\z", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRunPattern = new(@"[\s\u00A0]+", RegexOptions.Compiled);
    private static readonly Regex AsciiLetterPattern = new("[A-Za-z]", RegexOptions.Compiled);
    private static readonly Regex NonLetterRunPattern = new(@"[^\p{L}]+", RegexOptions.Compiled);
    private static readonly Regex TrailingParentheticalPattern = new(@"\((?<hint>[^()]*)\)\s*$", RegexOptions.Compiled);
    private static readonly Regex EmptyBracketPairPattern = new(@"\(\s*\)|\[\s*\]", RegexOptions.Compiled);
    private static readonly Regex SeparatorOnlyTokenPattern = new("^[-\u2013\u2014:,/]+$", RegexOptions.Compiled);
    private static readonly Regex MultiCompoundSeparatorPattern = new(@"\s*;\s*|\s*\|\s*|\s\+\s", RegexOptions.Compiled);

    // Closed header-role vocabulary (exact match after normalisation). Extending it needs a new spec.
    private static readonly HashSet<string> NameHeaders = new(StringComparer.Ordinal)
    {
        "compound", "compounds", "name", "peptide", "product", "substance", "item", "medication", "supplement", "drug", "agent",
    };

    // Index = priority.
    private static readonly string[] DoseHeaders = ["dose", "dosage", "amount", "strength", "quantity"];
    private static readonly HashSet<string> UnitHeaders = new(["unit", "units"], StringComparer.Ordinal);
    private static readonly HashSet<string> FrequencyHeaders = new(["frequency", "freq", "schedule"], StringComparer.Ordinal);
    private static readonly HashSet<string> DurationHeaders = new(["duration", "length"], StringComparer.Ordinal);

    // Sparse row: only non-blank cells are stored, keyed by zero-based column index.
    private sealed class SheetRow
    {
        public Dictionary<int, string> Cells { get; } = new();

        public bool HasContentPastLimit { get; set; }

        public bool HasContent => HasContentPastLimit || Cells.Count > 0;

        public void Set(int column, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Cells.Remove(column);
            }
            else
            {
                Cells[column] = value;
            }
        }
    }

    // Shared with the lazy worksheet reader: narrowed to the header width once the header is known.
    private sealed class ColumnLimit
    {
        public int Value { get; set; } = MaxColumns;
    }

    private sealed class ColumnMap
    {
        public int Width { get; init; }
        public List<int> Names { get; } = [];
        public List<(int Index, string UnitHint)> Doses { get; } = [];
        public List<int> Units { get; } = [];
        public List<int> Frequencies { get; } = [];
        public List<int> Durations { get; } = [];
    }

    private sealed record SheetText(string Text, int SkippedRows, bool HadRows);

    public bool CanHandle(ProtocolIngestionRequest request) =>
        request.InputType == ProtocolInputType.FileUpload &&
        ProtocolExtractorSupport.MatchesContentTypeOrExtension(
            request,
            [".xlsx", ".csv"],
            ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "text/csv", "application/csv"]);

    public Task<ProtocolExtractionResult> ExtractAsync(ProtocolIngestionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SourceBytes is null || request.SourceBytes.Length == 0)
        {
            throw new ProtocolIngestionException("The uploaded spreadsheet was empty.");
        }

        if (ProtocolExtractorSupport.HasExtension(request.SourceName, ".csv") || string.Equals(request.ContentType, "text/csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvText = Encoding.UTF8.GetString(request.SourceBytes);
            var converted = ConvertRowsToText("CSV", ReadCsvRows(csvText), limit: null);
            return Task.FromResult(new ProtocolExtractionResult(
                converted.Text,
                BuildWarnings(converted.SkippedRows),
                Array.Empty<ProtocolIngestionArtifact>(),
                false));
        }

        try
        {
            using var stream = new MemoryStream(request.SourceBytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

            var workbook = LoadXml(archive, "xl/workbook.xml");
            var relationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");
            var sharedStrings = LoadSharedStrings(archive);
            var sheetMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in relationships.Root?.Elements(Relationship + "Relationship") ?? Enumerable.Empty<XElement>())
            {
                var id = element.Attribute("Id")?.Value;
                var rawTarget = element.Attribute("Target")?.Value;
                if (id is null || rawTarget is null)
                {
                    continue;
                }

                if (!sheetMap.TryAdd(id, rawTarget))
                {
                    throw new ProtocolIngestionException("The spreadsheet contains malformed content and could not be read.");
                }
            }

            var blocks = new List<string>();
            var skippedRows = 0;
            foreach (var sheet in workbook.Root?.Descendants(Spreadsheet + "sheet") ?? Enumerable.Empty<XElement>())
            {
                var sheetName = sheet.Attribute("name")?.Value ?? "Sheet";
                var relationId = sheet.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "id")?.Value;
                if (string.IsNullOrWhiteSpace(relationId) || !sheetMap.TryGetValue(relationId, out var target))
                {
                    continue;
                }

                var worksheet = LoadXml(archive, ResolveWorkbookTarget(target));
                var limit = new ColumnLimit();
                var converted = ConvertRowsToText(sheetName, ReadWorksheetRows(worksheet, sharedStrings, limit), limit);
                skippedRows += converted.SkippedRows;
                if (converted.HadRows)
                {
                    blocks.Add(converted.Text);
                }
            }

            return Task.FromResult(new ProtocolExtractionResult(
                string.Join(Environment.NewLine + Environment.NewLine, blocks),
                BuildWarnings(skippedRows),
                Array.Empty<ProtocolIngestionArtifact>(),
                false));
        }
        catch (ProtocolIngestionException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            throw new ProtocolIngestionException("The spreadsheet appears to be corrupted or is not a valid XLSX file.");
        }
        catch (XmlException)
        {
            throw new ProtocolIngestionException("The spreadsheet contains malformed content and could not be read.");
        }
    }

    private static XDocument LoadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path)
            ?? throw new ProtocolIngestionException("The spreadsheet is missing required content and could not be read.");

        using var entryStream = entry.Open();
        return XDocument.Load(entryStream);
    }

    private static string ResolveWorkbookTarget(string rawTarget)
    {
        var target = rawTarget.Replace('\\', '/');
        var segments = new List<string>();
        if (!target.StartsWith('/'))
        {
            segments.Add("xl");
        }

        foreach (var segment in target.Split('/'))
        {
            if (segment.Length == 0 || segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    throw new ProtocolIngestionException("The spreadsheet contains malformed content and could not be read.");
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        if (segments.Count == 0)
        {
            throw new ProtocolIngestionException("The spreadsheet contains malformed content and could not be read.");
        }

        return string.Join('/', segments);
    }

    private static List<string> LoadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document.Root?
            .Descendants(Spreadsheet + "si")
            .Select(item => string.Concat(item.Descendants(Spreadsheet + "t").Select(node => node.Value)))
            .ToList()
            ?? [];
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;
        if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
        {
            // Streaming writers store text in <is><t>..</t></is> (possibly several rich-text runs).
            var inline = cell.Element(Spreadsheet + "is");
            return inline is null
                ? string.Empty
                : string.Concat(inline.Descendants(Spreadsheet + "t").Select(node => node.Value));
        }

        var raw = cell.Element(Spreadsheet + "v")?.Value?.Trim() ?? string.Empty;
        if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(raw, out var sharedIndex) &&
            sharedIndex >= 0 &&
            sharedIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedIndex];
        }

        return raw;
    }

    // Column index (A = 0) of a cell. A cell with no `r`, or a malformed one, follows the previous
    // cell. The result is not capped here; callers drop indexes >= MaxColumns.
    private static int ResolveColumn(string? reference, int previousColumn)
    {
        if (reference is not null)
        {
            var match = CellReferencePattern.Match(reference);
            if (match.Success)
            {
                var column = 0;
                foreach (var letter in match.Groups["column"].Value)
                {
                    column = (column * 26) + (char.ToUpperInvariant(letter) - 'A' + 1);
                }

                return column - 1;
            }
        }

        return previousColumn + 1;
    }

    // Rows are produced lazily so that, once the header row has fixed the table width, later rows
    // only materialise columns below it (limit.Value). Cells past the limit are still inspected
    // for content so a row whose only data sits past the header is not mistaken for a blank row.
    private static IEnumerable<SheetRow> ReadWorksheetRows(XDocument worksheet, IReadOnlyList<string> sharedStrings, ColumnLimit limit)
    {
        foreach (var rowElement in worksheet.Root?.Descendants(Spreadsheet + "row") ?? Enumerable.Empty<XElement>())
        {
            var row = new SheetRow();
            var previousColumn = -1;
            foreach (var cell in rowElement.Elements(Spreadsheet + "c"))
            {
                var column = ResolveColumn(cell.Attribute("r")?.Value, previousColumn);
                previousColumn = column;
                if (column >= MaxColumns)
                {
                    continue;
                }

                var value = ReadCellValue(cell, sharedStrings);
                if (column < limit.Value)
                {
                    row.Set(column, value);
                }
                else if (!string.IsNullOrWhiteSpace(value))
                {
                    row.HasContentPastLimit = true;
                }
            }

            yield return row;
        }
    }

    // RFC 4180 reader. Delimiter is ',' only; row breaks are \r\n, \n or a lone \r outside quotes;
    // a field is quoted only when its first non-space character is '"'.
    private static List<SheetRow> ReadCsvRows(string csvText)
    {
        var rows = new List<SheetRow>();
        var position = csvText.Length > 0 && csvText[0] == '\uFEFF' ? 1 : 0;
        while (position < csvText.Length)
        {
            var row = new SheetRow();
            var column = 0;
            while (true)
            {
                var field = ReadCsvField(csvText, ref position);
                if (column < MaxColumns)
                {
                    row.Set(column, field);
                }

                column++;
                if (position < csvText.Length && csvText[position] == ',')
                {
                    position++;
                    continue;
                }

                if (position < csvText.Length)
                {
                    position += csvText[position] == '\r' && position + 1 < csvText.Length && csvText[position + 1] == '\n' ? 2 : 1;
                }

                break;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string ReadCsvField(string text, ref int position)
    {
        var start = position;
        var cursor = position;
        while (cursor < text.Length && (text[cursor] == ' ' || text[cursor] == '\t'))
        {
            cursor++;
        }

        if (cursor < text.Length && text[cursor] == '"')
        {
            var value = new StringBuilder();
            cursor++;
            var closed = false;
            while (cursor < text.Length)
            {
                if (text[cursor] == '"')
                {
                    if (cursor + 1 < text.Length && text[cursor + 1] == '"')
                    {
                        value.Append('"');
                        cursor += 2;
                        continue;
                    }

                    cursor++;
                    closed = true;
                    break;
                }

                value.Append(text[cursor]);
                cursor++;
            }

            if (!closed)
            {
                throw new ProtocolIngestionException(MalformedContentMessage);
            }

            // Text after the closing quote in the same field is appended literally.
            while (cursor < text.Length && text[cursor] != ',' && text[cursor] != '\r' && text[cursor] != '\n')
            {
                value.Append(text[cursor]);
                cursor++;
            }

            position = cursor;
            return value.ToString();
        }

        while (cursor < text.Length && text[cursor] != ',' && text[cursor] != '\r' && text[cursor] != '\n')
        {
            cursor++;
        }

        position = cursor;
        return text.Substring(start, cursor - start).Trim();
    }

    private static string Sanitise(string value) =>
        WhitespaceRunPattern.Replace(value, " ").Trim();

    private static SheetText ConvertRowsToText(string sheetName, IEnumerable<SheetRow> rows, ColumnLimit? limit)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Sheet: {sheetName}");

        var skippedRows = 0;
        var hadRows = false;
        ColumnMap? columns = null;
        foreach (var row in rows)
        {
            if (!row.HasContent)
            {
                continue;
            }

            if (!hadRows)
            {
                // The first non-blank row is the header when it has any letter (unchanged semantics).
                hadRows = true;
                if (row.Cells.Values.Any(value => AsciiLetterPattern.IsMatch(value)))
                {
                    columns = BuildColumnMap(row);
                    if (columns is not null && limit is not null)
                    {
                        limit.Value = columns.Width;
                    }

                    continue;
                }
            }

            if (columns is null)
            {
                AppendValuesOnly(builder, row);
            }
            else if (!AppendReconstructed(builder, row, columns))
            {
                skippedRows++;
            }
        }

        return new SheetText(builder.ToString().Trim(), skippedRows, hadRows);
    }

    // One aggregated entry, no sheet name and no row content.
    private static IReadOnlyList<string> BuildWarnings(int skippedRows) =>
        skippedRows > 0
            ? [$"{skippedRows} row(s) skipped: no compound name found."]
            : Array.Empty<string>();

    // Returns null when the header row has no name-role column (values-only fallback).
    private static ColumnMap? BuildColumnMap(SheetRow header)
    {
        var map = new ColumnMap { Width = header.Cells.Keys.Max() + 1 };
        var doseColumns = new List<(int Index, int Priority, string UnitHint)>();
        foreach (var (index, raw) in header.Cells.OrderBy(cell => cell.Key))
        {
            var name = NormalizeHeader(raw, out var unitHint);
            var priority = Array.IndexOf(DoseHeaders, name);
            if (NameHeaders.Contains(name))
            {
                map.Names.Add(index);
            }
            else if (priority >= 0)
            {
                doseColumns.Add((index, priority, unitHint));
            }
            else if (UnitHeaders.Contains(name))
            {
                map.Units.Add(index);
            }
            else if (FrequencyHeaders.Contains(name))
            {
                map.Frequencies.Add(index);
            }
            else if (DurationHeaders.Contains(name))
            {
                map.Durations.Add(index);
            }
        }

        if (map.Names.Count == 0)
        {
            return null;
        }

        foreach (var column in doseColumns.OrderBy(column => column.Priority).ThenBy(column => column.Index))
        {
            map.Doses.Add((column.Index, column.UnitHint));
        }

        return map;
    }

    // Lowercased header with BOM/NBSP stripped, one trailing parenthetical captured as the unit hint,
    // and every run of non-letters collapsed to a single space.
    private static string NormalizeHeader(string raw, out string unitHint)
    {
        var value = raw.Replace("\uFEFF", string.Empty).Replace('\u00A0', ' ').Trim().ToLowerInvariant();
        unitHint = string.Empty;
        var parenthetical = TrailingParentheticalPattern.Match(value);
        if (parenthetical.Success)
        {
            unitHint = Sanitise(parenthetical.Groups["hint"].Value);
            value = value[..parenthetical.Index];
        }

        return NonLetterRunPattern.Replace(value, " ").Trim();
    }

    private static void AppendValuesOnly(StringBuilder builder, SheetRow row)
    {
        var values = row.Cells
            .OrderBy(cell => cell.Key)
            .Select(cell => Sanitise(cell.Value))
            .Where(value => value.Length > 0);
        builder.AppendLine(string.Join(" | ", values));
    }

    // Emits one `<name> <dose> <frequency> <duration>` line (or one name-only line per part of a
    // multi-compound name cell). Returns false when the row has no usable name.
    private static bool AppendReconstructed(StringBuilder builder, SheetRow row, ColumnMap columns)
    {
        string Cell(int index) => row.Cells.TryGetValue(index, out var value) ? Sanitise(value) : string.Empty;

        // A non-empty dose/frequency/duration cell wins over the same kind of quantity typed into
        // the name, even when that cell's own value is later omitted.
        var hasDoseCell = columns.Doses.Any(column => Cell(column.Index).Length > 0);
        var hasFrequencyCell = columns.Frequencies.Any(index => Cell(index).Length > 0);
        var hasDurationCell = columns.Durations.Any(index => Cell(index).Length > 0);

        string? name = null;
        foreach (var index in columns.Names)
        {
            var stripped = Cell(index);
            if (hasDoseCell)
            {
                stripped = DosePattern.Replace(stripped, " ");
            }

            if (hasFrequencyCell)
            {
                stripped = FrequencyPattern.Replace(stripped, " ");
            }

            if (hasDurationCell)
            {
                stripped = DurationPattern.Replace(stripped, " ");
            }

            // Spec: the candidate is the value after embedded-quantity removal *and* cleanup;
            // cleanup runs unconditionally, not only when removal changed the value.
            var candidate = CleanName(stripped);

            if (candidate.Any(char.IsLetter))
            {
                name = candidate;
                break;
            }
        }

        if (name is null)
        {
            return false;
        }

        var nameParts = MultiCompoundSeparatorPattern.Split(name);
        if (nameParts.Length > 1)
        {
            foreach (var part in nameParts.Select(part => part.Trim()).Where(part => part.Any(char.IsLetter)))
            {
                builder.AppendLine(part);
            }

            return true;
        }

        var line = new List<string>(4) { name };
        AddIfNotEmpty(line, BuildDose(columns, Cell));
        AddIfNotEmpty(line, BuildFrequency(columns, Cell));
        AddIfNotEmpty(line, BuildDuration(columns, Cell));
        builder.AppendLine(string.Join(' ', line));
        return true;
    }

    private static void AddIfNotEmpty(List<string> line, string value)
    {
        if (value.Length > 0)
        {
            line.Add(value);
        }
    }

    // Removes empty bracket pairs and leading/trailing separator-only tokens from a name
    // candidate ("Zorbatide ()" -> "Zorbatide", "Zorbatide -" -> "Zorbatide", "Zorbatide()" ->
    // "Zorbatide"), whether left behind by embedded-quantity removal or typed directly.
    private static string CleanName(string value)
    {
        string previous;
        do
        {
            previous = value;
            value = EmptyBracketPairPattern.Replace(value, " ");
        }
        while (!string.Equals(previous, value, StringComparison.Ordinal));

        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (tokens.Count > 0 && SeparatorOnlyTokenPattern.IsMatch(tokens[0]))
        {
            tokens.RemoveAt(0);
        }

        while (tokens.Count > 0 && SeparatorOnlyTokenPattern.IsMatch(tokens[^1]))
        {
            tokens.RemoveAt(tokens.Count - 1);
        }

        return string.Join(' ', tokens);
    }

    // The first non-empty dose cell (priority order) decides; at most one dose is emitted.
    private static string BuildDose(ColumnMap columns, Func<int, string> cell)
    {
        foreach (var (index, unitHint) in columns.Doses)
        {
            var value = cell(index);
            if (value.Length == 0)
            {
                continue;
            }

            // Ranges and digit-comma numbers are ambiguous: omit rather than guess a bound.
            if (RangePattern.IsMatch(value) || DigitCommaPattern.IsMatch(value))
            {
                return string.Empty;
            }

            var phrase = DosePattern.Match(value);
            if (phrase.Success)
            {
                return phrase.Value;
            }

            if (BareNumberPattern.IsMatch(value))
            {
                var unit = columns.Units.Select(cell).FirstOrDefault(candidate => candidate.Length > 0) ?? string.Empty;
                if (UnitOnlyPattern.IsMatch(unit))
                {
                    return $"{value} {unit}";
                }

                if (UnitOnlyPattern.IsMatch(unitHint))
                {
                    return $"{value} {unitHint}";
                }
            }

            return string.Empty;
        }

        return string.Empty;
    }

    private static string BuildFrequency(ColumnMap columns, Func<int, string> cell)
    {
        foreach (var index in columns.Frequencies)
        {
            var match = FrequencyPattern.Match(cell(index));
            if (match.Success)
            {
                return match.Value;
            }
        }

        return string.Empty;
    }

    private static string BuildDuration(ColumnMap columns, Func<int, string> cell)
    {
        foreach (var index in columns.Durations)
        {
            var value = cell(index);
            var match = DurationPattern.Match(value);
            if (match.Success && !RangePattern.IsMatch(value) && !CyclePattern.IsMatch(value))
            {
                return match.Value;
            }
        }

        return string.Empty;
    }
}

public sealed class ImageOcrProtocolExtractor : IProtocolTextExtractor
{
    private readonly IProtocolOcrService _ocrService;

    public ImageOcrProtocolExtractor(IProtocolOcrService ocrService)
    {
        _ocrService = ocrService;
    }

    public bool CanHandle(ProtocolIngestionRequest request)
    {
        if (request.InputType is not (ProtocolInputType.FileUpload or ProtocolInputType.CameraScan))
        {
            return false;
        }

        return ProtocolExtractorSupport.MatchesContentTypeOrExtension(
            request,
            [".jpg", ".jpeg", ".png", ".webp"],
            ["image/jpeg", "image/png", "image/webp"]);
    }

    public async Task<ProtocolExtractionResult> ExtractAsync(ProtocolIngestionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SourceBytes is null || request.SourceBytes.Length == 0)
        {
            throw new ProtocolIngestionException("The uploaded image was empty.");
        }

        var result = await _ocrService.ExtractAsync(request.SourceBytes, request.SourceName, cancellationToken);
        return new ProtocolExtractionResult(
            result.ExtractedText,
            result.Warnings,
            Array.Empty<ProtocolIngestionArtifact>(),
            result.LowConfidence);
    }
}

public sealed class LinkProtocolExtractor : IProtocolTextExtractor
{
    public const int MaxLinkResponseBytes = 12 * 1024 * 1024;
    private const int MaxRedirects = 3;

    private static readonly HashSet<string> UnsupportedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "docs.google.com",
        "drive.google.com",
        "notion.so",
        "www.notion.so",
        "dropbox.com",
        "www.dropbox.com"
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public LinkProtocolExtractor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public bool CanHandle(ProtocolIngestionRequest request) =>
        request.InputType == ProtocolInputType.Link;

    public async Task<ProtocolExtractionResult> ExtractAsync(ProtocolIngestionRequest request, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(request.LinkUrl, UriKind.Absolute, out var uri))
        {
            throw new ProtocolIngestionException("Enter a valid document URL.");
        }

        var client = _httpClientFactory.CreateClient("protocol-link-extractor");
        using var fetchTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        fetchTimeout.CancelAfter(TimeSpan.FromSeconds(30));
        var fetchToken = fetchTimeout.Token;
        try
        {
            var currentUri = uri;
            for (var redirectCount = 0; ; redirectCount++)
            {
                await ValidateDestinationAsync(currentUri, fetchToken);

                using var outboundRequest = new HttpRequestMessage(HttpMethod.Get, currentUri);
                using var response = await client.SendAsync(
                    outboundRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    fetchToken);

                if (IsRedirect(response.StatusCode))
                {
                    if (redirectCount >= MaxRedirects || response.Headers.Location is null)
                    {
                        throw new ProtocolIngestionException("That document link redirected too many times.");
                    }

                    currentUri = response.Headers.Location.IsAbsoluteUri
                        ? response.Headers.Location
                        : new Uri(currentUri, response.Headers.Location);
                    continue;
                }

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    throw new ProtocolIngestionException("That document requires authentication. Use a public share link or upload the file directly.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new ProtocolIngestionException("We could not fetch that shared document.");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType;
                var bytes = await ReadBoundedContentAsync(response.Content, fetchToken);
                var nestedRequest = new ProtocolIngestionRequest(
                    GuessInputType(contentType),
                    null,
                    currentUri.ToString(),
                    request.SourceName ?? Path.GetFileName(currentUri.LocalPath),
                    contentType,
                    bytes);

                if (string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase))
                {
                    return new ProtocolExtractionResult(
                        Encoding.UTF8.GetString(bytes),
                        Array.Empty<string>(),
                        Array.Empty<ProtocolIngestionArtifact>(),
                        false);
                }

                var extractor = CreateNestedExtractor(nestedRequest);
                if (extractor is null)
                {
                    throw new ProtocolIngestionException("That link points to a source we cannot extract yet. Upload the file directly for now.");
                }

                return await extractor.ExtractAsync(nestedRequest, cancellationToken);
            }
        }
        catch (ProtocolIngestionException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw new ProtocolIngestionException("We could not reach that URL. Check the link and try again.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProtocolIngestionException("That document took too long to download. Try uploading the file directly.");
        }
    }

    public static async ValueTask<Stream> ConnectToPublicEndpointAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        IPAddress[] addresses;
        try
        {
            addresses = await ResolvePublicAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        }
        catch (Exception ex) when (ex is SocketException or ProtocolIngestionException)
        {
            throw new HttpRequestException("The document host did not resolve to a public address.", ex);
        }

        Exception? lastError = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                socket.Dispose();
                lastError = ex;
                if (ex is OperationCanceledException)
                    throw;
            }
        }

        throw new HttpRequestException("The document host could not be reached at a validated public address.", lastError);
    }

    private static async Task ValidateDestinationAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new ProtocolIngestionException("Only secure HTTPS document links are supported.");

        if (!string.IsNullOrEmpty(uri.UserInfo) || uri.Port != 443)
            throw new ProtocolIngestionException("Document links must use public HTTPS on the standard port.");

        if (UnsupportedHosts.Contains(uri.Host))
            throw new ProtocolIngestionException("That shared document source is not supported yet. Try exporting the file and uploading it directly.");

        try
        {
            await ResolvePublicAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (SocketException)
        {
            throw new ProtocolIngestionException("We could not resolve that document host.");
        }
    }

    private static async Task<IPAddress[]> ResolvePublicAddressesAsync(string host, CancellationToken cancellationToken)
    {
        var normalizedHost = host.TrimEnd('.');
        if (string.Equals(normalizedHost, "localhost", StringComparison.OrdinalIgnoreCase) ||
            normalizedHost.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new ProtocolIngestionException("Only public internet document links are supported.");
        }

        var addresses = IPAddress.TryParse(normalizedHost, out var literal)
            ? new[] { literal! }
            : await Dns.GetHostAddressesAsync(normalizedHost, cancellationToken);

        if (addresses.Length == 0 || addresses.Any(address => !IsPublicUnicast(address)))
            throw new ProtocolIngestionException("Only public internet document links are supported.");

        return addresses;
    }

    private static bool IsPublicUnicast(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.None) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.IPv6None))
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] != 0 &&
                   bytes[0] != 10 &&
                   !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127) &&
                   bytes[0] != 127 &&
                   !(bytes[0] == 169 && bytes[1] == 254) &&
                   !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
                   !(bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0) &&
                   !(bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2) &&
                   !(bytes[0] == 192 && bytes[1] == 168) &&
                   !(bytes[0] == 198 && bytes[1] is 18 or 19) &&
                   !(bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) &&
                   !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113) &&
                   bytes[0] < 224;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return !address.IsIPv6LinkLocal &&
                   !address.IsIPv6Multicast &&
                   !address.IsIPv6SiteLocal &&
                   !bytes.Take(12).All(value => value == 0) &&
                   (bytes[0] & 0xfe) != 0xfc &&
                   !(bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xff && bytes[3] == 0x9b) &&
                   !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8);
        }

        return false;
    }

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.MovedPermanently or
            HttpStatusCode.Found or
            HttpStatusCode.SeeOther or
            HttpStatusCode.TemporaryRedirect or
            HttpStatusCode.PermanentRedirect;

    private static async Task<byte[]> ReadBoundedContentAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > MaxLinkResponseBytes)
            throw new ProtocolIngestionException("That document is too large. Upload a file smaller than 12 MB.");

        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        using var destination = new MemoryStream();
        var buffer = new byte[81920];

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            if (destination.Length + read > MaxLinkResponseBytes)
                throw new ProtocolIngestionException("That document is too large. Upload a file smaller than 12 MB.");

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return destination.ToArray();
    }

    private static ProtocolInputType GuessInputType(string? contentType)
    {
        return string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(contentType, "image/webp", StringComparison.OrdinalIgnoreCase)
            ? ProtocolInputType.CameraScan
            : ProtocolInputType.FileUpload;
    }

    private static IProtocolTextExtractor? CreateNestedExtractor(ProtocolIngestionRequest request)
    {
        return ProtocolExtractorSupport.MatchesContentTypeOrExtension(request, [".pdf"], ["application/pdf"])
            ? new PdfProtocolExtractor()
            : ProtocolExtractorSupport.MatchesContentTypeOrExtension(request, [".docx"], ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"])
                ? new DocxProtocolExtractor()
                : ProtocolExtractorSupport.MatchesContentTypeOrExtension(request, [".xlsx", ".csv"], ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "text/csv", "application/csv"])
                    ? new SpreadsheetProtocolExtractor()
                    : null;
    }
}

public sealed class AzureVisionProtocolOcrService : IProtocolOcrService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProtocolOcrOptions _options;
    private readonly IConsentGate _consentGate;

    public AzureVisionProtocolOcrService(
        IHttpClientFactory httpClientFactory,
        Microsoft.Extensions.Options.IOptions<ProtocolOcrOptions> options,
        IConsentGate consentGate)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _consentGate = consentGate;
    }

    public async Task<ProtocolOcrResult> ExtractAsync(byte[] imageBytes, string? sourceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint) || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ProtocolIngestionException("Image OCR is not configured yet for this environment. Upload text-based files or paste the protocol for now.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!await _consentGate.IsConsentGrantedAsync(cancellationToken))
        {
            throw new ProtocolIngestionException("Current consent is required before image bytes can be sent for OCR.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var client = _httpClientFactory.CreateClient("protocol-ocr");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.Endpoint.TrimEnd('/')}/computervision/imageanalysis:analyze?api-version=2024-02-01&features=read");
        request.Headers.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
        request.Content = new ByteArrayContent(imageBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new ProtocolIngestionException("We could not read text from that image yet. Try a clearer photo or upload the file instead.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var lines = payload.RootElement
                .GetProperty("readResult")
                .GetProperty("blocks")
                .EnumerateArray()
                .SelectMany(block => block.GetProperty("lines").EnumerateArray())
                .Select(line => line.GetProperty("text").GetString())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(text => text!.Trim())
                .ToList();

            var text = string.Join(Environment.NewLine, lines);
            var warnings = new List<string>();
            if (lines.Count < 3)
            {
                warnings.Add("This image produced limited OCR output. Review the parsed protocol carefully.");
            }

            return new ProtocolOcrResult(text, warnings, warnings.Count > 0);
        }
        catch (ProtocolIngestionException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw new ProtocolIngestionException("Could not reach the image analysis service. Try again or upload a text-based file instead.");
        }
        catch (JsonException)
        {
            throw new ProtocolIngestionException("The image analysis service returned an unexpected response. Try again or upload a text-based file instead.");
        }
        catch (KeyNotFoundException)
        {
            throw new ProtocolIngestionException("The image analysis service returned an unexpected response. Try again or upload a text-based file instead.");
        }
    }
}

public sealed class ProtocolOcrOptions
{
    public string? Endpoint { get; init; }
    public string? ApiKey { get; init; }
}

public sealed class ProtocolIngestionException : Exception
{
    public ProtocolIngestionException(string message) : base(message)
    {
    }
}

internal static class ProtocolExtractorSupport
{
    public static bool MatchesContentTypeOrExtension(ProtocolIngestionRequest request, IReadOnlyCollection<string> extensions, IReadOnlyCollection<string> contentTypes)
    {
        return HasExtension(request.SourceName, extensions.ToArray()) ||
               (!string.IsNullOrWhiteSpace(request.ContentType) && contentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase));
    }

    public static bool HasExtension(string? fileName, params string[] extensions)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        return extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public static string CreatePreview(string value)
    {
        var normalized = Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
        return normalized.Length <= 160 ? normalized : $"{normalized[..157]}...";
    }
}
