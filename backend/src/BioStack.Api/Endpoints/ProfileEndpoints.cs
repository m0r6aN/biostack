namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/profiles")
            .WithTags("Profiles")
            .RequireAuthorization();

        group.MapGet("/", GetAllProfiles)
            .WithName("GetAllProfiles");

        group.MapPost("/", CreateProfile)
            .WithName("CreateProfile");

        group.MapGet("/{id}", GetProfile)
            .WithName("GetProfile");

        group.MapPut("/{id}", UpdateProfile)
            .WithName("UpdateProfile");

        group.MapDelete("/{id}", DeleteProfile)
            .WithName("DeleteProfile");
    }

    private static async Task<IResult> GetAllProfiles(IProfileService profileService, CancellationToken ct)
    {
        var profiles = await profileService.GetAllProfilesAsync(ct);
        return Results.Ok(profiles);
    }

    private static async Task<IResult> CreateProfile(CreateProfileRequest request, IProfileService profileService, CancellationToken ct)
    {
        var profile = await profileService.CreateProfileAsync(request, ct);
        return Results.CreatedAtRoute("GetProfile", new { id = profile.Id }, profile);
    }

    private static async Task<IResult> GetProfile(Guid id, IProfileService profileService, CancellationToken ct)
    {
        var profile = await profileService.GetProfileAsync(id, ct);
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static async Task<IResult> UpdateProfile(Guid id, UpdateProfileRequest request, IProfileService profileService, CancellationToken ct)
    {
        try
        {
            var profile = await profileService.UpdateProfileAsync(id, request, ct);
            return Results.Ok(profile);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> DeleteProfile(Guid id, IProfileService profileService, CancellationToken ct)
    {
        var deleted = await profileService.DeleteProfileAsync(id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
