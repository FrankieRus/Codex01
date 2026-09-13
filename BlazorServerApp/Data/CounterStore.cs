using Microsoft.Data.Sqlite;

namespace BlazorServerApp.Data;

/// <summary>
/// A single counter value shared by all users, persisted in a SQLite file
/// under App_Data so it survives app restarts and redeploys.
/// </summary>
public class CounterStore
{
    private readonly string _connectionString;
    private readonly object _lock = new();

    public CounterStore(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _connectionString = $"Data Source={Path.Combine(dataDir, "counter.db")}";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Counter (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Value INTEGER NOT NULL
            );
            INSERT OR IGNORE INTO Counter (Id, Value) VALUES (1, 0);
            """;
        command.ExecuteNonQuery();
    }

    public int GetValue()
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Counter WHERE Id = 1";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }

    public int Increment()
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE Counter SET Value = Value + 1 WHERE Id = 1";
            update.ExecuteNonQuery();

            var select = connection.CreateCommand();
            select.Transaction = transaction;
            select.CommandText = "SELECT Value FROM Counter WHERE Id = 1";
            var value = Convert.ToInt32(select.ExecuteScalar());

            transaction.Commit();
            return value;
        }
    }
}
