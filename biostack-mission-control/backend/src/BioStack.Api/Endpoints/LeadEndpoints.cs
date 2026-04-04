namespace BioStack.Api.Endpoints;

using BioStack.Contracts.Requests;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class LeadEndpoints
{
    public static void MapLeadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/leads")
            .WithTags("Leads");

        group.MapPost("/capture", CaptureLead)
            .WithName("CaptureLead");
    }

    private static async Task<IResult> CaptureLead(
        LeadCaptureRequest request,
        BioStackDbContext dbContext,
        CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var source = request.Source.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(source))
        {
            return Results.BadRequest(new { error = "Email and source are required." });
        }

        var exists = await dbContext.LeadCaptures.AnyAsync(
            lead => lead.Email == email && lead.Source == source,
            ct);

        if (!exists)
        {
            dbContext.LeadCaptures.Add(new LeadCapture
            {
                Id = Guid.NewGuid(),
                Email = email,
                Source = source,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await dbContext.SaveChangesAsync(ct);
        }

        return Results.NoContent();
    }
}
