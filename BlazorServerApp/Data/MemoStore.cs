using Microsoft.Data.Sqlite;

namespace BlazorServerApp.Data;

/// <summary>
/// A single free-text memo shared by all users, persisted alongside the
/// counter in App_Data/counter.db. Raises <see cref="OnMemoChanged"/> so
/// every connected page can update live as it's typed.
/// </summary>
public class MemoStore
{
    private readonly string _connectionString;
    private readonly object _lock = new();

    public event Action<string>? OnMemoChanged;

    public MemoStore(IWebHostEnvironment env)
    {
        _connectionString = AppDatabase.GetConnectionString(env);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Memo (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Text TEXT NOT NULL
            );
            INSERT OR IGNORE INTO Memo (Id, Text) VALUES (1, '');
            """;
        command.ExecuteNonQuery();
    }

    public string GetValue()
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Text FROM Memo WHERE Id = 1";
            return command.ExecuteScalar() as string ?? "";
        }
    }

    public void SetValue(string text)
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Memo SET Text = $text WHERE Id = 1";
            command.Parameters.AddWithValue("$text", text);
            command.ExecuteNonQuery();
        }

        OnMemoChanged?.Invoke(text);
    }
}
