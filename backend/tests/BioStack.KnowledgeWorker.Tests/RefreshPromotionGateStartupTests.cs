namespace BioStack.KnowledgeWorker.Tests;

using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>
/// Coverage for the <c>--Worker:AllowUnpromoted=true</c> override's safety check: it must
/// be refused against anything but a local database unless
/// <c>Worker:AcknowledgeUnpromotedProduction=true</c> is also explicitly set, and it must
/// never engage the review-decision loader at all once refused/approved (the whole point
/// of the override is to skip that gate).
/// </summary>
public class RefreshPromotionGateStartupTests
{
    [Fact]
    public void AllowUnpromoted_Is_Refused_Against_A_NonLocal_Host_Without_Acknowledgement()
    {
        var options = new WorkerOptions
        {
            AllowUnpromoted = true,
            AcknowledgeUnpromotedProduction = false,
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            RefreshPromotionGateStartup.ValidateAndLoad(
                options, "Host=prod-db.internal;Database=biostack;Username=app;Password=x"));

        Assert.Contains("AllowUnpromoted", ex.Message);
        Assert.Contains("localhost", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    public void AllowUnpromoted_Is_Accepted_Against_A_Local_Host_Without_Acknowledgement(string host)
    {
        var options = new WorkerOptions { AllowUnpromoted = true, AcknowledgeUnpromotedProduction = false };

        var gate = RefreshPromotionGateStartup.ValidateAndLoad(
            options, $"Host={host};Database=biostack;Username=app;Password=x");

        Assert.False(gate.IsEnforced);
        Assert.True(gate.Evaluate("Anything").Allowed);
    }

    [Fact]
    public void AllowUnpromoted_Is_Accepted_Against_A_NonLocal_Host_When_Acknowledged()
    {
        var options = new WorkerOptions
        {
            AllowUnpromoted = true,
            AcknowledgeUnpromotedProduction = true,
        };

        var gate = RefreshPromotionGateStartup.ValidateAndLoad(
            options, "Host=prod-db.internal;Database=biostack;Username=app;Password=x");

        Assert.False(gate.IsEnforced);
    }

    // NOTE: the non-override ("real gate") path of ValidateAndLoad resolves its schema
    // directory from AppContext.BaseDirectory + "Schemas", matching Program.cs's existing
    // IResearchArtifactValidator registration exactly. That directory is only populated in
    // the *worker's own* published/build output, not this test project's (which mirrors
    // only Fixtures/ — see TestPaths.cs), so that branch is covered end-to-end via
    // PromotionGateLoaderTests instead, using TestPaths.WorkerSchemaDirectory() as the
    // schema source. Exercising it here would require duplicating that directory-discovery
    // machinery rather than testing new behavior.
}
