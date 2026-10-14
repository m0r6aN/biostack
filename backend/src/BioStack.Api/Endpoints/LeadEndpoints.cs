namespace BioStack.Api.Endpoints;

using System.Net.Mail;
using BioStack.Contracts.Requests;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class LeadEndpoints
{
    public const int MaxEmailLength = 254;
    public const int MaxSourceLength = 80;

    private static readonly HashSet<string> AllowedSources = new(StringComparer.Ordinal)
    {
        "clinics-coaches-pricing",
        "reconstitution-calculator",
    };

    public static void MapLeadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/leads")
            .WithTags("Leads");

        group.MapPost("/capture", CaptureLead)
            .RequireRateLimiting("lead-capture")
            .WithName("CaptureLead");
    }

    private static async Task<IResult> CaptureLead(
        LeadCaptureRequest request,
        BioStackDbContext dbContext,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return Results.NoContent();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var source = request.Source.Trim();

        if (!IsAcceptableEmail(email) ||
            source.Length is < 1 or > MaxSourceLength ||
            !AllowedSources.Contains(source))
        {
            return Results.BadRequest(new { error = "A valid email and a known source are required." });
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

    private static bool IsAcceptableEmail(string email)
        => email.Length is > 0 and <= MaxEmailLength && MailAddress.TryCreate(email, out _);
}
