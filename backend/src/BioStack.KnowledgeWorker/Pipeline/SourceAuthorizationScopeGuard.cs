namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

public static class SourceAuthorizationScopeGuard
{
    // Byte equality alone cannot establish that registry activation matches the selected decisions.
    public static void RequireMatchingActiveSources(JsonNode decisions, JsonNode registry)
    {
        var approved = decisions["sources"]!.AsArray()
            .Where(source => (string?)source?["decisionStatus"] == "approved"
                && (bool?)source?["activationReady"] == true)
            .Select(source => (string)source!["sourceId"]!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var active = registry["sources"]!.AsArray()
            .Where(source => (string?)source?["rights"]?["reviewStatus"] == "approved"
                && (string?)source?["operations"]?["status"] == "active"
                && (bool?)source?["acquisition"]?["enabled"] == true)
            .Select(source => (string)source!["identity"]!["sourceId"]!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!approved.SetEquals(active))
        {
            throw new InvalidOperationException(
                "Source authorization decisions and active registry source sets do not match.");
        }
    }
}
