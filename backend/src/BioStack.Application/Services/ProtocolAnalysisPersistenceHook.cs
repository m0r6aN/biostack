namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;

public interface IProtocolAnalysisPersistenceHook
{
    Task RecordAsync(string fingerprintKey, AnalyzeProtocolResponse response, CancellationToken cancellationToken = default);
}

public sealed class NullProtocolAnalysisPersistenceHook : IProtocolAnalysisPersistenceHook
{
    public Task RecordAsync(string fingerprintKey, AnalyzeProtocolResponse response, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
