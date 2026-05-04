Add-Type -Path "$PSScriptRoot\src\BioStack.Api\bin\Debug\net10.0\Npgsql.dll"

$connStr = "Host=biostackmcpg20260407e2.postgres.database.azure.com;Port=5432;Database=postgres;Username=biostackadmin;Password=Bi0St@ckR0ckz2026!;SSL Mode=Require;Trust Server Certificate=true"

$conn = [Npgsql.NpgsqlConnection]::new($connStr)
$conn.Open()
Write-Host "Connected: $($conn.State)"

$sql = @"
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260422125251_RecoverBillingTierEnforcement', '10.0.0')
ON CONFLICT DO NOTHING;
"@

$cmd = $conn.CreateCommand()
$cmd.CommandText = $sql
$rows = $cmd.ExecuteNonQuery()
Write-Host "Rows affected: $rows"
$conn.Close()
Write-Host "Migration stamped successfully"
