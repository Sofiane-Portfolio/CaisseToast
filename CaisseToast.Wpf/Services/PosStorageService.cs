using System.Text.Json;
using CaisseToast.Wpf.Models;
using Microsoft.Data.Sqlite;

namespace CaisseToast.Wpf.Services;

public interface IPosStorageService
{
    bool TryLoad(out PosSnapshot snapshot);
    void Save(PosSnapshot snapshot);
}

public sealed class PosStorageService : IPosStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private readonly string _dbPath;

    public PosStorageService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CaisseToast");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "pos.sqlite");
        EnsureSchema();
    }

    public bool TryLoad(out PosSnapshot snapshot)
    {
        snapshot = new PosSnapshot();
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT json FROM snapshots WHERE id = 1";
            var json = cmd.ExecuteScalar() as string;
            if (string.IsNullOrWhiteSpace(json)) return false;
            snapshot = JsonSerializer.Deserialize<PosSnapshot>(json, JsonOptions) ?? new PosSnapshot();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Save(PosSnapshot snapshot)
    {
        try
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO snapshots (id, json, updated_at)
                VALUES (1, $json, $updated)
                ON CONFLICT(id) DO UPDATE SET json = $json, updated_at = $updated
                """;
            cmd.Parameters.AddWithValue("$json", json);
            cmd.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O"));
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Persistence must not crash the POS session.
        }
    }

    private void EnsureSchema()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS snapshots (
                id INTEGER PRIMARY KEY,
                json TEXT NOT NULL,
                updated_at TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open() => new($"Data Source={_dbPath}");
}
