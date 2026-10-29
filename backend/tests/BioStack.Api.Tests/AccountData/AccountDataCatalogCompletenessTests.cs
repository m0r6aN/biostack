namespace BioStack.Api.Tests.AccountData;

using BioStack.Api.AccountData;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

/// <summary>
/// The guard that stops a new per-user table from shipping without an export mapping and a deletion rule.
/// If one of these fails after you add an entity: add it to AccountDataCatalog (UserScoped with a query, or a
/// ReferenceTable with the reason it is not about a person), then seed it in AccountExportDeletionIntegrationTests.
/// </summary>
public sealed class AccountDataCatalogCompletenessTests
{
    private static List<IEntityType> ModelEntities()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var context = new BioStackDbContext(options);
        return context.Model.GetEntityTypes().Where(e => !e.IsOwned()).ToList();
    }

    [Fact]
    public void EveryEntityInTheModelIsClassifiedExactlyOnce()
    {
        var modelTypes = ModelEntities().Select(e => e.ClrType).ToHashSet();
        var catalogTypes = AccountDataCatalog.Tables.Select(t => t.EntityType).ToList();

        var unclassified = modelTypes.Except(catalogTypes).Select(t => t.Name).OrderBy(n => n).ToList();
        Assert.True(unclassified.Count == 0,
            "New table(s) with no export/deletion classification in AccountDataCatalog: " + string.Join(", ", unclassified));

        var unknown = catalogTypes.Except(modelTypes).Select(t => t.Name).ToList();
        Assert.True(unknown.Count == 0, "AccountDataCatalog lists types that are not in the model: " + string.Join(", ", unknown));

        var duplicates = catalogTypes.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key.Name).ToList();
        Assert.True(duplicates.Count == 0, "Entity listed more than once: " + string.Join(", ", duplicates));

        var duplicateNames = AccountDataCatalog.Tables.GroupBy(t => t.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(duplicateNames);
    }

    [Fact]
    public void CatalogNamesMatchTheDbSetTableNames()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var context = new BioStackDbContext(options);
        foreach (var table in AccountDataCatalog.Tables)
        {
            var entity = context.Model.FindEntityType(table.EntityType);
            Assert.NotNull(entity);
            Assert.Equal(entity!.GetTableName(), table.Name);
        }
    }

    private static readonly HashSet<string> PersonKeyColumns = new(StringComparer.Ordinal)
    {
        "UserId", "AppUserId", "OwnerId", "PersonId", "ProfileId", "IdentityId", "ActorId", "SubmitterKey", "Email", "StripeCustomerId",
    };

    [Fact]
    public void AnyTableThatLooksLikeItHoldsPersonDataIsNotLeftAsReference()
    {
        // A table carrying a person key (user id, email, actor id...) must be exported and handled on deletion.
        // Reference tables may not carry one unless it is listed here with the reason it is not about a person.
        var allowedReference = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var table in AccountDataCatalog.Tables.Where(t => !t.IsExported))
        {
            var entity = ModelEntities().Single(e => e.ClrType == table.EntityType);
            var personColumns = entity.GetProperties().Select(p => p.Name).Where(PersonKeyColumns.Contains).ToList();
            if (personColumns.Count == 0) continue;
            Assert.True(allowedReference.ContainsKey(table.Name),
                $"{table.Name} is classified {table.Disposition} but has person-key column(s) {string.Join(", ", personColumns)}. Classify it UserScoped or justify it.");
        }
    }

    [Fact]
    public void ForeignKeysToAnAccountOrProfileAreAllHandledByUserScopedTables()
    {
        var accountRoots = new[] { "AppUser", "PersonProfile", "Protocol", "AuthIdentity" };
        foreach (var entity in ModelEntities())
        {
            foreach (var fk in entity.GetForeignKeys().Where(f => accountRoots.Contains(f.PrincipalEntityType.ClrType.Name)))
            {
                var table = AccountDataCatalog.Tables.Single(t => t.EntityType == entity.ClrType);
                Assert.True(table.IsDeleted,
                    $"{entity.ClrType.Name} references {fk.PrincipalEntityType.ClrType.Name} but is not removed on account deletion.");
            }
        }
    }

    [Fact]
    public void DeletionOrderRespectsEveryRestrictiveForeignKey()
    {
        var deleted = AccountDataCatalog.DeletedTables.Select(t => t.EntityType).ToList();
        foreach (var entity in ModelEntities().Where(e => deleted.Contains(e.ClrType)))
        {
            foreach (var fk in entity.GetForeignKeys())
            {
                var principal = fk.PrincipalEntityType.ClrType;
                if (principal == entity.ClrType || !deleted.Contains(principal)) continue;
                if (fk.DeleteBehavior is not (DeleteBehavior.Restrict or DeleteBehavior.NoAction or DeleteBehavior.ClientNoAction)) continue;
                Assert.True(deleted.IndexOf(entity.ClrType) < deleted.IndexOf(principal),
                    $"{entity.ClrType.Name} must be deleted before {principal.Name} (restrictive foreign key).");
            }
        }
    }

    [Fact]
    public void DeletedTablesEndWithTheAccountRow()
    {
        Assert.Equal("AppUsers", AccountDataCatalog.DeletedTables.Last().Name);
    }

    [Fact]
    public void SecretColumnsAreNeverExported()
    {
        var omitted = new Dictionary<string, string[]>
        {
            ["AuthChallenges"] = ["TokenHash"],
            ["Sessions"] = ["TokenHash"],
            ["PasskeyCredentials"] = ["CredentialId", "PublicKey", "UserHandle"],
            ["PasskeyOperationChallenges"] = ["OptionsJson", "RequestIdHash"],
            ["EvidenceAlertPreferences"] = ["UnsubscribeToken"],
            ["ClaimSubmissions"] = ["SubmitterKey"],
        };
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var context = new BioStackDbContext(options);
        foreach (var (name, columns) in omitted)
        {
            var table = AccountDataCatalog.Tables.Single(t => t.Name == name);
            Assert.True(table.IsExported, name);
            foreach (var column in columns)
                Assert.NotNull(context.Model.FindEntityType(table.EntityType)!.FindProperty(column));
        }
    }

    [Fact]
    public void TheBackupWindowMatchesPostgresPitrRetention()
    {
        Assert.Equal(35, AccountDataCatalog.BackupRetentionDays);
    }
}
