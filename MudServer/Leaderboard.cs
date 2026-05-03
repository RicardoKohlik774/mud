using System.Text.Json;

namespace MudServer;

public class Leaderboard
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _sem = new(1, 1);
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public Leaderboard(string filePath) { _filePath = filePath; }

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
            entries = entries.OrderBy(e => e.PlayTimeSeconds).ToList();
            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(entries, JsonOpts));
        }
        finally { _sem.Release(); }
    }

    public async Task<string> FormatTopAsync(int count = 10)
    {
        if (!File.Exists(_filePath)) return "  Žebříček je zatím prázdný.";
        await _sem.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var entries = JsonSerializer.Deserialize<List<LeaderboardEntry>>(json, JsonOpts) ?? new();
            if (entries.Count == 0) return "  Žebříček je zatím prázdný.";

            var lines = new List<string>
            {
                "┌─ Nejlepší hráči ─────────────────────────────────┐"
            };
            for (int i = 0; i < Math.Min(count, entries.Count); i++)
            {
                var e = entries[i];
                var t = e.PlayTimeSeconds < 60 ? $"{e.PlayTimeSeconds}s"
                      : e.PlayTimeSeconds < 3600 ? $"{e.PlayTimeSeconds/60}m {e.PlayTimeSeconds%60}s"
                      : $"{e.PlayTimeSeconds/3600}h {e.PlayTimeSeconds%3600/60}m";
                lines.Add($"│ {i+1,2}. {e.PlayerName,-18} {t,8}   {e.CompletedAt} │");
            }
            lines.Add("└──────────────────────────────────────────────────┘");
            return string.Join("\n", lines);
        }
        finally { _sem.Release(); }
    }
}
