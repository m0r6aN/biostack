namespace BioStack.Api.Tests.Auth;

using Azure.Communication.Email;
using BioStack.Api.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

public sealed class OutgoingEmailReplyToTests
{
    private static IConfiguration Config(string? value)
    {
        var values = new Dictionary<string, string?>();
        if (value is not null)
        {
            values[OutgoingEmailReplyTo.ConfigKey] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static EmailMessage NewMessage() =>
        new("noreply@example.org", "person@example.net", new EmailContent("Subject") { PlainText = "Body" });

    [Fact]
    public void Unset_AddsNoReplyTo()
    {
        var message = NewMessage();
        OutgoingEmailReplyTo.Apply(message, Config(null));
        Assert.Empty(message.ReplyTo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not-an-address")]
    [InlineData("a@b@example.org")]
    [InlineData("two words@example.org")]
    public void BlankOrMalformed_AddsNoReplyTo(string value)
    {
        var message = NewMessage();
        OutgoingEmailReplyTo.Apply(message, Config(value));
        Assert.Empty(message.ReplyTo);
    }

    [Fact]
    public void ConfiguredAddress_IsAddedOnceAndTrimmed()
    {
        var message = NewMessage();
        OutgoingEmailReplyTo.Apply(message, Config("  help@example.org "));
        var reply = Assert.Single(message.ReplyTo);
        Assert.Equal("help@example.org", reply.Address);
    }
}
