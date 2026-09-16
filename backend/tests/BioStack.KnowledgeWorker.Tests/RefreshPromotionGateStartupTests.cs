namespace BioStack.KnowledgeWorker.Tests;

using BioStack.KnowledgeWorker.Config;
using Microsoft.Extensions.Configuration;
using Xunit;

public class RefreshPromotionGateStartupTests
{
    [Theory]
    [InlineData("Production", "localhost")]
    [InlineData("Staging", "127.0.0.1")]
    [InlineData("", "localhost")]
    [InlineData("Development", "prod-db.internal")]
    [InlineData("Development", "localhost,prod-db.internal")]
    public void Override_Refuses_Anything_Outside_Local_Development(string environment, string host)
    {
        Assert.Throws<InvalidOperationException>(() => RefreshPromotionGateStartup.ValidateAndLoad(
            new WorkerOptions { AllowUnpromoted = true }, $"Host={host};Database=test", environment));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void Override_Allows_Local_Development_Only(string host)
    {
        var gate = RefreshPromotionGateStartup.ValidateAndLoad(
            new WorkerOptions { AllowUnpromoted = true }, $"Host={host};Database=test", "Development");
        Assert.False(gate.IsEnforced);
        Assert.True(gate.Evaluate("Anything").Allowed);
    }

    [Fact]
    public void Local_Host_Alone_Does_Not_Authorize_Default_Production_Override()
    {
        Assert.Throws<InvalidOperationException>(() => RefreshPromotionGateStartup.ValidateAndLoad(
            new WorkerOptions { AllowUnpromoted = true }, "Host=localhost;Database=test"));
    }

    [Fact]
    public void Removed_Acknowledgement_Configuration_Does_Not_Bypass_Remote_Guard()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Worker:AllowUnpromoted"] = "true",
            ["Worker:AcknowledgeUnpromotedProduction"] = "true"
        }).Build();
        var options = new WorkerOptions();
        config.GetSection("Worker").Bind(options);
        Assert.Throws<InvalidOperationException>(() => RefreshPromotionGateStartup.ValidateAndLoad(
            options, "Host=prod-db.internal;Database=test", "Development"));
    }
}
