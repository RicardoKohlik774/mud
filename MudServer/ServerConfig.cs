namespace MudServer;

public class ServerConfig
{
    public int Port { get; set; } = 4000;
    public string LogFile { get; set; } = "log.txt";
    public int MaxInventorySize { get; set; } = 10;
}
