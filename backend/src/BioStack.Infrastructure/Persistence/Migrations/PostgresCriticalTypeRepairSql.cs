namespace BioStack.Infrastructure.Persistence.Migrations;

using System.Text;

internal static class PostgresCriticalTypeRepairSql
{
    private static readonly (string Name, string Table, string Column, string PrincipalTable, string PrincipalColumn, string DeleteAction)[] ForeignKeys =
    [
        ("FK_PersonProfiles_AppUsers_OwnerId", "PersonProfiles", "OwnerId", "AppUsers", "Id", "SET NULL"),
        ("FK_AuthIdentities_AppUsers_UserId", "AuthIdentities", "UserId", "AppUsers", "Id", "CASCADE"),
        ("FK_AuthChallenges_AuthIdentities_IdentityId", "AuthChallenges", "IdentityId", "AuthIdentities", "Id", "CASCADE"),
        ("FK_Sessions_AppUsers_UserId", "Sessions", "UserId", "AppUsers", "Id", "CASCADE"),
        ("FK_Subscriptions_AppUsers_AppUserId", "Subscriptions", "AppUserId", "AppUsers", "Id", "CASCADE"),
    ];

    internal static string Build()
    {
        var repairColumns = CriticalPostgresSchemaContract.Columns
            .Where(column => column.ExpectedUdtNames.SetEquals(["uuid"])
                || column.ExpectedUdtNames.SetEquals(["timestamptz"])
                || column.ExpectedUdtNames.SetEquals(["bool"]))
            .ToArray();

        var sql = new StringBuilder();
        sql.AppendLine("DO $repair$");
        sql.AppendLine("DECLARE");
        sql.AppendLine("    column_spec record;");
        sql.AppendLine("    legacy_value record;");
        sql.AppendLine("    foreign_keys_dropped boolean := false;");
        sql.AppendLine("BEGIN");
        sql.AppendLine("    -- Validate every legacy value before taking constraints down or changing a type.");
        sql.AppendLine("    FOR column_spec IN");
        sql.AppendLine("        SELECT * FROM (VALUES");

        for (var index = 0; index < repairColumns.Length; index++)
        {
            var column = repairColumns[index];
            var targetType = column.ExpectedUdtNames.Single();
            sql.Append("            (")
                .Append(Literal(column.Table)).Append(", ")
                .Append(Literal(column.Column)).Append(", ")
                .Append(Literal(targetType)).Append(", ")
                .Append(column.IsNullable ? "true" : "false").Append(')')
                .AppendLine(index == repairColumns.Length - 1 ? string.Empty : ",");
        }

        sql.AppendLine("        ) AS specs(table_name, column_name, target_type, is_nullable)");
        sql.AppendLine("    LOOP");
        sql.AppendLine("        IF EXISTS (");
        sql.AppendLine("            SELECT 1 FROM information_schema.columns");
        sql.AppendLine("            WHERE table_schema = 'public'");
        sql.AppendLine("              AND table_name = column_spec.table_name");
        sql.AppendLine("              AND column_name = column_spec.column_name");
        sql.AppendLine("              AND udt_name <> column_spec.target_type");
        sql.AppendLine("        ) THEN");
        sql.AppendLine("            FOR legacy_value IN EXECUTE format(");
        sql.AppendLine("                'SELECT ctid::text AS row_locator, %1$I::text AS value FROM %2$I',");
        sql.AppendLine("                column_spec.column_name,");
        sql.AppendLine("                column_spec.table_name)");
        sql.AppendLine("            LOOP");
        sql.AppendLine("                IF legacy_value.value IS NULL THEN");
        sql.AppendLine("                    IF NOT column_spec.is_nullable THEN");
        sql.AppendLine("                        RAISE EXCEPTION 'BioStack schema repair preflight failed: %.% contains NULL at row %',");
        sql.AppendLine("                            column_spec.table_name, column_spec.column_name, legacy_value.row_locator;");
        sql.AppendLine("                    END IF;");
        sql.AppendLine("                    CONTINUE;");
        sql.AppendLine("                END IF;");
        sql.AppendLine();
        sql.AppendLine("                IF btrim(legacy_value.value) = '' THEN");
        sql.AppendLine("                    RAISE EXCEPTION 'BioStack schema repair preflight failed: %.% contains a blank value at row %',");
        sql.AppendLine("                        column_spec.table_name, column_spec.column_name, legacy_value.row_locator;");
        sql.AppendLine("                END IF;");
        sql.AppendLine();
        sql.AppendLine("                BEGIN");
        sql.AppendLine("                    CASE column_spec.target_type");
        sql.AppendLine("                        WHEN 'uuid' THEN");
        sql.AppendLine("                            PERFORM btrim(legacy_value.value)::uuid;");
        sql.AppendLine("                        WHEN 'timestamptz' THEN");
        sql.AppendLine("                            IF btrim(legacy_value.value) ~* '(Z|[+-][0-9]{2}(:?[0-9]{2})?)$' THEN");
        sql.AppendLine("                                PERFORM btrim(legacy_value.value)::timestamp with time zone;");
        sql.AppendLine("                            ELSE");
        sql.AppendLine("                                PERFORM btrim(legacy_value.value)::timestamp without time zone;");
        sql.AppendLine("                            END IF;");
        sql.AppendLine("                        WHEN 'bool' THEN");
        sql.AppendLine("                            IF lower(btrim(legacy_value.value)) NOT IN ('0', '1', 'false', 'true', 'f', 't') THEN");
        sql.AppendLine("                                RAISE EXCEPTION 'unrecognized boolean';");
        sql.AppendLine("                            END IF;");
        sql.AppendLine("                    END CASE;");
        sql.AppendLine("                EXCEPTION WHEN others THEN");
        sql.AppendLine("                    RAISE EXCEPTION 'BioStack schema repair preflight failed: %.% contains a malformed % value at row %',");
        sql.AppendLine("                        column_spec.table_name, column_spec.column_name, column_spec.target_type, legacy_value.row_locator;");
        sql.AppendLine("                END;");
        sql.AppendLine("            END LOOP;");
        sql.AppendLine("        END IF;");
        sql.AppendLine("    END LOOP;");
        sql.AppendLine();
        sql.AppendLine("    IF EXISTS (");
        sql.AppendLine("        SELECT 1 FROM information_schema.columns");
        sql.AppendLine("        WHERE table_schema = 'public'");
        sql.AppendLine("          AND udt_name <> 'uuid'");
        sql.AppendLine("          AND (table_name, column_name) IN (");

        var uuidColumns = repairColumns.Where(column => column.ExpectedUdtNames.Contains("uuid")).ToArray();
        for (var index = 0; index < uuidColumns.Length; index++)
        {
            var column = uuidColumns[index];
            sql.Append("              (").Append(Literal(column.Table)).Append(", ").Append(Literal(column.Column)).Append(')')
                .AppendLine(index == uuidColumns.Length - 1 ? string.Empty : ",");
        }

        sql.AppendLine("          )");
        sql.AppendLine("    ) THEN");
        foreach (var foreignKey in ForeignKeys)
        {
            sql.Append("        ALTER TABLE ").Append(Identifier(foreignKey.Table))
                .Append(" DROP CONSTRAINT IF EXISTS ").Append(Identifier(foreignKey.Name)).AppendLine(";");
        }
        sql.AppendLine("        foreign_keys_dropped := true;");
        sql.AppendLine("    END IF;");
        sql.AppendLine();

        foreach (var column in repairColumns)
        {
            AppendColumnRepair(sql, column);
        }

        sql.AppendLine("    IF foreign_keys_dropped THEN");
        foreach (var foreignKey in ForeignKeys)
        {
            sql.Append("        ALTER TABLE ").Append(Identifier(foreignKey.Table))
                .Append(" ADD CONSTRAINT ").Append(Identifier(foreignKey.Name))
                .Append(" FOREIGN KEY (").Append(Identifier(foreignKey.Column)).Append(") REFERENCES ")
                .Append(Identifier(foreignKey.PrincipalTable)).Append(" (")
                .Append(Identifier(foreignKey.PrincipalColumn)).Append(") ON DELETE ")
                .Append(foreignKey.DeleteAction).AppendLine(";");
        }
        sql.AppendLine("    END IF;");
        sql.AppendLine("END");
        sql.AppendLine("$repair$;");
        return sql.ToString();
    }

    private static void AppendColumnRepair(StringBuilder sql, CriticalPostgresColumn column)
    {
        var targetType = column.ExpectedUdtNames.Single();
        sql.AppendLine("    IF EXISTS (");
        sql.AppendLine("        SELECT 1 FROM information_schema.columns");
        sql.AppendLine("        WHERE table_schema = 'public'");
        sql.Append("          AND table_name = ").Append(Literal(column.Table)).AppendLine();
        sql.Append("          AND column_name = ").Append(Literal(column.Column)).AppendLine();
        sql.Append("          AND udt_name <> ").Append(Literal(targetType)).AppendLine();
        sql.AppendLine("    ) THEN");
        sql.Append("        ALTER TABLE ").Append(Identifier(column.Table))
            .Append(" ALTER COLUMN ").Append(Identifier(column.Column));

        switch (targetType)
        {
            case "uuid":
                sql.Append(" TYPE uuid USING btrim(").Append(Identifier(column.Column)).AppendLine("::text)::uuid;");
                break;
            case "timestamptz":
                sql.AppendLine(" TYPE timestamp with time zone USING CASE");
                sql.Append("            WHEN ").Append(Identifier(column.Column)).AppendLine(" IS NULL THEN NULL");
                sql.Append("            WHEN btrim(").Append(Identifier(column.Column)).AppendLine("::text) ~* '(Z|[+-][0-9]{2}(:?[0-9]{2})?)$'");
                sql.Append("                THEN btrim(").Append(Identifier(column.Column)).AppendLine("::text)::timestamp with time zone");
                sql.Append("            ELSE btrim(").Append(Identifier(column.Column)).AppendLine("::text)::timestamp without time zone AT TIME ZONE 'UTC'");
                sql.AppendLine("        END;");
                break;
            case "bool":
                sql.AppendLine(" TYPE boolean USING CASE");
                sql.Append("            WHEN lower(btrim(").Append(Identifier(column.Column)).AppendLine("::text)) IN ('1', 'true', 't') THEN TRUE");
                sql.Append("            WHEN lower(btrim(").Append(Identifier(column.Column)).AppendLine("::text)) IN ('0', 'false', 'f') THEN FALSE");
                sql.AppendLine("        END;");
                break;
            default:
                throw new InvalidOperationException($"Unsupported repair target type: {targetType}");
        }

        sql.AppendLine("    END IF;");
        sql.AppendLine();
    }

    private static string Identifier(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static string Literal(string value) => $"'{value.Replace("'", "''")}'";
}
