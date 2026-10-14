namespace BioStack.Api.Security;

using System.Net;

public static class ClientIpResolver
{
    public const string ForwardedForHeader = "X-Forwarded-For";

    public static string PartitionKey(HttpContext httpContext, IConfiguration configuration)
    {
        var remote = httpContext.Connection.RemoteIpAddress;
        if (remote is not null && IsTrustedProxy(remote, configuration) &&
            httpContext.Request.Headers.TryGetValue(ForwardedForHeader, out var forwarded))
        {
            var client = FirstForwardedAddress(forwarded.ToString());
            if (client is not null)
            {
                return Normalize(client);
            }
        }

        return remote is null ? "unknown" : Normalize(remote);
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

    private static bool IsTrustedProxy(IPAddress remote, IConfiguration configuration)
    {
        var normalized = Normalize(remote);
        if (TrustedProxyAddresses(configuration).Contains(normalized))
        {
            return true;
        }

        if (IPAddress.IsLoopback(remote))
        {
            return true;
        }

        if (remote.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork &&
            !remote.IsIPv4MappedToIPv6)
        {
            return false;
        }

        var bytes = remote.MapToIPv4().GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127);
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

    private static IPAddress? FirstForwardedAddress(string header)
    {
        var first = header.Split(',', 2, StringSplitOptions.TrimEntries)[0];
        return IPAddress.TryParse(first, out var address) ? address : null;
    }

    private static string Normalize(IPAddress address)
        => address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
}
