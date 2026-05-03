using System.Text.Json;
using MudServer;

ServerConfig config;
string configPath = "config.json";

if (File.Exists(configPath))
{
    var json = await File.ReadAllTextAsync(configPath);
    config = JsonSerializer.Deserialize<ServerConfig>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? new ServerConfig();
}
else
{
    Console.WriteLine("[WARN] config.json nenalezen, pouzivam vychozi nastaveni.");
    config = new ServerConfig();
}

var logger = new Logger(config.LogFile);
await logger.LogAsync("=== Server se spousti ===");

var world = await GameWorld.LoadAsync(config.DataDir);
await logger.LogAsync("Herni svet nacten.");

var leaderboard = new Leaderboard(Path.Combine(config.DataDir, "leaderboard.json"));
var persistence = new PlayerPersistence("players");

var server = new Server(config.Port, logger, world, leaderboard, persistence, config);
await server.StartAsync();
