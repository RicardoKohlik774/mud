using System.Net;
using System.Net.Sockets;

namespace MudServer;

public class Server
{
    private readonly int _port;
    private readonly Logger _logger;
    private readonly GameWorld _world;
    private readonly Leaderboard _leaderboard;
    private readonly PlayerPersistence _persistence;
    private readonly ServerConfig _config;
    private readonly List<ClientSession> _sessions = new();
    private readonly object _sessionsLock = new();

    public Server(int port, Logger logger, GameWorld world, Leaderboard leaderboard,
        PlayerPersistence persistence, ServerConfig config)
    {
        _port = port; _logger = logger; _world = world;
        _leaderboard = leaderboard; _persistence = persistence; _config = config;
    }

    public async Task StartAsync()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        await _logger.LogAsync($"Server nasloucha na portu {_port}.");

        while (true)
        {
            var tcpClient = await listener.AcceptTcpClientAsync();
            var session = new ClientSession(tcpClient, _logger, this, _world,
                _leaderboard, _persistence, _config);
            lock (_sessionsLock) _sessions.Add(session);
            _ = Task.Run(() => RunSessionAsync(session));
        }
    }

    private async Task RunSessionAsync(ClientSession session)
    {
        try   { await session.RunAsync(); }
        catch (Exception ex) { await _logger.LogErrorAsync($"Session error: {ex.Message}"); }
        finally
        {
            lock (_sessionsLock) _sessions.Remove(session);
            await _logger.LogAsync($"Session [{session.PlayerName ?? "neznamy"}] ukoncena. Online: {_sessions.Count}");
        }
    }

    public async Task BroadcastAsync(string message, ClientSession? exclude = null)
    {
        List<ClientSession> snap;
        lock (_sessionsLock) snap = new(_sessions);
        foreach (var s in snap)
            if (s != exclude) await s.SendAsync(message);
    }

    public async Task BroadcastToRoomAsync(string roomId, string message, ClientSession? exclude = null)
    {
        List<ClientSession> snap;
        lock (_sessionsLock) snap = new(_sessions);
        foreach (var s in snap)
            if (s != exclude && s.CurrentRoomId == roomId && s.PlayerName != null)
                await s.SendAsync(message);
    }

    public List<ClientSession> GetSessionsInRoom(string roomId)
    {
        lock (_sessionsLock)
            return _sessions.Where(s => s.CurrentRoomId == roomId && s.PlayerName != null).ToList();
    }

    public List<ClientSession> GetAllSessions()
    {
        lock (_sessionsLock) return new(_sessions);
    }
}
