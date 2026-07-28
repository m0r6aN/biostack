namespace BioStack.KnowledgeWorker.Pipeline;

using System.Net;

internal static class SourceAcquisitionHttpTransport
{
    public static HttpClient CreateRedirectDisabledAnonymousClient(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        return new HttpClient(CreateRedirectDisabledHandler(), disposeHandler: true)
        {
            Timeout = timeout,
        };
    }

    internal static SocketsHttpHandler CreateRedirectDisabledHandler()
        => new()
        {
            AllowAutoRedirect = false,
            AutomaticDecompression =
                DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };

    public static async Task<byte[]> ReadBoundedBodyAsync(
        HttpContent content,
        int maximumResponseBytes,
        string sourceDisplayName,
        CancellationToken cancellationToken)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (maximumResponseBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumResponseBytes));
        }
        if (string.IsNullOrWhiteSpace(sourceDisplayName))
        {
            throw new ArgumentException(
                "Source display name is required.",
                nameof(sourceDisplayName));
        }

        if (content.Headers.ContentLength > maximumResponseBytes)
        {
            throw ResponseTooLarge(sourceDisplayName);
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > maximumResponseBytes)
            {
                throw ResponseTooLarge(sourceDisplayName);
            }
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
        return buffer.ToArray();
    }

    private static SourceAcquisitionException ResponseTooLarge(string sourceDisplayName)
        => new(
            "response-too-large",
            $"{sourceDisplayName} response exceeded the configured size limit.");
}

internal interface ISourceRequestGate
{
    ValueTask<IDisposable> AcquireAsync(CancellationToken cancellationToken);
}

internal sealed record SourceSlidingWindowBudget(
    int MaximumRequests,
    TimeSpan Window,
    string ExhaustedCode,
    string ExhaustedMessage);

internal sealed record SourceDailyRequestBudget(
    int MaximumRequests,
    string ExhaustedCode,
    string ExhaustedMessage);

internal sealed class SerializedSourceRequestGate : ISourceRequestGate
{
    private readonly TimeProvider _timeProvider;
    private readonly IReadOnlyList<WindowState> _windowStates;
    private readonly SourceDailyRequestBudget? _dailyBudget;
    private readonly SemaphoreSlim _serialGate = new(1, 1);
    private DateOnly _budgetDateUtc;
    private int _dailyRequests;

    public SerializedSourceRequestGate(
        TimeProvider timeProvider,
        IReadOnlyList<SourceSlidingWindowBudget> windowBudgets,
        SourceDailyRequestBudget? dailyBudget)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        if (windowBudgets is null) throw new ArgumentNullException(nameof(windowBudgets));
        if (windowBudgets.Any(budget =>
                budget is null
                || budget.MaximumRequests <= 0
                || budget.Window <= TimeSpan.Zero
                || string.IsNullOrWhiteSpace(budget.ExhaustedCode)
                || string.IsNullOrWhiteSpace(budget.ExhaustedMessage)))
        {
            throw new ArgumentException(
                "Every sliding-window budget must be positive and declare an error.",
                nameof(windowBudgets));
        }
        if (dailyBudget is not null
            && (dailyBudget.MaximumRequests <= 0
                || string.IsNullOrWhiteSpace(dailyBudget.ExhaustedCode)
                || string.IsNullOrWhiteSpace(dailyBudget.ExhaustedMessage)))
        {
            throw new ArgumentException(
                "The daily budget must be positive and declare an error.",
                nameof(dailyBudget));
        }

        _windowStates = windowBudgets
            .Select(budget => new WindowState(budget))
            .ToList();
        _dailyBudget = dailyBudget;
    }

    public async ValueTask<IDisposable> AcquireAsync(
        CancellationToken cancellationToken)
    {
        await _serialGate.WaitAsync(cancellationToken);
        try
        {
            var now = _timeProvider.GetUtcNow();
            var date = DateOnly.FromDateTime(now.UtcDateTime);
            if (date != _budgetDateUtc)
            {
                _budgetDateUtc = date;
                _dailyRequests = 0;
            }

            foreach (var state in _windowStates)
            {
                while (state.Requests.TryPeek(out var requestAt)
                       && now - requestAt >= state.Budget.Window)
                {
                    state.Requests.Dequeue();
                }
                if (state.Requests.Count >= state.Budget.MaximumRequests)
                {
                    throw new SourceAcquisitionException(
                        state.Budget.ExhaustedCode,
                        state.Budget.ExhaustedMessage);
                }
            }
            if (_dailyBudget is not null
                && _dailyRequests >= _dailyBudget.MaximumRequests)
            {
                throw new SourceAcquisitionException(
                    _dailyBudget.ExhaustedCode,
                    _dailyBudget.ExhaustedMessage);
            }

            foreach (var state in _windowStates)
            {
                state.Requests.Enqueue(now);
            }
            if (_dailyBudget is not null) _dailyRequests++;
            return new RequestLease(_serialGate);
        }
        catch
        {
            _serialGate.Release();
            throw;
        }
    }

    private sealed record WindowState(SourceSlidingWindowBudget Budget)
    {
        public Queue<DateTimeOffset> Requests { get; } = new();
    }

    private sealed class RequestLease(SemaphoreSlim serialGate) : IDisposable
    {
        private SemaphoreSlim? _serialGate = serialGate;

        public void Dispose()
            => Interlocked.Exchange(ref _serialGate, null)?.Release();
    }
}
