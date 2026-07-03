namespace BioStack.Application.Abstractions;

using BioStack.Contracts.Responses;

public interface IProtocolOperationsExportBundleVerifier
{
    ProtocolOperationsExportBundleVerificationResult Verify(ProtocolOperationsExportBundle? bundle);
}
