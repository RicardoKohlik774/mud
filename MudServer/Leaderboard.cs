using System.Text.Json;

namespace MudServer;

/// <summary>
/// Spravuje žebříček hráčů – načítá ze souboru a ukládá zpět.
/// Thread-safe přes SemaphoreSlim.
/// </summary>
public class Leaderboard
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _sem = new(1, 1);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public Leaderboard(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<List<LeaderboardEntry>> GetAllAsync()
    {
        await _sem.WaitAsync();
        try
        {
            if (!File.Exists(_filePath)) return new();
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<LeaderboardEntry>>(json, JsonOpts) ?? new();
        }
        finally { _sem.Release(); }
    }

    public async Task AddEntryAsync(string playerName, long playTimeSeconds)
    {
        await _sem.WaitAsync();
        try
        {
            List<LeaderboardEntry> entries = new();

            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath);
                entries = JsonSerializer.Deserialize<List<LeaderboardEntry>>(json, JsonOpts) ?? new();
            }

            entries.Add(new LeaderboardEntry
            {
                PlayerName = playerName,
                CompletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                PlayTimeSeconds = playTimeSeconds
            });

            // Seřadit podle času (kratší = lepší)
            entries = entries.OrderBy(e => e.PlayTimeSeconds).ToList();

            await File.WriteAllTextAsync(_filePath,
                JsonSerializer.Serialize(entries, JsonOpts));
        }
        finally { _sem.Release(); }
    }

    public async Task<string> FormatTopAsync(int count = 10)
    {
        var entries = await GetAllAsync();
        if (entries.Count == 0)
            return "  Žebříček je zatím prázdný.";

        var lines = new List<string>();
        lines.Add("┌─ Nejlepší hráči (čas dokončení) ────────────────┐");

        for (int i = 0; i < Math.Min(count, entries.Count); i++)
        {
            var e = entries[i];
            var time = FormatTime(e.PlayTimeSeconds);
            lines.Add($"│ {i + 1,2}. {e.PlayerName,-20} {time,10}   {e.CompletedAt,-19} │");
        }

        lines.Add("└──────────────────────────────────────────────────┘");
        return string.Join("\n", lines);
    }

    private static string FormatTime(long seconds)
    {
        if (seconds < 60) return $"{seconds}s";
        if (seconds < 3600) return $"{seconds / 60}m {seconds % 60}s";
        return $"{seconds / 3600}h {seconds % 3600 / 60}m";
    }
}
