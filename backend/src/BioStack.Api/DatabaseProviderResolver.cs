namespace BioStack.Api;

public static class DatabaseProviderResolver
{
    public static bool IsPostgres(string? configuredProvider, string? connectionString)
    {
        if (!string.IsNullOrWhiteSpace(configuredProvider))
        {
            return configuredProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase)
                || configuredProvider.Equals("postgresql", StringComparison.OrdinalIgnoreCase)
                || configuredProvider.Equals("npgsql", StringComparison.OrdinalIgnoreCase);
        }

        return IsPostgres(connectionString);
    }

    public static bool IsPostgres(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Ssl Mode=", StringComparison.OrdinalIgnoreCase);
    }
}