using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MudServer;

public class PlayerPersistence
{
    private readonly string _dir;
    private readonly SemaphoreSlim _sem = new(1, 1);
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public PlayerPersistence(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(dir);
    }

    private string FilePath(string name) =>
        Path.Combine(_dir, name.ToLower().Replace(" ", "_") + ".json");

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    public async Task<PlayerData?> LoadAsync(string name)
    {
        var path = FilePath(name);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<PlayerData>(json, JsonOpts);
    }

    public async Task SaveAsync(PlayerData data)
    {
        await _sem.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOpts);
            await File.WriteAllTextAsync(FilePath(data.Name), json);
        }
        finally { _sem.Release(); }
    }

    public bool PlayerExists(string name) => File.Exists(FilePath(name));
}
