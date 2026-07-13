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
    public bool CanHandle(ProtocolIngestionRequest request) =>
        request.InputType == ProtocolInputType.FileUpload &&
        ProtocolExtractorSupport.MatchesContentTypeOrExtension(request, [".pdf"], ["application/pdf"]);

    public Task<ProtocolExtractionResult> ExtractAsync(ProtocolIngestionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SourceBytes is null || request.SourceBytes.Length == 0)
        {
            throw new ProtocolIngestionException("The uploaded PDF was empty.");
        }

        var warnings = new List<string>();
        var artifacts = new List<ProtocolIngestionArtifact>();
        var decoded = Encoding.GetEncoding("ISO-8859-1").GetString(request.SourceBytes);
        var pageMatches = Regex.Matches(decoded, @"/Type\s*/Page\b", RegexOptions.IgnoreCase);
        var extractedLines = ExtractPdfText(decoded).ToList();
        foreach (var (line, index) in extractedLines.Select((line, index) => (line, index)))
        {
            var preview = ProtocolExtractorSupport.CreatePreview(line);
            if (!string.IsNullOrWhiteSpace(preview))
            {
                artifacts.Add(new ProtocolIngestionArtifact("page", $"Block {index + 1}", preview));
            }
        }

        var text = string.Join(Environment.NewLine, extractedLines).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ProtocolIngestionException("This PDF did not expose readable text. Try a clearer source file or a direct image scan.");
        }

        if (pageMatches.Count > 0 && text.Length / pageMatches.Count < 120)
        {
            warnings.Add("This PDF appears to be image-heavy or low text density. Review the extracted protocol carefully.");
        }

        return Task.FromResult(new ProtocolExtractionResult(
            text,
            warnings,
            artifacts,
            warnings.Count > 0));
    }

    private static IEnumerable<string> ExtractPdfText(string content)
    {
        var directText = Regex.Matches(content, @"\((?<text>(?:\\\)|\\\(|\\\\|[^\)])+)\)\s*Tj", RegexOptions.Singleline)
            .Select(match => DecodePdfString(match.Groups["text"].Value));

        var arrayText = Regex.Matches(content, @"\[(?<items>.*?)\]\s*TJ", RegexOptions.Singleline)
            .Select(match => string.Join(" ", Regex.Matches(match.Groups["items"].Value, @"\((?<text>(?:\\\)|\\\(|\\\\|[^\)])+)\)")
                .Select(inner => DecodePdfString(inner.Groups["text"].Value))));

        return directText
            .Concat(arrayText)
            .Select(value => Regex.Replace(value, @"\s+", " ").Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value));
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
            return Task.FromResult(new ProtocolExtractionResult(
                ConvertDelimitedRowsToText("CSV", ReadDelimitedRows(csvText)),
                Array.Empty<string>(),
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
            var sheetMap = relationships.Root?
                .Elements(Relationship + "Relationship")
                .Where(element => element.Attribute("Id") is not null && element.Attribute("Target") is not null)
                .ToDictionary(
                    element => element.Attribute("Id")!.Value,
                    element => $"xl/{element.Attribute("Target")!.Value.Replace("\\", "/").TrimStart('/')}",
                    StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var blocks = new List<string>();
            foreach (var sheet in workbook.Root?.Descendants(Spreadsheet + "sheet") ?? Enumerable.Empty<XElement>())
            {
                var sheetName = sheet.Attribute("name")?.Value ?? "Sheet";
                var relationId = sheet.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "id")?.Value;
                if (string.IsNullOrWhiteSpace(relationId) || !sheetMap.TryGetValue(relationId, out var target))
                {
                    continue;
                }

                var worksheet = LoadXml(archive, target);
                var rows = worksheet.Root?
                    .Descendants(Spreadsheet + "row")
                    .Select(row => row.Elements(Spreadsheet + "c").Select(cell => ReadCellValue(cell, sharedStrings)).ToList())
                    .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
                    .ToList()
                    ?? new List<List<string>>();

                if (rows.Count > 0)
                {
                    blocks.Add(ConvertDelimitedRowsToText(sheetName, rows));
                }
            }

            return Task.FromResult(new ProtocolExtractionResult(
                string.Join(Environment.NewLine + Environment.NewLine, blocks),
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
            ?? throw new ProtocolIngestionException($"The spreadsheet is missing {path}.");

        using var entryStream = entry.Open();
        return XDocument.Load(entryStream);
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
        var raw = cell.Element(Spreadsheet + "v")?.Value?.Trim() ?? string.Empty;
        var type = cell.Attribute("t")?.Value;
        if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(raw, out var sharedIndex) &&
            sharedIndex >= 0 &&
            sharedIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedIndex];
        }

        return raw;
    }

    private static List<List<string>> ReadDelimitedRows(string csvText)
    {
        return csvText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(',').Select(value => value.Trim().Trim('"')).ToList())
            .ToList();
    }

    private static string ConvertDelimitedRowsToText(string sheetName, IReadOnlyList<List<string>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Sheet: {sheetName}");

        var header = rows.FirstOrDefault() ?? [];
        var hasHeader = header.Count > 0 && header.Any(value => Regex.IsMatch(value, "[A-Za-z]"));

        foreach (var row in rows.Skip(hasHeader ? 1 : 0))
        {
            if (row.Count == 0)
            {
                continue;
            }

            if (hasHeader && header.Count >= row.Count)
            {
                builder.AppendLine(string.Join(" | ",
                    row.Select((value, index) => $"{header[index]}: {value}".Trim())));
            }
            else
            {
                builder.AppendLine(string.Join(" | ", row));
            }
        }

        return builder.ToString().Trim();
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

    public AzureVisionProtocolOcrService(IHttpClientFactory httpClientFactory, Microsoft.Extensions.Options.IOptions<ProtocolOcrOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<ProtocolOcrResult> ExtractAsync(byte[] imageBytes, string? sourceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint) || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ProtocolIngestionException("Image OCR is not configured yet for this environment. Upload text-based files or paste the protocol for now.");
        }

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
