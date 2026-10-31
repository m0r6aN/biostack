namespace BioStack.Api.Tests.Auth;

using BioStack.Api.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class ContactRecipientTests
{
    private static string Resolve(string? configured)
    {
        var values = new Dictionary<string, string?>();
        if (configured is not null)
        {
            values[AzureCommunicationEmailContactMessageSender.SupportInboxConfigKey] = configured;
        }

        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return AzureCommunicationEmailContactMessageSender.ResolveRecipient(config, NullLogger.Instance);
    }

    [Fact]
    public void Unset_UsesTheDefaultInbox() =>
        Assert.Equal(AzureCommunicationEmailContactMessageSender.DefaultSupportInboxAddress, Resolve(null));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_UsesTheDefaultInbox(string configured) =>
        Assert.Equal(AzureCommunicationEmailContactMessageSender.DefaultSupportInboxAddress, Resolve(configured));

    [Fact]
    public void ConfiguredAddress_IsUsedAndTrimmed() =>
        Assert.Equal("help@example.org", Resolve("  help@example.org "));

    [Theory]
    [InlineData("not-an-address")]
    [InlineData("a@b@example.org")]
    [InlineData("two words@example.org")]
    [InlineData("a@nodot")]
    public void MalformedAddress_FallsBackToTheDefault(string configured) =>
        Assert.Equal(AzureCommunicationEmailContactMessageSender.DefaultSupportInboxAddress, Resolve(configured));
}
