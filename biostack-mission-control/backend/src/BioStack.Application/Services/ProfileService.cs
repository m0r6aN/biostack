namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public sealed class ProfileService : IProfileService
{
    private readonly IPersonProfileRepository _profileRepository;

    public ProfileService(IPersonProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<ProfileResponse> CreateProfileAsync(CreateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = new PersonProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = request.DisplayName,
            Sex = request.Sex,
            Weight = request.Weight,
            Age = request.Age,
            GoalSummary = request.GoalSummary ?? string.Empty,
            Notes = request.Notes ?? string.Empty,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _profileRepository.AddAsync(profile, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(profile);
    }

    public async Task<ProfileResponse?> GetProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByIdAsync(id, cancellationToken);
        return profile is null ? null : MapToResponse(profile);
    }

    public async Task<IEnumerable<ProfileResponse>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await _profileRepository.GetAllAsync(cancellationToken);
        return profiles.Select(MapToResponse);
    }

    public async Task<ProfileResponse> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByIdAsync(id, cancellationToken);
        if (profile is null)
            throw new InvalidOperationException($"Profile with ID {id} not found");

        profile.DisplayName = request.DisplayName;
        profile.Sex = request.Sex;
        profile.Weight = request.Weight;
        profile.Age = request.Age;
        profile.GoalSummary = request.GoalSummary ?? string.Empty;
        profile.Notes = request.Notes ?? string.Empty;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _profileRepository.UpdateAsync(profile, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(profile);
    }

    public async Task<bool> DeleteProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByIdAsync(id, cancellationToken);
        if (profile is null)
            return false;

        await _profileRepository.DeleteAsync(profile, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProfileResponse MapToResponse(PersonProfile profile)
    {
        return new ProfileResponse(
            profile.Id,
            profile.DisplayName,
            profile.Sex,
            profile.Age,
            profile.Weight,
            profile.GoalSummary,
            profile.Notes,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc
        );
    }
}

public interface IProfileService
{
    Task<ProfileResponse> CreateProfileAsync(CreateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ProfileResponse?> GetProfileAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfileResponse>> GetAllProfilesAsync(CancellationToken cancellationToken = default);
    Task<ProfileResponse> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteProfileAsync(Guid id, CancellationToken cancellationToken = default);
}

