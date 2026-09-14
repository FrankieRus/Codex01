using Microsoft.Data.Sqlite;

namespace BlazorServerApp.Data;

public record TableInfo(string Name, int RowCount);

public record SqlResult(List<string>? Columns, List<object?[]>? Rows, int? RowsAffected, string? Error);

/// <summary>
/// Generic read/write access to the app's SQLite database (App_Data/counter.db)
/// for the built-in database viewer/maintenance page. Every table in this app
/// is a plain rowid table, so `rowid` is used as a universal row identifier
/// for editing and deleting without needing to know each table's own primary key.
/// </summary>
public class DatabaseAdminService
{
    private readonly string _connectionString;

    public DatabaseAdminService(IWebHostEnvironment env)
    {
        _connectionString = AppDatabase.GetConnectionString(env);
    }

    public List<TableInfo> ListTables()
    {
        using var connection = Open();

        var listCommand = connection.CreateCommand();
        listCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        var names = new List<string>();
        using (var reader = listCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                names.Add(reader.GetString(0));
            }
        }

        var tables = new List<TableInfo>();
        foreach (var name in names)
        {
            var countCommand = connection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM \"{name}\"";
            var count = Convert.ToInt32(countCommand.ExecuteScalar());
            tables.Add(new TableInfo(name, count));
        }
        return tables;
    }

    public (List<string> Columns, List<(long RowId, object?[] Values)> Rows) GetRows(string table, int limit = 200)
    {
        EnsureKnownTable(table);

        using var connection = Open();
        var columns = GetColumns(connection, table);
        var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));

        var command = connection.CreateCommand();
        command.CommandText = $"SELECT rowid, {columnList} FROM \"{table}\" ORDER BY rowid DESC LIMIT $limit";
        command.Parameters.AddWithValue("$limit", limit);

        var rows = new List<(long, object?[])>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var rowId = reader.GetInt64(0);
            var values = new object?[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                values[i] = reader.IsDBNull(i + 1) ? null : reader.GetValue(i + 1);
            }
            rows.Add((rowId, values));
        }
        return (columns, rows);
    }

    public void DeleteRow(string table, long rowId)
    {
        EnsureKnownTable(table);

        using var connection = Open();
        var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM \"{table}\" WHERE rowid = $rowid";
        command.Parameters.AddWithValue("$rowid", rowId);
        command.ExecuteNonQuery();
    }

    public void UpdateCell(string table, long rowId, string column, string? value)
    {
        EnsureKnownTable(table);
        var columns = ListColumns(table);
        if (!columns.Contains(column))
        {
            throw new ArgumentException($"Unknown column '{column}' on table '{table}'.");
        }

        using var connection = Open();
        var command = connection.CreateCommand();
        command.CommandText = $"UPDATE \"{table}\" SET \"{column}\" = $value WHERE rowid = $rowid";
        command.Parameters.AddWithValue("$value", (object?)value ?? DBNull.Value);
        command.Parameters.AddWithValue("$rowid", rowId);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Runs an arbitrary SQL statement for maintenance purposes (VACUUM, ad-hoc
    /// fixes, exploratory SELECTs, ...). Intentionally unrestricted: this page
    /// is meant as a developer/admin tool for the app's own database.
    /// </summary>
    public SqlResult RunQuery(string sql)
    {
        try
        {
            using var connection = Open();
            var command = connection.CreateCommand();
            command.CommandText = sql;

            var trimmed = sql.TrimStart();
            var isQuery = trimmed.StartsWith("select", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("pragma", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("explain", StringComparison.OrdinalIgnoreCase);

            if (isQuery)
            {
                using var reader = command.ExecuteReader();
                var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                var rows = new List<object?[]>();
                while (reader.Read())
                {
                    var values = new object?[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    rows.Add(values);
                }
                return new SqlResult(columns, rows, null, null);
            }

            var affected = command.ExecuteNonQuery();
            return new SqlResult(null, null, affected, null);
        }
        catch (SqliteException ex)
        {
            return new SqlResult(null, null, null, ex.Message);
        }
    }

    public List<string> ListColumns(string table)
    {
        EnsureKnownTable(table);
        using var connection = Open();
        return GetColumns(connection, table);
    }

    private static List<string> GetColumns(SqliteConnection connection, string table)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\")";
        var columns = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }
        return columns;
    }

    private void EnsureKnownTable(string table)
    {
        if (!ListTables().Any(t => t.Name == table))
        {
            throw new ArgumentException($"Unknown table '{table}'.");
        }
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
