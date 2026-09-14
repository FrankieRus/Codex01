using Microsoft.Data.Sqlite;

namespace BlazorServerApp.Data;

/// <summary>
/// One of a fixed set of visual themes the app can be switched to.
/// </summary>
public record ThemeOption(string Id, string Name, string SwatchFrom, string SwatchTo);

/// <summary>
/// The app-wide theme choice, persisted in SQLite so it survives restarts and
/// is shared by every visitor. Raises <see cref="OnThemeChanged"/> so every
/// connected page can switch live.
/// </summary>
public class ThemeStore
{
    public static readonly IReadOnlyList<ThemeOption> Themes = new List<ThemeOption>
    {
        new("classic", "Classic", "#052767", "#3a0647"),
        new("aurora", "Aurora", "#0f2027", "#2ec4b6"),
        new("sunset", "Sunset", "#ff512f", "#dd2476"),
        new("ocean", "Ocean", "#005c97", "#363795"),
        new("galaxy", "Galaxy", "#0f0c29", "#ff00cc"),
        new("forest", "Forest", "#134e5e", "#71b280"),
    };

    private const string DefaultThemeId = "classic";

    private readonly string _connectionString;
    private readonly object _lock = new();

    public event Action<string>? OnThemeChanged;

    public ThemeStore(IWebHostEnvironment env)
    {
        _connectionString = AppDatabase.GetConnectionString(env);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createTable = connection.CreateCommand();
        createTable.CommandText = """
            CREATE TABLE IF NOT EXISTS AppSetting (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
            """;
        createTable.ExecuteNonQuery();

        var insertDefault = connection.CreateCommand();
        insertDefault.CommandText = "INSERT OR IGNORE INTO AppSetting (Key, Value) VALUES ('Theme', @defaultTheme);";
        insertDefault.Parameters.AddWithValue("@defaultTheme", DefaultThemeId);
        insertDefault.ExecuteNonQuery();
    }

    public string GetCurrentThemeId()
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM AppSetting WHERE Key = 'Theme'";
            var value = command.ExecuteScalar() as string;
            return IsValidTheme(value) ? value! : DefaultThemeId;
        }
    }

    public void SetTheme(string themeId)
    {
        if (!IsValidTheme(themeId))
        {
            return;
        }

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO AppSetting (Key, Value) VALUES ('Theme', @theme)
                ON CONFLICT(Key) DO UPDATE SET Value = @theme;
                """;
            command.Parameters.AddWithValue("@theme", themeId);
            command.ExecuteNonQuery();
        }

        OnThemeChanged?.Invoke(themeId);
    }

    private static bool IsValidTheme(string? themeId) =>
        themeId is not null && Themes.Any(t => t.Id == themeId);
}
