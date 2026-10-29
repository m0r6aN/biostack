namespace BioStack.Api.AccountData;

using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>How a table relates to a person, for export and deletion.</summary>
public enum UserDataDisposition
{
    /// <summary>Rows belong to one user: exported and removed (or irreversibly anonymized) on deletion.</summary>
    UserScoped,

    /// <summary>Rows carry a pseudonymous actor id for the user: exported, but cannot be edited (append-only hash chain).</summary>
    RetainedPseudonymous,

    /// <summary>Stored with no link to a person by design (hashed queries, salted keys for signed-out visitors).</summary>
    AnonymousByDesign,

    /// <summary>Shared product content or operator data, not about any one user.</summary>
    GlobalReference,
}

/// <summary>Everything the export and deletion paths know about the person whose data is being handled.</summary>
public sealed record AccountScope(Guid UserId, IReadOnlyList<string> Emails, string ActorId, string SubmitterKey);

/// <summary>
/// One entry per table in the model. Adding a table to <see cref="BioStackDbContext"/> without adding it to
/// <see cref="AccountDataCatalog"/> fails the completeness test, so a new per-user table cannot ship without
/// an export mapping and a deletion rule.
/// </summary>
public abstract class AccountDataTable
{
    protected AccountDataTable(string name, Type entityType, UserDataDisposition disposition, string note)
    {
        Name = name;
        EntityType = entityType;
        Disposition = disposition;
        Note = note;
    }

    public string Name { get; }
    public Type EntityType { get; }
    public UserDataDisposition Disposition { get; }
    public string Note { get; }

    public bool IsExported => Disposition is UserDataDisposition.UserScoped or UserDataDisposition.RetainedPseudonymous;
    public bool IsDeleted => Disposition is UserDataDisposition.UserScoped;

    public abstract Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExportAsync(
        BioStackDbContext db, AccountScope scope, CancellationToken ct);

    public abstract Task<int> CountAsync(BioStackDbContext db, AccountScope scope, CancellationToken ct);

    /// <summary>Removes or anonymizes this user's rows. Returns the number of rows affected.</summary>
    public abstract Task<int> DeleteAsync(BioStackDbContext db, AccountScope scope, CancellationToken ct);
}

/// <summary>A table with no per-user rows (global content, operator data, or anonymous by design).</summary>
public sealed class ReferenceTable : AccountDataTable
{
    public ReferenceTable(string name, Type entityType, UserDataDisposition disposition, string note)
        : base(name, entityType, disposition, note)
    {
        if (disposition is UserDataDisposition.UserScoped or UserDataDisposition.RetainedPseudonymous)
            throw new ArgumentException("A reference table cannot hold per-user rows.", nameof(disposition));
    }

    public override Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExportAsync(BioStackDbContext db, AccountScope scope, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>([]);

    public override Task<int> CountAsync(BioStackDbContext db, AccountScope scope, CancellationToken ct) => Task.FromResult(0);
    public override Task<int> DeleteAsync(BioStackDbContext db, AccountScope scope, CancellationToken ct) => Task.FromResult(0);
}

/// <summary>A table whose rows can be selected for one user.</summary>
public sealed class ScopedTable<T> : AccountDataTable where T : class
{
    private readonly Func<BioStackDbContext, AccountScope, IQueryable<T>> _query;
    private readonly string[] _omitFromExport;
    private readonly Func<BioStackDbContext, AccountScope, CancellationToken, Task<int>>? _delete;

    public ScopedTable(
        string name,
        UserDataDisposition disposition,
        string note,
        Func<BioStackDbContext, AccountScope, IQueryable<T>> query,
        string[]? omitFromExport = null,
        Func<BioStackDbContext, AccountScope, CancellationToken, Task<int>>? delete = null)
        : base(name, typeof(T), disposition, note)
    {
        if (disposition is UserDataDisposition.AnonymousByDesign or UserDataDisposition.GlobalReference)
            throw new ArgumentException("A scoped table must hold per-user rows.", nameof(disposition));
        _query = query;
        _omitFromExport = omitFromExport ?? [];
        _delete = delete;
    }

    public override async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExportAsync(
        BioStackDbContext db, AccountScope scope, CancellationToken ct)
    {
        var entityType = db.Model.FindEntityType(typeof(T))
            ?? throw new InvalidOperationException($"{typeof(T).Name} is not part of the model.");
        var properties = entityType.GetProperties()
            .Where(p => !_omitFromExport.Contains(p.Name, StringComparer.Ordinal))
            .ToList();

        var rows = await _query(db, scope).AsNoTracking().ToListAsync(ct);
        var result = new List<IReadOnlyDictionary<string, object?>>(rows.Count);
        foreach (var row in rows)
        {
            var map = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var property in properties)
            {
                map[ToCamel(property.Name)] = Normalize(property.GetGetter().GetClrValue(row));
            }

            result.Add(map);
        }

        return result;
    }

    public override Task<int> CountAsync(BioStackDbContext db, AccountScope scope, CancellationToken ct)
        => _query(db, scope).CountAsync(ct);

    public override Task<int> DeleteAsync(BioStackDbContext db, AccountScope scope, CancellationToken ct)
    {
        if (!IsDeleted) return Task.FromResult(0);
        return _delete is not null
            ? _delete(db, scope, ct)
            : _query(db, scope).ExecuteDeleteAsync(ct);
    }

    private static object? Normalize(object? value) => value switch
    {
        null => null,
        Enum e => e.ToString(),
        DateTime d => DateTime.SpecifyKind(d, DateTimeKind.Utc),
        byte[] bytes => Convert.ToBase64String(bytes),
        _ => value,
    };

    private static string ToCamel(string name) => string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
