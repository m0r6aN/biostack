namespace BioStack.Api.Tests.Unit.Governance;

using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

internal static class SpineTestHelpers
{
    public static SpineRepository CreateRepository(
        BioStackDbContext db,
        SpineCheckpointOptions? checkpointOptions = null)
    {
        var options = Options.Create(checkpointOptions ?? new SpineCheckpointOptions
        {
            // Disable auto/cadence noise in unit tests unless a test opts in.
            AutoCheckpointEveryNEntries = 0,
            CadenceMinutes = 0,
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(options);
        services.AddSingleton<IOptions<SpineCheckpointOptions>>(options);
        services.AddScoped<ISpineRepository>(_ => new SpineRepository(
            db,
            _.GetRequiredService<IServiceProvider>(),
            options,
            NullLogger<SpineRepository>.Instance));
        services.AddScoped<ISpineCheckpointService, SpineCheckpointService>();

        var provider = services.BuildServiceProvider();
        return new SpineRepository(
            db,
            provider,
            options,
            NullLogger<SpineRepository>.Instance);
    }

    public static (SpineRepository spine, SpineCheckpointService checkpoints) CreateWithCheckpoints(
        BioStackDbContext db,
        SpineCheckpointOptions? checkpointOptions = null)
    {
        var options = Options.Create(checkpointOptions ?? new SpineCheckpointOptions
        {
            SigningKey = "unit-test-signing-key-not-for-production",
            AutoCheckpointEveryNEntries = 0,
            CadenceMinutes = 0,
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton<IOptions<SpineCheckpointOptions>>(options);

        // Build a provider that can resolve checkpoint service against this repo.
        ServiceProvider? provider = null;
        services.AddScoped<ISpineRepository>(_ => new SpineRepository(
            db,
            provider!,
            options,
            NullLogger<SpineRepository>.Instance));
        services.AddScoped<ISpineCheckpointService, SpineCheckpointService>();
        provider = services.BuildServiceProvider();

        var spine = provider.GetRequiredService<ISpineRepository>() as SpineRepository
            ?? throw new InvalidOperationException("expected SpineRepository");
        var checkpoints = (SpineCheckpointService)provider.GetRequiredService<ISpineCheckpointService>();
        return (spine, checkpoints);
    }
}
