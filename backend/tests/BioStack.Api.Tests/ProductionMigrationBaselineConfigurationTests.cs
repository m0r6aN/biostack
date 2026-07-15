using BioStack.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace BioStack.Api.Tests;

public sealed class ProductionMigrationBaselineConfigurationTests
{
    [Fact]
    public void Configure_LogsPendingModelChangesWarning()
    {
        var optionsBuilder = new DbContextOptionsBuilder();

        ProductionMigrationBaselineConfiguration.Configure(optionsBuilder);

        var coreOptions = optionsBuilder.Options.FindExtension<CoreOptionsExtension>();
        Assert.NotNull(coreOptions);
        Assert.Equal(
            WarningBehavior.Log,
            coreOptions.WarningsConfiguration.GetBehavior(RelationalEventId.PendingModelChangesWarning));
    }

    [Fact]
    public void Configure_DoesNotChangeOtherRelationalWarningBehavior()
    {
        var optionsBuilder = new DbContextOptionsBuilder();

        ProductionMigrationBaselineConfiguration.Configure(optionsBuilder);

        var coreOptions = optionsBuilder.Options.FindExtension<CoreOptionsExtension>();
        Assert.NotNull(coreOptions);
        Assert.Null(
            coreOptions.WarningsConfiguration.GetBehavior(RelationalEventId.MultipleCollectionIncludeWarning));
    }
}
