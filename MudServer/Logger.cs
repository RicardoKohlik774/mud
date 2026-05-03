namespace MudServer;

public class Logger
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Logger(string filePath) { _filePath = filePath; }

    public async Task LogAsync(string message, string level = "INFO")
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        Console.WriteLine(line);
        await _semaphore.WaitAsync();
        try   { await File.AppendAllTextAsync(_filePath, line + Environment.NewLine); }
        finally { _semaphore.Release(); }
    }

    public Task LogErrorAsync(string msg) => LogAsync(msg, "ERROR");
    public Task LogWarnAsync(string msg)  => LogAsync(msg, "WARN");
}
