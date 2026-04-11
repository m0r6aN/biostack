using System.Globalization;
using BioStack.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BioStack.Api;

public static class DatabaseSchemaBootstrapper
{
    public static string MakeSqliteCreateScriptIdempotent(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return string.Empty;
        }

        return script
            .Replace("CREATE UNIQUE INDEX ", "CREATE UNIQUE INDEX IF NOT EXISTS ")
            .Replace("CREATE INDEX ", "CREATE INDEX IF NOT EXISTS ")
            .Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ");
    }

    public static void BackfillMissingSqliteColumns(BioStackDbContext db)
    {
        using var connection = new SqliteConnection(db.Database.GetDbConnection().ConnectionString);
        connection.Open();

        foreach (var entityType in db.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                continue;
            }

            var tableIdentifier = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
            var existingColumns = GetExistingColumns(connection, tableName);
            if (existingColumns.Count == 0)
            {
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(tableIdentifier);
                if (string.IsNullOrWhiteSpace(columnName) || existingColumns.Contains(columnName))
                {
                    continue;
                }

                var sql = BuildAddColumnSql(tableName, columnName, property, tableIdentifier);
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
                existingColumns.Add(columnName);
            }
        }
    }

    private static HashSet<string> GetExistingColumns(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName.Replace("\"", "\"\"")}\")";
        using var reader = command.ExecuteReader();

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            columns.Add(reader.GetString(reader.GetOrdinal("name")));
        }

        return columns;
    }

    public static string BuildAddColumnSql(
        string tableName,
        string columnName,
        IProperty property,
        StoreObjectIdentifier tableIdentifier)
    {
        var storeType = property.GetColumnType(tableIdentifier)
            ?? property.GetRelationalTypeMapping().StoreType;
        var nullableClause = property.IsColumnNullable(tableIdentifier)
            ? "NULL"
            : $"NOT NULL DEFAULT {BuildDefaultLiteral(property)}";

        return $"ALTER TABLE \"{tableName.Replace("\"", "\"\"")}\" ADD COLUMN \"{columnName.Replace("\"", "\"\"")}\" {storeType} {nullableClause};";
    }

    private static string BuildDefaultLiteral(IProperty property)
    {
        var defaultValue = property.GetDefaultValue();
        if (defaultValue is not null)
        {
            return ToSqlLiteral(defaultValue);
        }

        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

        if (clrType == typeof(string))
        {
            return "''";
        }

        if (clrType == typeof(Guid))
        {
            return $"'{Guid.Empty}'";
        }

        if (clrType == typeof(DateTime))
        {
            return $"'{DateTime.UnixEpoch:O}'";
        }

        if (clrType == typeof(bool))
        {
            return "0";
        }

        if (clrType.IsEnum)
        {
            return "0";
        }

        if (clrType == typeof(int) || clrType == typeof(long) || clrType == typeof(short) ||
            clrType == typeof(byte) || clrType == typeof(decimal) || clrType == typeof(double) ||
            clrType == typeof(float))
        {
            return "0";
        }

        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(clrType) && clrType != typeof(byte[]))
        {
            return "''";
        }

        return "''";
    }

    private static string ToSqlLiteral(object value)
    {
        return value switch
        {
            string stringValue => $"'{stringValue.Replace("'", "''")}'",
            Guid guidValue => $"'{guidValue}'",
            DateTime dateTimeValue => $"'{dateTimeValue:O}'",
            bool boolValue => boolValue ? "1" : "0",
            Enum => Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "''",
            _ => $"'{value.ToString()?.Replace("'", "''") ?? string.Empty}'",
        };
    }
}