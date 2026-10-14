namespace BioStack.Api.Tests.Security;

using System.Net;
using BioStack.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

public sealed class ClientIpResolverTests
{
    [Fact]
    public void PartitionKey_IgnoresForwardedHeaderFromUntrustedPeer()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.8");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.10";

        var key = ClientIpResolver.PartitionKey(context, Config("10.0.0.4"));

        Assert.Equal("203.0.113.8", key);
    }

    [Fact]
    public void PartitionKey_UsesForwardedClientWhenPeerIsTrusted()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.4");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.10, 10.0.0.4";

        var key = ClientIpResolver.PartitionKey(context, Config("10.0.0.4"));

        Assert.Equal("198.51.100.10", key);
    }

    [Fact]
    public void PartitionKey_UsesRightmostUntrustedForwardedAddress()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.4");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.9, 198.51.100.20";

        var key = ClientIpResolver.PartitionKey(context, Config("10.0.0.4"));

        Assert.Equal("198.51.100.20", key);
    }

    [Fact]
    public void PartitionKey_UsesSocketWhenForwardedHopsAreAllTrusted()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.4");
        context.Request.Headers["X-Forwarded-For"] = "10.0.0.4, 127.0.0.1";

        var key = ClientIpResolver.PartitionKey(context, Config("10.0.0.4"));

        Assert.Equal("10.0.0.4", key);
    }

    [Theory]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.4")]
    [InlineData("192.168.1.9")]
    [InlineData("100.64.0.4")]
    public void PartitionKey_IgnoresForwardedHeaderWhenPrivateOrCgnatPeerIsNotConfigured(string peer)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.22";

        var key = ClientIpResolver.PartitionKey(context, new ConfigurationBuilder().Build());

        Assert.Equal(peer, key);
    }

    [Fact]
    public void AccountKey_NormalizesEmail()
    {
        Assert.Equal("user@example.com", ClientIpResolver.AccountKey("  User@Example.com "));
        Assert.Null(ClientIpResolver.AccountKey(" "));
    }

    private static IConfiguration Config(string trustedProxy)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:TrustedProxies:0"] = trustedProxy,
            })
            .Build();
}
