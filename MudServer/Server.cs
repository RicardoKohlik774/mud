using System.Net;
using System.Net.Sockets;

namespace MudServer;


public class Server
{
    private readonly int _port;
    private readonly Logger _logger;
    private readonly List<ClientSession> _sessions = new();
    private readonly object _sessionsLock = new();

    public Server(int port, Logger logger)
    {
        _port = port;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        await _logger.LogAsync($"Server nasloucha na portu {_port}. Cekam na hrace...");

        while (true)
        {
           
            TcpClient tcpClient = await listener.AcceptTcpClientAsync();

        
            var session = new ClientSession(tcpClient, _logger, this);

            lock (_sessionsLock)
                _sessions.Add(session);

            _ = Task.Run(() => RunSessionAsync(session));
        }
    }

    private async Task RunSessionAsync(ClientSession session)
    {
        try
        {
            await session.RunAsync();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Neocekavana chyba v session: {ex.Message}");
        }
        finally
        {
            lock (_sessionsLock)
                _sessions.Remove(session);

            await _logger.LogAsync($"Session [{session.PlayerName ?? "neznamy"}] ukoncena. Aktivnich hracu: {_sessions.Count}");
        }
    }

  
    public async Task BroadcastAsync(string message, ClientSession? exclude = null)
    {
        List<ClientSession> snapshot;
        lock (_sessionsLock)
            snapshot = new List<ClientSession>(_sessions);

        foreach (var session in snapshot)
        {
            if (session != exclude)
                await session.SendAsync(message);
        }
    }

   
    public List<ClientSession> GetSessionsInRoom(string roomId)
    {
        lock (_sessionsLock)
            return _sessions.Where(s => s.CurrentRoomId == roomId && s.PlayerName != null).ToList();
    }
}
