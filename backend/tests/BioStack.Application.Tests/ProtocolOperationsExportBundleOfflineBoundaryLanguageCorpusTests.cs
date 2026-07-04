namespace BioStack.Application.Tests;

using System.Text.Json;
using BioStack.Tests.Shared;
using Xunit;

/// <summary>
/// Applies the shared offline-verification boundary-language corpus to the human-readable posture
/// surfaces owned in this repository: the CLI docs drilldown (README), the release checklist, and the
/// export bundle disclaimer. Posture prose must affirm the required inspection-only, non-authority
/// concepts. This test adds no production logic and no user-facing claim expansion.
/// </summary>
public sealed class ProtocolOperationsExportBundleOfflineBoundaryLanguageCorpusTests
{
    [Fact]
    public void DocsDrilldown_AffirmsAllRequiredPostureConcepts()
    {
        var text = File.ReadAllText(RepoPath(
            "backend", "tools", "BioStack.ProtocolOperationsExportBundleVerifierCli", "README.md"));

        var missing = OfflineVerificationBoundaryLanguageCorpus.FindMissingRequiredConcepts(
            text, OfflineVerificationBoundaryLanguageCorpus.DocsDrilldownRequiredConcepts);

        Assert.Empty(missing);
    }

    [Fact]
    public void ReleaseChecklist_AffirmsAllRequiredPostureConcepts()
    {
        var text = File.ReadAllText(RepoPath(
            "backend", "tools", "BioStack.ProtocolOperationsExportBundleVerifierCli",
            "OFFLINE_VERIFICATION_RELEASE_CHECKLIST.md"));

        var missing = OfflineVerificationBoundaryLanguageCorpus.FindMissingRequiredConcepts(
            text, OfflineVerificationBoundaryLanguageCorpus.ReleaseChecklistRequiredConcepts);

        Assert.Empty(missing);
    }

    [Fact]
    public void BundleSurfaceDisclaimer_AffirmsRequiredPostureConcepts()
    {
        using var bundle = JsonDocument.Parse(File.ReadAllText(BundleFixturePath()));
        var disclaimer = bundle.RootElement.GetProperty("Disclaimer").GetString() ?? string.Empty;

        var missing = OfflineVerificationBoundaryLanguageCorpus.FindMissingRequiredConcepts(
            disclaimer, OfflineVerificationBoundaryLanguageCorpus.BundleDisclaimerRequiredConcepts);

        Assert.Empty(missing);
    }

    [Fact]
    public void Corpus_DefinesForbiddenAuthorityAndRequiredPostureConcepts()
    {
        // The corpus is the single owned definition; assert it stays populated so a future edit that
        // empties it fails loudly rather than silently disabling every downstream guard.
        Assert.NotEmpty(OfflineVerificationBoundaryLanguageCorpus.ForbiddenAuthorityConcepts);
        Assert.NotEmpty(OfflineVerificationBoundaryLanguageCorpus.DocsDrilldownRequiredConcepts);
        Assert.NotEmpty(OfflineVerificationBoundaryLanguageCorpus.ReleaseChecklistRequiredConcepts);
        Assert.NotEmpty(OfflineVerificationBoundaryLanguageCorpus.BundleDisclaimerRequiredConcepts);

        foreach (var forbiddenLabel in new[] { "dosing recommendation", "diagnosis", "treatment", "prescription" })
        {
            Assert.Contains(
                OfflineVerificationBoundaryLanguageCorpus.ForbiddenAuthorityConcepts,
                concept => concept.Label == forbiddenLabel);
        }
    }

    private static string BundleFixturePath()
    {
        return RepoPath(
            "backend", "tests", "BioStack.Application.Tests", "Fixtures",
            "ProtocolOperationsExportBundle", "ProtocolOperationsExportBundle.golden.json");
    }

    private static string RepoPath(params string[] segments)
    {
        return Path.Combine(new[] { OfflineVerificationBoundaryLanguageCorpus.RepositoryRoot() }.Concat(segments).ToArray());
    }
}
