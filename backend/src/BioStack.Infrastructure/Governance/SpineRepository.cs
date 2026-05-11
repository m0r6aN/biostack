namespace BioStack.Infrastructure.Governance;

using BioStack.Domain.Governance;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class SpineImmutabilityViolationException(string receiptUri)
    : Exception($"Spine entry for receipt '{receiptUri}' already exists. Receipts are immutable.");

public interface ISpineRepository
{
    Task<SpineEntry> AppendAsync(SpineEntry entry, CancellationToken ct = default);
    Task<SpineEntry?> GetByReceiptUriAsync(string receiptUri, CancellationToken ct = default);
    Task<IReadOnlyList<SpineEntry>> GetBySubjectAsync(string subjectUri, CancellationToken ct = default);
}

public sealed class SpineRepository(BioStackDbContext db) : ISpineRepository
{
    public async Task<SpineEntry> AppendAsync(SpineEntry entry, CancellationToken ct = default)
    {
        var exists = await db.SpineEntries
            .AnyAsync(e => e.ReceiptUri == entry.ReceiptUri, ct);

        if (exists)
            throw new SpineImmutabilityViolationException(entry.ReceiptUri);

        db.SpineEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry;
    }

    public Task<SpineEntry?> GetByReceiptUriAsync(string receiptUri, CancellationToken ct = default)
        => db.SpineEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ReceiptUri == receiptUri, ct);

    public async Task<IReadOnlyList<SpineEntry>> GetBySubjectAsync(string subjectUri, CancellationToken ct = default)
        => await db.SpineEntries
            .AsNoTracking()
            .Where(e => e.SubjectUri == subjectUri)
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync(ct);
}
