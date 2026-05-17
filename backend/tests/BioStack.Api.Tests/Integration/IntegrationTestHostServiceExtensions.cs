namespace BioStack.Api.Tests.Integration;

using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

internal static class IntegrationTestHostServiceExtensions
{
    // EF Core 8+ registers IDbContextOptionsConfiguration<TContext> via Add (not TryAdd),
    // so every AddDbContext call appends another options-builder lambda. Removing only
    // DbContextOptions<TContext> leaves Program.cs's UseNpgsql lambda live, and the test's
    // UseSqlite lambda then runs on top of it, tripping EF Core's "single provider" guard.
    public static IServiceCollection RemoveBioStackDbContext(this IServiceCollection services)
    {
        services.RemoveAll<IDbContextOptionsConfiguration<BioStackDbContext>>();
        services.RemoveAll<DbContextOptions<BioStackDbContext>>();
        return services;
    }
}
