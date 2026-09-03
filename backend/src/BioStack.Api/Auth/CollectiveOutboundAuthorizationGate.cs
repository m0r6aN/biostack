namespace BioStack.Api.Auth;

using BioStack.Application.Services;
using BioStack.Cognition.CollectiveApi;

public sealed class CollectiveOutboundAuthorizationGate : ICollectiveOutboundAuthorizationGate
{
    private readonly IConsentGate _consentGate;

    public CollectiveOutboundAuthorizationGate(IConsentGate consentGate)
    {
        _consentGate = consentGate;
    }

    public Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken = default) =>
        _consentGate.IsConsentGrantedAsync(cancellationToken);
}
