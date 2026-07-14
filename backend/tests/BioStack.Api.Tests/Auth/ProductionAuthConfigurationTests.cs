namespace BioStack.Api.Tests.Auth;

using BioStack.Api.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

public sealed class ProductionAuthConfigurationTests
{
    [Fact]
    public void Validate_AllowsDevelopmentWithoutProductionSettings()
        => ProductionAuthConfiguration.Validate(new ConfigurationBuilder().Build(), isProduction: false);

    [Fact]
    public void Validate_AcceptsHttpsFrontendCorsAndSingleAzureEmailProvider()
    {
        var configuration = Build(new Dictionary<string, string?>
        {
            ["FrontendUrl"] = "https://app.biostack.example",
            ["Cors:AllowedOrigins:0"] = "https://app.biostack.example",
            ["AzureCommunicationEmail:ConnectionString"] = "configured-connection-string",
            ["AzureCommunicationEmail:SenderAddress"] = "no-reply@biostack.example",
        });

        ProductionAuthConfiguration.Validate(configuration, isProduction: true);
    }

    [Theory]
    [InlineData("http://app.biostack.example", "https://app.biostack.example")]
    [InlineData("https://app.biostack.example/path", "https://app.biostack.example")]
    [InlineData("https://app.biostack.example", "https://other.biostack.example")]
    public void Validate_RejectsUnsafeOrMismatchedFrontendOrigin(string frontendUrl, string allowedOrigin)
    {
        var configuration = Build(new Dictionary<string, string?>
        {
            ["FrontendUrl"] = frontendUrl,
            ["Cors:AllowedOrigins:0"] = allowedOrigin,
            ["AzureCommunicationEmail:ConnectionString"] = "configured",
            ["AzureCommunicationEmail:SenderAddress"] = "no-reply@biostack.example",
        });

        Assert.Throws<InvalidOperationException>(
            () => ProductionAuthConfiguration.Validate(configuration, isProduction: true));
    }

    [Fact]
    public void Validate_RequiresExactlyOneCompleteEmailProvider()
    {
        var values = new Dictionary<string, string?>
        {
            ["FrontendUrl"] = "https://app.biostack.example",
            ["Cors:AllowedOrigins:0"] = "https://app.biostack.example",
        };

        Assert.Throws<InvalidOperationException>(
            () => ProductionAuthConfiguration.Validate(Build(values), isProduction: true));

        values["AzureCommunicationEmail:ConnectionString"] = "configured";
        values["AzureCommunicationEmail:SenderAddress"] = "no-reply@biostack.example";
        values["Smtp:Host"] = "smtp.biostack.example";
        values["Smtp:FromEmail"] = "no-reply@biostack.example";
        Assert.Throws<InvalidOperationException>(
            () => ProductionAuthConfiguration.Validate(Build(values), isProduction: true));
    }

    private static IConfiguration Build(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
