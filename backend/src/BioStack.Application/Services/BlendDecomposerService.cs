namespace BioStack.Application.Services;

using System.Text.RegularExpressions;

public sealed class BlendDecomposerService : IBlendDecomposerService
{
    private static readonly Regex ParentheticalComponentsPattern = new(@"\((?<components>[^)]+)\)", RegexOptions.Compiled);
    private static readonly Regex BlendNamePattern = new(@"^(?<name>.+?\bblend)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, string[]> KnownBlends =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Triple Threat Blend"] = ["NAD+", "MOTS-c", "5-Amino-1MQ"],
            ["GLOW Blend"] = ["GHK-cu", "BPC-157", "TB-500"],
            ["KLOW Blend"] = ["GHK-cu", "BPC-157", "TB-500", "KPV"],
            ["Tesamorelin/Ipamorelin Blend"] = ["Tesamorelin", "Ipamorelin"],
        };

    public BlendDecompositionResult Decompose(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || !segment.Contains("blend", StringComparison.OrdinalIgnoreCase))
        {
            return BlendDecompositionResult.None;
        }

        var blendName = ExtractBlendName(segment);
        var components = TryGetComponentsFromParentheses(segment)
            ?? TryGetComponentsFromKnownBlends(blendName)
            ?? TryGetSlashSeparatedComponents(blendName);

        return components is null || components.Count == 0
            ? BlendDecompositionResult.None
            : new BlendDecompositionResult(blendName, components);
    }

    private static string ExtractBlendName(string segment)
    {
        var match = BlendNamePattern.Match(segment.Trim());
        return match.Success ? match.Groups["name"].Value.Trim() : segment.Trim();
    }

    private static List<string>? TryGetComponentsFromParentheses(string segment)
    {
        var match = ParentheticalComponentsPattern.Match(segment);
        if (!match.Success)
        {
            return null;
        }

        return match.Groups["components"].Value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(component => component.Trim())
            .Where(component => component.Length > 0)
            .ToList();
    }

    private static List<string>? TryGetComponentsFromKnownBlends(string blendName)
    {
        return KnownBlends.TryGetValue(blendName, out var components)
            ? components.ToList()
            : null;
    }

    private static List<string>? TryGetSlashSeparatedComponents(string blendName)
    {
        var blendIndex = blendName.IndexOf("Blend", StringComparison.OrdinalIgnoreCase);
        if (blendIndex <= 0)
        {
            return null;
        }

        var prefix = blendName[..blendIndex].Trim();
        if (!prefix.Contains('/'))
        {
            return null;
        }

        var components = prefix
            .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        return components.Count > 1 ? components : null;
    }
}

public sealed record BlendDecompositionResult(string BlendName, List<string> Components)
{
    public static BlendDecompositionResult None { get; } = new(string.Empty, new List<string>());
}

public interface IBlendDecomposerService
{
    BlendDecompositionResult Decompose(string segment);
}
