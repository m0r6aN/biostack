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
        var password = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "";
        var database = Environment.GetEnvironmentVariable("PGDATABASE") ?? "biostack";
        var port = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";

        var connectionString = $"Server={host};Port={port};Database={database};User Id={user};Password={password};SSL Mode=Require";

        var optionsBuilder = new DbContextOptionsBuilder<BioStackDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new BioStackDbContext(optionsBuilder.Options);
    }
    }
}