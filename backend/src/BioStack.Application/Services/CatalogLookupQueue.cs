namespace BioStack.Application.Services;

using System.Threading.Channels;

/// <summary>Hands new lookups to the background processor. In-memory and single-replica by design (the sweep catches anything dropped).</summary>
public interface ICatalogLookupQueue
{
    bool TryEnqueue(Guid lookupId);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct);
}

public sealed class ChannelCatalogLookupQueue : ICatalogLookupQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(256)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
    });

    public bool TryEnqueue(Guid lookupId) => _channel.Writer.TryWrite(lookupId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
