using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;

namespace RhinoImageStudio.Backend.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        await EnsureGenerationArchiveColumnsAsync(db);
        await EnsureJobProviderModelColumnAsync(db);
    }

    private static async Task EnsureGenerationArchiveColumnsAsync(AppDbContext db)
    {
        using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(Generations)";
        var existingColumns = new HashSet<string>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                existingColumns.Add(reader.GetString(1));
        }

        if (!existingColumns.Contains("IsArchived"))
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Generations ADD COLUMN IsArchived INTEGER NOT NULL DEFAULT 0");
        if (!existingColumns.Contains("ArchivedAt"))
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Generations ADD COLUMN ArchivedAt TEXT");
    }

    private static async Task EnsureJobProviderModelColumnAsync(AppDbContext db)
    {
        using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(Jobs)";
        var existingColumns = new HashSet<string>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                existingColumns.Add(reader.GetString(1));
        }

        if (!existingColumns.Contains("ProviderModelId"))
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Jobs ADD COLUMN ProviderModelId TEXT");
    }
}
