namespace BioStack.Application.Services;

using System.Text.Json;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;

/// <summary>
/// The minimal in-app inbox. Any feature creates a note with <see cref="UpsertAsync"/>; a repeated dedupe key updates one
/// note instead of stacking. Notes belong to one person and are never shown to anyone else.
/// </summary>
public interface INoticeService
{
    /// <summary>Creates or updates a note. A changed text brings a read or dismissed note back as unread.</summary>
    Task UpsertAsync(Guid userId, string kind, string dedupeKey, string titleKey, object? values, string? actionPath, Guid? relatedId, CancellationToken ct = default);
    /// <summary>Marks a note answered (read and dismissed), for example when the person confirmed or declined a match.</summary>
    Task ResolveAsync(Guid userId, string dedupeKey, CancellationToken ct = default);
    Task<IReadOnlyList<NoticeResponse>> ListAsync(Guid userId, bool unreadOnly, CancellationToken ct = default);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
    Task<bool> DismissAsync(Guid userId, Guid id, CancellationToken ct = default);
}

public sealed class NoticeService : INoticeService
{
    public const int ListLimit = 50;
    public const int PerUserCap = 200;
    public static readonly TimeSpan ReadRetention = TimeSpan.FromDays(90);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IUserNoticeRepository _notices;

    public NoticeService(IUserNoticeRepository notices) => _notices = notices;

    public async Task UpsertAsync(
        Guid userId, string kind, string dedupeKey, string titleKey, object? values, string? actionPath, Guid? relatedId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A note needs an owner.", nameof(userId));
        if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("A note needs a kind.", nameof(kind));
        if (string.IsNullOrWhiteSpace(dedupeKey)) throw new ArgumentException("A note needs a dedupe key.", nameof(dedupeKey));
        if (!IsAppPath(actionPath)) throw new ArgumentException("A note can only link to a path inside the app.", nameof(actionPath));

        var json = JsonSerializer.Serialize(values ?? new { }, JsonOptions);
        var now = DateTime.UtcNow;
        var existing = await _notices.FindByDedupeKeyAsync(userId, dedupeKey, ct);
        if (existing is null)
        {
            await _notices.AddAsync(new UserNotice
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = kind,
                TitleKey = titleKey,
                ParamsJson = json,
                ActionPath = actionPath,
                RelatedId = relatedId,
                DedupeKey = dedupeKey,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            }, ct);
        }
        else
        {
            var changed = existing.ParamsJson != json || existing.TitleKey != titleKey || existing.ActionPath != actionPath;
            existing.Kind = kind;
            existing.TitleKey = titleKey;
            existing.ParamsJson = json;
            existing.ActionPath = actionPath;
            existing.RelatedId = relatedId;
            if (changed)
            {
                existing.UpdatedAtUtc = now;
                existing.ReadAtUtc = null;
                existing.DismissedAtUtc = null;
            }
        }

        await _notices.SaveChangesAsync(ct);
        await _notices.PruneAsync(userId, now - ReadRetention, PerUserCap, ct);
    }

    public async Task ResolveAsync(Guid userId, string dedupeKey, CancellationToken ct = default)
    {
        var existing = await _notices.FindByDedupeKeyAsync(userId, dedupeKey, ct);
        if (existing is null || existing.DismissedAtUtc is not null) return;
        var now = DateTime.UtcNow;
        existing.ReadAtUtc ??= now;
        existing.DismissedAtUtc = now;
        existing.UpdatedAtUtc = now;
        await _notices.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NoticeResponse>> ListAsync(Guid userId, bool unreadOnly, CancellationToken ct = default)
        => (await _notices.ListAsync(userId, unreadOnly, ListLimit, ct)).Select(Map).ToList();

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default) => _notices.CountUnreadAsync(userId, ct);

    public async Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var note = await _notices.FindAsync(userId, id, ct);
        if (note is null) return false;
        if (note.ReadAtUtc is null)
        {
            note.ReadAtUtc = DateTime.UtcNow;
            await _notices.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
        => await _notices.MarkAllReadAsync(userId, DateTime.UtcNow, ct);

    public async Task<bool> DismissAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var note = await _notices.FindAsync(userId, id, ct);
        if (note is null) return false;
        if (note.DismissedAtUtc is null)
        {
            var now = DateTime.UtcNow;
            note.ReadAtUtc ??= now;
            note.DismissedAtUtc = now;
            await _notices.SaveChangesAsync(ct);
        }

        return true;
    }

    /// <summary>Only a relative path inside this app: one leading slash, no scheme, no host, no backslashes, no control characters.</summary>
    public static bool IsAppPath(string? path)
    {
        if (path is null) return true;
        if (path.Length is 0 or > 200) return false;
        if (path[0] != '/' || path.StartsWith("//", StringComparison.Ordinal)) return false;
        return !path.Contains('\\') && !path.Any(char.IsControl) && !path.Contains("://", StringComparison.Ordinal);
    }

    private static NoticeResponse Map(UserNotice note)
    {
        JsonElement values;
        try
        {
            values = JsonDocument.Parse(note.ParamsJson).RootElement.Clone();
        }
        catch (JsonException)
        {
            values = JsonDocument.Parse("{}").RootElement.Clone();
        }

        return new NoticeResponse(
            note.Id, note.Kind, note.TitleKey, values, note.ActionPath, note.RelatedId,
            note.CreatedAtUtc, note.UpdatedAtUtc, note.ReadAtUtc is not null);
    }
}
