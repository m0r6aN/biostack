namespace BioStack.Api.Endpoints;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // All admin routes require the AdminOnly policy (role claim == "1")
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/knowledge/ingest", async (
            [FromBody] List<KnowledgeEntry> entries,
            [FromServices] IKnowledgeSource knowledgeSource,
            [FromServices] IMemoryCache memoryCache,
            CancellationToken ct) =>
        {
            if (entries == null || entries.Count == 0)
                return Results.BadRequest("No entries provided");

            try
            {
                var count = await knowledgeSource.IngestBulkAsync(entries, ct);
                // Bust the parser's alias cache so the analyzer immediately reflects newly seeded compounds.
                memoryCache.Remove("analyzer:knowledge:aliases");
                return Results.Ok(new { Message = $"Successfully ingested {count} compounds", Count = count });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        group.MapGet("/stats", async (
            [FromServices] BioStack.Infrastructure.Persistence.BioStackDbContext db,
            CancellationToken ct) =>
        {
            var stats = new
            {
                Profiles             = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.PersonProfiles, ct),
                KnowledgeEntries     = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.KnowledgeEntries, ct),
                TotalCompoundRecords = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.CompoundRecords, ct),
                TotalCheckIns        = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.CheckIns, ct),
                RegisteredUsers      = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.AppUsers, ct),
            };
            return Results.Ok(stats);
        });

        // Promote a user to Admin (admin-only — only existing admins can elevate others)
        group.MapPost("/users/{userId:guid}/promote", async (
            Guid userId,
            [FromServices] BioStack.Infrastructure.Repositories.IAppUserRepository userRepo,
            [FromServices] BioStack.Infrastructure.Persistence.BioStackDbContext db,
            CancellationToken ct) =>
        {
            var user = await userRepo.GetByIdAsync(userId, ct);
            if (user is null) return Results.NotFound();

            user.Role = BioStack.Domain.Enums.UserRole.Admin;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.Id, user.Email, Role = (int)user.Role });
        });

        // Demote Admin back to User
        group.MapPost("/users/{userId:guid}/demote", async (
            Guid userId,
            [FromServices] BioStack.Infrastructure.Repositories.IAppUserRepository userRepo,
            [FromServices] BioStack.Infrastructure.Persistence.BioStackDbContext db,
            CancellationToken ct) =>
        {
            var user = await userRepo.GetByIdAsync(userId, ct);
            if (user is null) return Results.NotFound();

            user.Role = BioStack.Domain.Enums.UserRole.User;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.Id, user.Email, Role = (int)user.Role });
        });

        // List users (admin view)
        group.MapGet("/users", async (
            [FromServices] BioStack.Infrastructure.Persistence.BioStackDbContext db,
            CancellationToken ct) =>
        {
            var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(db.AppUsers.Select(u => new
                {
                    u.Id, u.Email, u.DisplayName, u.AvatarUrl,
                    Role = (int)u.Role, Provider = u.Provider,
                    u.CreatedAtUtc, u.LastSeenAtUtc
                }), ct);
            return Results.Ok(users);
        });
    }
}
