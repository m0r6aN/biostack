namespace BioStack.Api.Security;

using System.Collections.Concurrent;

public sealed class AuthStartAccountBudget
{
    public const int PermitLimit = 3;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, WindowCount> _windows = new(StringComparer.Ordinal);

    public bool TryConsume(string accountKey, DateTime utcNow)
    {
        var window = _windows.AddOrUpdate(
            accountKey,
            _ => new WindowCount(utcNow, 1),
            (_, current) => current.StartedAtUtc <= utcNow - Window
                ? new WindowCount(utcNow, 1)
                : current with { Count = current.Count + 1 });

        return window.Count <= PermitLimit;
    }

    private sealed record WindowCount(DateTime StartedAtUtc, int Count);
}
