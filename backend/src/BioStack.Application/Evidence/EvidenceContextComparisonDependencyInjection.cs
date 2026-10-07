namespace BioStack.Application.Evidence;

using BioStack.Domain.Evidence;
using Microsoft.Extensions.DependencyInjection;

public static class EvidenceContextComparisonDependencyInjection
{
    public static IServiceCollection AddEvidenceContextComparison(this IServiceCollection services)
    {
        services.AddSingleton<IEvidenceContextComparisonService, EvidenceContextComparisonService>();
        services.AddSingleton<ProtocolEvidenceContextComparer>();
        return services;
    }
}
