namespace BlazorServerApp.Data;

/// <summary>
/// Shared SQLite file location for all app data (App_Data/counter.db).
/// </summary>
internal static class AppDatabase
{
    public static string GetConnectionString(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        return $"Data Source={Path.Combine(dataDir, "counter.db")}";
    }
}
