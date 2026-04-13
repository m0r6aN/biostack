namespace BioStack.Api.Auth;

using System.Collections.Concurrent;

public sealed record DevMagicLinkMessage(
    string Contact,
    string Link,
    string RedirectPath,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc
);

public interface IMagicLinkDelivery
{
    Task SendAsync(string contact, string magicLink, string redirectPath, DateTime expiresAtUtc, CancellationToken ct);
}

public interface IDevMagicLinkInbox
{
    IReadOnlyCollection<DevMagicLinkMessage> Latest();
}

public sealed class InMemoryMagicLinkDelivery : IMagicLinkDelivery, IDevMagicLinkInbox
{
    private const int MaxMessages = 25;
    private readonly ConcurrentQueue<DevMagicLinkMessage> _messages = new();
    private readonly ILogger<InMemoryMagicLinkDelivery> _logger;

    public InMemoryMagicLinkDelivery(ILogger<InMemoryMagicLinkDelivery> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string contact, string magicLink, string redirectPath, DateTime expiresAtUtc, CancellationToken ct)
    {
        _messages.Enqueue(new DevMagicLinkMessage(contact, magicLink, redirectPath, DateTime.UtcNow, expiresAtUtc));

        while (_messages.Count > MaxMessages && _messages.TryDequeue(out _))
        {
        }

        _logger.LogInformation("BioStack magic link for {Contact}: {MagicLink}", contact, magicLink);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<DevMagicLinkMessage> Latest()
        => _messages.Reverse().ToArray();
}
