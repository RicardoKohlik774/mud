using System.Text.Json;
using MudServer;


ServerConfig config;
if (File.Exists("config.json"))
{
    var json = File.ReadAllText("config.json");
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
await logger.LogAsync($"Server se spousti na portu {config.Port}");

var server = new Server(config.Port, logger);
await server.StartAsync();
