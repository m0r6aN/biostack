namespace BioStack.Api.Endpoints;

using BioStack.Api.Auth;

/// <summary>
/// Public contact-form intake. Anonymous by design: visitors reach support without an account.
/// </summary>
public static class ContactEndpoints
{
    private const int MaxMessageLength = 4000;

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.Ordinal)
    {
        "Support",
        "Billing",
        "Feedback",
        "Partnership",
        "Other",
    };

    public static void MapContactEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/contact")
            .WithTags("Contact");

        group.MapPost("/", SubmitContactMessage)
            .WithName("SubmitContactMessage")
            .RequireRateLimiting("contact");
    }

    private static async Task<IResult> SubmitContactMessage(
        ContactMessageRequest request,
        IContactMessageSender sender,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var category = request.Category?.Trim() ?? string.Empty;
        var message = request.Message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || !IsSimpleEmail(email))
        {
            return Results.BadRequest(new { error = "A valid email address is required so we can reply." });
        }

        if (!AllowedCategories.Contains(category))
        {
            return Results.BadRequest(new { error = "Category must be one of Support, Billing, Feedback, Partnership, or Other." });
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Results.BadRequest(new { error = "A message is required." });
        }

        if (message.Length > MaxMessageLength)
        {
            return Results.BadRequest(new { error = $"Message must be {MaxMessageLength} characters or fewer." });
        }

        try
        {
            await sender.SendAsync(new ContactMessage(name, email, category, message), ct);
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger(nameof(ContactEndpoints))
                .LogWarning(ex, "Failed to deliver contact message ({Category}) from {Email}", category, email);
            return Results.Json(
                new { error = "We couldn’t deliver your message right now. Please try again later." },
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(new { ok = true });
    }

    /// <summary>Deliberately simple shape check — delivery-level validity is the mail provider's call.</summary>
    private static bool IsSimpleEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0
            && at == email.LastIndexOf('@')
            && at < email.Length - 3
            && email.IndexOf('.', at) > at + 1
            && !email.Any(char.IsWhiteSpace);
    }
}

public sealed record ContactMessageRequest(string? Name, string? Email, string? Category, string? Message);
