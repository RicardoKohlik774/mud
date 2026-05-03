namespace MudServer;

public class ServerConfig
{
    public int Port { get; set; } = 4000;
    public string LogFile { get; set; } = "log.txt";
    public int MaxInventorySize { get; set; } = 10;
    public int PlayerMaxHp { get; set; } = 100;
    public int PlayerBaseAttack { get; set; } = 10;
    public string DataDir { get; set; } = "data";
}
