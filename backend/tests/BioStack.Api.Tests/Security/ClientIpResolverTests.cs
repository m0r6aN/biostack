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
    public void PartitionKey_UsesForwardedClientWhenPeerIsPlatformPrivate()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("100.64.0.4");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.22";

        var key = ClientIpResolver.PartitionKey(context, new ConfigurationBuilder().Build());

        Assert.Equal("198.51.100.22", key);
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
