namespace BioStack.Api.Tests;

using BioStack.Api;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

public class DatabaseSchemaBootstrapperTests
{
    [Fact]
    public void MakeSqliteCreateScriptIdempotent_AddsIfNotExistsGuards()
    {
        const string script = """
CREATE TABLE "KnowledgeEntries" (
    "Id" TEXT NOT NULL
);

CREATE UNIQUE INDEX "IX_Test" ON "KnowledgeEntries" ("Id");
CREATE INDEX "IX_Other" ON "KnowledgeEntries" ("Id");
""";

        var result = DatabaseSchemaBootstrapper.MakeSqliteCreateScriptIdempotent(script);

        Assert.Contains("CREATE TABLE IF NOT EXISTS \"KnowledgeEntries\"", result);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Test\"", result);
        Assert.Contains("CREATE INDEX IF NOT EXISTS \"IX_Other\"", result);
    }

    [Fact]
    public void BuildAddColumnSql_UsesModelMetadataForNullableColumns()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var db = new BioStackDbContext(options);
        var entityType = db.Model.FindEntityType("BioStack.Domain.Entities.PersonProfile")!;
        var property = entityType.FindProperty("Age")!;
        var tableIdentifier = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());

        var sql = DatabaseSchemaBootstrapper.BuildAddColumnSql("PersonProfiles", "Age", property, tableIdentifier);

        Assert.Equal("ALTER TABLE \"PersonProfiles\" ADD COLUMN \"Age\" INTEGER NULL;", sql);
    }
}