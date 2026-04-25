namespace BioStack.Application.Services;

using System.Security.Cryptography;
using System.Text;

public sealed class ProtocolFingerprintService : IProtocolFingerprintService
{
    public const string IngestionVersion = "v1";
    public const string OcrVersion = "v1";
    public const string ParserVersion = "v2";
    public const string KnowledgeVersion = "v1";
    public const string ScoringVersion = "v2";
    public const string CounterfactualVersion = "v2";

    public string GetIngestionFingerprint(ProtocolIngestionRequest request)
    {
        return request.InputType switch
        {
            BioStack.Contracts.Requests.ProtocolInputType.Paste => ComputeHash(NormalizeRawInput(request.InputText ?? string.Empty)),
            BioStack.Contracts.Requests.ProtocolInputType.Link => ComputeHash(NormalizeLink(request.LinkUrl ?? string.Empty)),
            _ => ComputeHash(request.SourceBytes is null ? string.Empty : Convert.ToHexString(SHA256.HashData(request.SourceBytes)))
        };
    }

    public string GetIngestionCacheKey(ProtocolIngestionRequest request, string ingestionFingerprint)
    {
        var mode = request.InputType.ToString().ToLowerInvariant();
        return $"analyzer:ingestion:{mode}:{IngestionVersion}:{ingestionFingerprint}";
    }

    public string GetRawInputHash(string input)
    {
        var normalized = NormalizeRawInput(input);
        return ComputeHash(normalized);
    }

    public string GetNormalizedTextHash(string normalizedText)
    {
        return ComputeHash(NormalizeRawInput(normalizedText));
    }

    public string GetNormalizedProtocolHash(NormalizedProtocol protocol)
    {
        var canonical = string.Join("|", protocol.Compounds
            .OrderBy(compound => compound.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(compound => compound.DoseMcg)
            .ThenBy(compound => compound.Frequency, StringComparer.OrdinalIgnoreCase)
            .ThenBy(compound => compound.Duration, StringComparer.OrdinalIgnoreCase)
            .Select(compound => $"{compound.CanonicalName.ToLowerInvariant()}:{compound.DoseMcg:0.####}:{compound.Frequency.ToLowerInvariant()}:{compound.Duration.ToLowerInvariant()}"));

        return ComputeHash(canonical);
    }

    public string GetAnalysisKey(NormalizedProtocol protocol, AnalysisContext context)
    {
        var protocolHash = GetNormalizedProtocolHash(protocol);
        var contextKey = string.Join(":",
            NormalizeToken(context.Goal),
            NormalizeToken(context.Sex),
            NormalizeToken(context.AgeBand),
            NormalizeToken(context.WeightBand),
            string.Join(",", context.ExistingStackContext.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).Select(NormalizeToken)));

        return $"analyzer:analysis:parser-{context.ParserVersion}:knowledge-{context.KnowledgeVersion}:score-{context.ScoringVersion}:{contextKey}:{protocolHash}";
    }

    public string GetCounterfactualKey(NormalizedProtocol protocol, OptimizationContext context)
    {
        var protocolHash = GetNormalizedProtocolHash(protocol);
        var constraints = string.Join(":",
            NormalizeToken(context.Goal),
            context.MaxCompounds,
            NormalizeToken(context.OptimizationMode),
            string.Join(",", context.RequiredCompoundIds.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).Select(NormalizeToken)),
            string.Join(",", context.ExcludedCompoundIds.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).Select(NormalizeToken)),
            string.Join(",", context.ExistingProfileContext.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).Select(NormalizeToken)),
            context.ScoreFloor,
            context.BeamWidth);

        return $"analyzer:counterfactual:knowledge-{context.KnowledgeVersion}:score-{context.ScoringVersion}:cf-{context.CounterfactualVersion}:{constraints}:{protocolHash}";
    }

    public static string NormalizeRawInput(string input)
    {
        var normalized = (input ?? string.Empty).Trim().ToLowerInvariant();
        normalized = normalized
            .Replace("μg", "mcg", StringComparison.OrdinalIgnoreCase)
            .Replace("ug", "mcg", StringComparison.OrdinalIgnoreCase)
            .Replace("micrograms", "mcg", StringComparison.OrdinalIgnoreCase)
            .Replace("microgram", "mcg", StringComparison.OrdinalIgnoreCase)
            .Replace("every day", "daily", StringComparison.OrdinalIgnoreCase);
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^\w\+\-\.\/\s]", " ");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");
        return normalized.Trim();
    }

    private static string NormalizeToken(string value)
    {
        return System.Text.RegularExpressions.Regex.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), @"\s+", "-");
    }

    private static string NormalizeLink(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            return normalized.ToLowerInvariant();
        }

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/').ToLowerInvariant();
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public interface IProtocolFingerprintService
{
    string GetIngestionFingerprint(ProtocolIngestionRequest request);
    string GetIngestionCacheKey(ProtocolIngestionRequest request, string ingestionFingerprint);
    string GetRawInputHash(string input);
    string GetNormalizedTextHash(string normalizedText);
    string GetNormalizedProtocolHash(NormalizedProtocol protocol);
    string GetAnalysisKey(NormalizedProtocol protocol, AnalysisContext context);
    string GetCounterfactualKey(NormalizedProtocol protocol, OptimizationContext context);
}
