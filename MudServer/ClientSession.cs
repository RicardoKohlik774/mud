using System.Net.Sockets;
using System.Text;

namespace MudServer;


public class ClientSession
{
    private readonly TcpClient _tcpClient;
    private readonly Logger _logger;
    private readonly Server _server;
    private readonly CommandDispatcher _dispatcher;

    private StreamWriter? _writer;

    
    public string? PlayerName { get; set; }
    public string CurrentRoomId { get; set; } = "vstupni_sin";

    public ClientSession(TcpClient tcpClient, Logger logger, Server server)
    {
        _tcpClient = tcpClient;
        _logger = logger;
        _server = server;
        _dispatcher = new CommandDispatcher(this, server, logger);
    }

    public async Task RunAsync()
    {
        var remoteEndpoint = _tcpClient.Client.RemoteEndPoint?.ToString() ?? "neznama adresa";
        await _logger.LogAsync($"Nove pripojeni z {remoteEndpoint}");

        try
        {
            using var stream = _tcpClient.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

          
            await SendAsync("╔══════════════════════════════╗");
            await SendAsync("║  Vítej v Zapomenutém podzemí ║");
            await SendAsync("╚══════════════════════════════╝");
            await SendAsync("");
            await SendAsync("Zadej své jméno: ");

            string? name = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(name))
            {
                await SendAsync("Neplatné jméno. Odpojuji.");
                return;
            }

            PlayerName = name.Trim();
            await _logger.LogAsync($"Hrac '{PlayerName}' se pripojil z {remoteEndpoint}");

            await SendAsync($"\nVítej, {PlayerName}! Napiš 'pomoc' pro seznam příkazů.\n");

        
            await _dispatcher.DispatchAsync("prozkoumej");

         
            while (true)
            {
                await SendAsync("\n> ");
                string? input = await reader.ReadLineAsync();

             
                if (input == null) break;

                input = input.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                await _logger.LogAsync($"[{PlayerName}] prikaz: {input}");
                await _dispatcher.DispatchAsync(input);
            }
        }
        catch (IOException)
        {
            
            await _logger.LogWarnAsync($"Hrac '{PlayerName ?? "neznamy"}' se neocekavane odpojil.");
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Chyba v session hrace '{PlayerName}': {ex.Message}");
        }
        finally
        {
            _tcpClient.Close();
            if (PlayerName != null)
                await _logger.LogAsync($"Hrac '{PlayerName}' se odpojil.");
        }
    }

  
    public async Task SendAsync(string message)
    {
        if (_writer == null) return;
        try
        {
            await _writer.WriteLineAsync(message);
        }
        catch (IOException)
        {
          
        }
    }
}
