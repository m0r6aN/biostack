namespace BioStack.Api.Security;

using System.Net;

public static class ClientIpResolver
{
    public const string ForwardedForHeader = "X-Forwarded-For";

    public static string PartitionKey(HttpContext httpContext, IConfiguration configuration)
    {
        var remote = httpContext.Connection.RemoteIpAddress;
        if (remote is null)
        {
            return "unknown";
        }

        // Loopback and RateLimit:TrustedProxies only. Private and CGNAT ranges are not trusted.
        var trusted = TrustedProxyAddresses(configuration);
        if (IsTrustedProxy(remote, trusted) &&
            httpContext.Request.Headers.TryGetValue(ForwardedForHeader, out var forwarded))
        {
            var client = RightmostUntrustedForwardedAddress(forwarded.ToString(), trusted);
            if (client is not null)
            {
                return Normalize(client);
            }
        }

        return Normalize(remote);
    }

    public static string? AccountKey(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim().ToLowerInvariant();
        return normalized.Length is 0 or > 254 ? null : normalized;
    }

    private static bool IsTrustedProxy(IPAddress remote, HashSet<string> trusted)
    {
        if (IPAddress.IsLoopback(remote))
        {
            return true;
        }

        return trusted.Contains(Normalize(remote));
    }

    private static HashSet<string> TrustedProxyAddresses(IConfiguration configuration)
    {
        var configured = configuration.GetSection("RateLimit:TrustedProxies").Get<string[]>() ?? [];
        var parsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in configured)
        {
            if (IPAddress.TryParse(value, out var address))
            {
                parsed.Add(Normalize(address));
            }
        }

        return parsed;
    }

    private static IPAddress? RightmostUntrustedForwardedAddress(string header, HashSet<string> trusted)
    {
        var hops = header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var index = hops.Length - 1; index >= 0; index--)
        {
            if (!IPAddress.TryParse(hops[index], out var address))
            {
                return null;
            }

            if (!IsTrustedProxy(address, trusted))
            {
                return address;
            }
        }

        return null;
    }

    private static string Normalize(IPAddress address)
        => address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
}
