using Microsoft.Data.Sqlite;

namespace BlazorServerApp.Data;

public record PdfRecord(int Id, string FileName, string StoredFileName, string Description, string UploadedAt);

/// <summary>
/// Metadata for uploaded PDFs (description + reference to the stored file),
/// persisted alongside the other app data in App_Data/counter.db. The actual
/// PDF bytes live under wwwroot/uploads/pdfs.
/// </summary>
public class PdfDocumentStore
{
    private readonly string _connectionString;
    private readonly object _lock = new();

    public PdfDocumentStore(IWebHostEnvironment env)
    {
        _connectionString = AppDatabase.GetConnectionString(env);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS PdfDocument (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FileName TEXT NOT NULL,
                StoredFileName TEXT NOT NULL,
                Description TEXT NOT NULL,
                UploadedAt TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public int Add(string fileName, string storedFileName, string description)
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO PdfDocument (FileName, StoredFileName, Description, UploadedAt)
                VALUES ($fileName, $storedFileName, $description, $uploadedAt);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$fileName", fileName);
            command.Parameters.AddWithValue("$storedFileName", storedFileName);
            command.Parameters.AddWithValue("$description", description);
            command.Parameters.AddWithValue("$uploadedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"));
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }

    public List<PdfRecord> Search(string? query)
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();

            if (string.IsNullOrWhiteSpace(query))
            {
                command.CommandText = "SELECT Id, FileName, StoredFileName, Description, UploadedAt FROM PdfDocument ORDER BY Id DESC";
            }
            else
            {
                command.CommandText = """
                    SELECT Id, FileName, StoredFileName, Description, UploadedAt FROM PdfDocument
                    WHERE Description LIKE $q OR FileName LIKE $q
                    ORDER BY Id DESC
                    """;
                command.Parameters.AddWithValue("$q", $"%{query}%");
            }

            var results = new List<PdfRecord>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new PdfRecord(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4)));
            }
            return results;
        }
    }
}
