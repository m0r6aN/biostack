namespace BioStack.Api.Tests;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class InfrastructureRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public InfrastructureRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task ProtocolRepository_GetByPersonIdAsync_ReturnsOnlyThatPersonsProtocolsNewestFirstWithItems()
    {
        var personId = Guid.NewGuid();
        var otherPersonId = Guid.NewGuid();
        var oldProtocolId = Guid.NewGuid();
        var newProtocolId = Guid.NewGuid();
        var compoundId = Guid.NewGuid();

        using (var seedContext = CreateDbContext())
        {
            seedContext.PersonProfiles.AddRange(
                CreateProfile(personId, "Primary"),
                CreateProfile(otherPersonId, "Other"));
            seedContext.CompoundRecords.Add(new CompoundRecord
            {
                Id = compoundId,
                PersonId = personId,
                Name = "BPC-157",
                Category = CompoundCategory.Peptide,
                Status = CompoundStatus.Active
            });
            seedContext.Protocols.AddRange(
                CreateProtocol(oldProtocolId, personId, "Older", createdAtUtc: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)),
                CreateProtocol(newProtocolId, personId, "Newer", createdAtUtc: new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc)),
                CreateProtocol(Guid.NewGuid(), otherPersonId, "Wrong person", createdAtUtc: new DateTime(2026, 1, 3, 12, 0, 0, DateTimeKind.Utc)));
            seedContext.ProtocolItems.Add(new ProtocolItem
            {
                Id = Guid.NewGuid(),
                ProtocolId = newProtocolId,
                CompoundRecordId = compoundId,
                Notes = "Morning"
            });

            await seedContext.SaveChangesAsync();
        }

        using var lookupContext = CreateDbContext();
        var repository = new ProtocolRepository(lookupContext);

        var protocols = (await repository.GetByPersonIdAsync(personId)).ToList();

        Assert.Equal(new[] { newProtocolId, oldProtocolId }, protocols.Select(protocol => protocol.Id));
        var newest = Assert.Single(protocols[0].Items);
        Assert.Equal("BPC-157", newest.CompoundRecord?.Name);
    }

    [Fact]
    public async Task ProtocolRepository_GetLineageAsync_ReturnsRootAndChildrenInVersionOrder()
    {
        var personId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var draftId = Guid.NewGuid();

        using (var seedContext = CreateDbContext())
        {
            seedContext.PersonProfiles.Add(CreateProfile(personId, "Lineage"));
            seedContext.Protocols.AddRange(
                CreateProtocol(rootId, personId, "Root", version: 1),
                CreateProtocol(draftId, personId, "Draft", version: 3, originProtocolId: rootId),
                CreateProtocol(childId, personId, "Child", version: 2, originProtocolId: rootId),
                CreateProtocol(Guid.NewGuid(), personId, "Unrelated", version: 4));

            await seedContext.SaveChangesAsync();
        }

        using var lookupContext = CreateDbContext();
        var repository = new ProtocolRepository(lookupContext);
        var root = await repository.GetByIdAsync(rootId);

        var lineage = (await repository.GetLineageAsync(root!)).ToList();
        var maxVersion = await repository.GetMaxVersionInLineageAsync(root!);

        Assert.Equal(new[] { rootId, childId, draftId }, lineage.Select(protocol => protocol.Id));
        Assert.Equal(3, maxVersion);
    }

    [Fact]
    public async Task CheckInRepository_GetByPersonIdAsync_ReturnsOnlyThatPersonsCheckInsNewestFirst()
    {
        var personId = Guid.NewGuid();
        var otherPersonId = Guid.NewGuid();
        var newestId = Guid.NewGuid();
        var oldestId = Guid.NewGuid();

        using (var seedContext = CreateDbContext())
        {
            seedContext.PersonProfiles.AddRange(
                CreateProfile(personId, "Primary"),
                CreateProfile(otherPersonId, "Other"));
            seedContext.CheckIns.AddRange(
                CreateCheckIn(oldestId, personId, new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc)),
                CreateCheckIn(newestId, personId, new DateTime(2026, 1, 3, 8, 0, 0, DateTimeKind.Utc)),
                CreateCheckIn(Guid.NewGuid(), otherPersonId, new DateTime(2026, 1, 4, 8, 0, 0, DateTimeKind.Utc)));

            await seedContext.SaveChangesAsync();
        }

        using var lookupContext = CreateDbContext();
        var repository = new CheckInRepository(lookupContext);

        var checkIns = (await repository.GetByPersonIdAsync(personId)).ToList();

        Assert.Equal(new[] { newestId, oldestId }, checkIns.Select(checkIn => checkIn.Id));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private BioStackDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new BioStackDbContext(options);
    }

    private static PersonProfile CreateProfile(Guid id, string displayName)
    {
        return new PersonProfile
        {
            Id = id,
            DisplayName = displayName,
            Sex = Sex.Unspecified,
            Weight = 80
        };
    }

    private static Protocol CreateProtocol(
        Guid id,
        Guid personId,
        string name,
        int version = 1,
        Guid? originProtocolId = null,
        DateTime? createdAtUtc = null)
    {
        return new Protocol
        {
            Id = id,
            PersonId = personId,
            Name = name,
            Version = version,
            OriginProtocolId = originProtocolId,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            UpdatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };
    }

    private static CheckIn CreateCheckIn(Guid id, Guid personId, DateTime date)
    {
        return new CheckIn
        {
            Id = id,
            PersonId = personId,
            Date = date,
            Weight = 80,
            SleepQuality = 7,
            Energy = 8,
            Appetite = 6,
            Recovery = 7
        };
    }
}
