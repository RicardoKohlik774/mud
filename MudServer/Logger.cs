namespace MudServer;

public class Logger
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Logger(string filePath)
    {
        _filePath = filePath;
    }

    public async Task LogAsync(string message, string level = "INFO")
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var line = $"[{timestamp}] [{level}] {message}";

        Console.WriteLine(line);

        await _semaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_filePath, line + Environment.NewLine);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task LogErrorAsync(string message) => LogAsync(message, "ERROR");
    public Task LogWarnAsync(string message)  => LogAsync(message, "WARN");
}
