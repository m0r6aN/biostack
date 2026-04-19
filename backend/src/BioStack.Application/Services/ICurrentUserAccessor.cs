namespace BioStack.Application.Services;

public interface ICurrentUserAccessor
{
    Guid GetCurrentUserId();
}
