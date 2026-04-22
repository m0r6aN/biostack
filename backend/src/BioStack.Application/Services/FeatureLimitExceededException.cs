namespace BioStack.Application.Services;

using BioStack.Domain.Enums;

public sealed class FeatureLimitExceededException : InvalidOperationException
{
    public FeatureLimitExceededException(string code, string message, ProductTier tier, int? limit)
        : base(message)
    {
        Code = code;
        Tier = tier;
        Limit = limit;
    }

    public string Code { get; }
    public ProductTier Tier { get; }
    public int? Limit { get; }
}
