using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BioStack.Infrastructure.Persistence
{
    public class BioStackDbContextFactory : IDesignTimeDbContextFactory<BioStackDbContext>
    {
        public BioStackDbContext CreateDbContext(string[] args)
    {
        var host = Environment.GetEnvironmentVariable("PGHOST") ?? "localhost";
        var user = Environment.GetEnvironmentVariable("PGUSER") ?? "biostack";
        var password = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "biostack_dev_password";
        var database = Environment.GetEnvironmentVariable("PGDATABASE") ?? "biostack";
        var port = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
        var sslMode = Environment.GetEnvironmentVariable("PGSSLMODE") ?? "Prefer";

        var connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode={sslMode}";

        var optionsBuilder = new DbContextOptionsBuilder<BioStackDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new BioStackDbContext(optionsBuilder.Options);
    }
    }
}