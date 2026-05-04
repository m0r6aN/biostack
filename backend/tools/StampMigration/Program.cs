using Npgsql;

var connStr = args.Length > 0 ? args[0] :
    "Host=biostackmcpg20260407e2.postgres.database.azure.com;Port=5432;Database=postgres;Username=biostackadmin;Password=Bi0St@ckR0ckz2026!;SSL Mode=Require;Trust Server Certificate=true";

await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
Console.WriteLine($"Connected to: {conn.Host}:{conn.Port}/{conn.Database}");

// Drop all user tables in the public schema to allow a clean EF migration from scratch
Console.WriteLine("Dropping all tables in public schema...");
await using var listCmd = conn.CreateCommand();
listCmd.CommandText = """
    SELECT tablename FROM pg_tables
    WHERE schemaname = 'public'
    ORDER BY tablename;
    """;
var tables = new List<string>();
await using (var reader = await listCmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
        tables.Add(reader.GetString(0));
}

Console.WriteLine($"Found {tables.Count} tables: {string.Join(", ", tables)}");

if (tables.Count > 0)
{
    var dropSql = $"DROP TABLE IF EXISTS {string.Join(", ", tables.Select(t => $"\"{t}\""))} CASCADE;";
    await using var dropCmd = conn.CreateCommand();
    dropCmd.CommandText = dropSql;
    await dropCmd.ExecuteNonQueryAsync();
    Console.WriteLine("All tables dropped.");
}
else
{
    Console.WriteLine("No tables to drop.");
}
