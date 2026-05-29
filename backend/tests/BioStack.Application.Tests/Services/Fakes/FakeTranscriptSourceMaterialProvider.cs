namespace BioStack.Application.Tests.Services.Fakes;

using BioStack.Application.Services;
using BioStack.Application.Tests.Fixtures;

internal sealed class FakeTranscriptSourceMaterialProvider : ITranscriptSourceMaterialProvider
{
    public bool NetworkAttempted { get; private set; }

    public Task<TranscriptSourceMaterialResult> ResolveAsync(
        TranscriptSourceReference sourceReference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.Equals(sourceReference.SourceType, Tb500TranscriptFixture.SourceType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(sourceReference.SourceUrl, Tb500TranscriptFixture.SourceUrl, StringComparison.Ordinal))
        {
            return Task.FromResult(Tb500TranscriptFixture.CreateResult());
        }

        throw new TranscriptSourceMaterialProviderException(
            failure: new TranscriptSourceMaterialResolutionFailure(
                Code: "transcript_source_not_found",
                Message: $"No deterministic transcript fixture exists for '{sourceReference.SourceType}:{sourceReference.SourceUrl}'."));
    }
}

internal sealed class TranscriptSourceMaterialProviderException : Exception
{
    public TranscriptSourceMaterialProviderException(TranscriptSourceMaterialResolutionFailure failure)
        : base(failure.Message)
    {
        Failure = failure;
    }

    public TranscriptSourceMaterialResolutionFailure Failure { get; }
}
