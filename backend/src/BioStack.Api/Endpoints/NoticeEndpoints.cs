namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;

/// <summary>
/// The person's own in-app notes. Every route is owner-only and none is behind the consent gate: reading, marking read or
/// dismissing a note stores no new personal data, and a person on an older consent version must still see why they are
/// being asked to agree again.
/// </summary>
public static class NoticeEndpoints
{
    public static void MapNoticeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/notices")
            .WithTags("Notices")
            .RequireAuthorization();

        group.MapGet("/", List).WithName("ListNotices");
        group.MapGet("/unread-count", UnreadCount).WithName("CountUnreadNotices");
        group.MapPost("/read-all", ReadAll).WithName("ReadAllNotices");
        group.MapPost("/{id:guid}/read", Read).WithName("ReadNotice");
        group.MapDelete("/{id:guid}", Dismiss).WithName("DismissNotice");
    }

    private static async Task<IResult> List(bool? unread, INoticeService notices, IOwnershipGuard ownership, CancellationToken ct)
        => Results.Ok(await notices.ListAsync(ownership.CurrentUserId, unread ?? false, ct));

    private static async Task<IResult> UnreadCount(INoticeService notices, IOwnershipGuard ownership, CancellationToken ct)
        => Results.Ok(new BioStack.Contracts.Responses.UnreadCountResponse(await notices.UnreadCountAsync(ownership.CurrentUserId, ct)));

    private static async Task<IResult> ReadAll(INoticeService notices, IOwnershipGuard ownership, CancellationToken ct)
    {
        await notices.MarkAllReadAsync(ownership.CurrentUserId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Read(Guid id, INoticeService notices, IOwnershipGuard ownership, CancellationToken ct)
        => await notices.MarkReadAsync(ownership.CurrentUserId, id, ct) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> Dismiss(Guid id, INoticeService notices, IOwnershipGuard ownership, CancellationToken ct)
        => await notices.DismissAsync(ownership.CurrentUserId, id, ct) ? Results.NoContent() : Results.NotFound();
}
