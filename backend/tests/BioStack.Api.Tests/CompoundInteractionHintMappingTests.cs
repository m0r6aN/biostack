namespace BioStack.Api.Tests;

using System.Collections.Generic;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class CompoundInteractionHintMappingTests
{
    [Fact]
    public void MechanismOverlap_HasStringValueConverter_SoTextColumnsRoundTripOnEveryProvider()
    {
        using var context = CreateContext();
        var property = context.Model
            .FindEntityType(typeof(CompoundInteractionHint))!
            .FindProperty(nameof(CompoundInteractionHint.MechanismOverlap))!;

        var converter = property.GetValueConverter();
        Assert.NotNull(converter);
        Assert.Equal(typeof(string), converter.ProviderClrType);

        Assert.Equal(
            "hepatic|renal",
            converter.ConvertToProvider(new List<string> { "hepatic", "renal" }));
        Assert.Equal(
            new List<string> { "hepatic", "renal" },
            (List<string>?)converter.ConvertFromProvider("hepatic|renal"));
        Assert.Null(converter.ConvertFromProvider(null));
    }

    private static BioStackDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseNpgsql("Host=localhost;Database=biostack_model_shape")
            .Options;
        return new BioStackDbContext(options);
    }
}
