using System.Net.Sockets;
using System.Text;

namespace MudServer;

public class ClientSession
{
    private readonly TcpClient _tcpClient;
    private readonly Logger _logger;
    private readonly Server _server;
    private readonly GameWorld _world;
    private readonly Leaderboard _leaderboard;
    private readonly PlayerPersistence _persistence;
    private readonly ServerConfig _config;
    private CommandDispatcher? _dispatcher;
    private StreamWriter? _writer;

    public string? PlayerName      { get; set; }
    public string CurrentRoomId    { get; set; } = "vstupni_sin";
    public List<string> Inventory  { get; set; } = new();
    public int CurrentHp           { get; set; } = 100;
    public int MaxHp               { get; set; } = 100;
    public bool GameCompleted      { get; set; } = false;
    public List<ActiveEffect> ActiveEffects { get; set; } = new();
    public DateTime LoginTime      { get; private set; }

    public ClientSession(TcpClient tcpClient, Logger logger, Server server,
        GameWorld world, Leaderboard leaderboard, PlayerPersistence persistence,
        ServerConfig config)
    {
        _tcpClient = tcpClient; _logger = logger; _server = server;
        _world = world; _leaderboard = leaderboard; _persistence = persistence;
        _config = config;
        MaxHp = config.PlayerMaxHp;
        CurrentHp = MaxHp;
    }

    public async Task RunAsync()
    {
        var remote = _tcpClient.Client.RemoteEndPoint?.ToString() ?? "?";
        await _logger.LogAsync($"Nove pripojeni z {remote}");

        try
        {
            using var stream = _tcpClient.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            await SendAsync("╔══════════════════════════════╗");
            await SendAsync("║  Vítej v Zapomenutém podzemí ║");
            await SendAsync("╚══════════════════════════════╝");
            await SendAsync("");

            if (!await LoginAsync(reader)) return;

            _dispatcher = new CommandDispatcher(this, _server, _logger, _world, _leaderboard, _config);

            await SendAsync($"\nVítej, {PlayerName}! Napiš 'pomoc' pro seznam příkazů.");
            if (GameCompleted) await SendAsync("[Již jsi dokončil hru. Gratuluji znovu!]");
            await SendAsync("");
            await _dispatcher.DispatchAsync("prozkoumej");

            while (true)
            {
                await SendAsync("\n> ");
                string? input = await reader.ReadLineAsync();
                if (input == null) break;
                input = input.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                await _logger.LogAsync($"[{PlayerName}] {input}");

                await ProcessEffectsAsync();
                await _dispatcher.DispatchAsync(input);
            }
        }
        catch (IOException) { await _logger.LogWarnAsync($"Hrac '{PlayerName ?? "?"}' se neocekavane odpojil."); }
        catch (Exception ex) { await _logger.LogErrorAsync($"Session error '{PlayerName}': {ex.Message}"); }
        finally
        {
            _tcpClient.Close();
            if (PlayerName != null)
            {
                await SaveAsync();
                await _logger.LogAsync($"Hrac '{PlayerName}' se odpojil.");
            }
        }
    }

    private async Task<bool> LoginAsync(StreamReader reader)
    {
        await SendAsync("Zadej své jméno: ");
        string? name = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(name)) { await SendAsync("Neplatné jméno."); return false; }
        name = name.Trim();

        if (_persistence.PlayerExists(name))
        {
            await SendAsync("Zadej heslo: ");
            string? pw = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(pw)) { await SendAsync("Neplatné heslo."); return false; }

            var data = await _persistence.LoadAsync(name);
            if (data == null || data.PasswordHash != PlayerPersistence.HashPassword(pw))
            {
                await SendAsync("Špatné heslo. Odpojuji.");
                return false;
            }
            PlayerName = data.Name;
            CurrentRoomId = _world.IsValidRoom(data.CurrentRoomId) ? data.CurrentRoomId : _world.StartRoomId;
            Inventory = data.Inventory;
            CurrentHp = Math.Clamp(data.CurrentHp, 1, MaxHp);
            GameCompleted = data.GameCompleted;
            LoginTime = DateTime.Now;
            await _logger.LogAsync($"Hrac '{PlayerName}' se prihlasil.");
        }
        else
        {
            await SendAsync("Nový hráč! Zvol heslo: ");
            string? pw = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(pw)) { await SendAsync("Neplatné heslo."); return false; }

            PlayerName = name;
            LoginTime = DateTime.Now;
            await _persistence.SaveAsync(new PlayerData
            {
                Name = name,
                PasswordHash = PlayerPersistence.HashPassword(pw),
                CurrentRoomId = _world.StartRoomId,
                Inventory = new(),
                CurrentHp = MaxHp
            });
            await _logger.LogAsync($"Novy hrac '{PlayerName}' zaregistrovan.");
        }
        return true;
    }

    private async Task ProcessEffectsAsync()
    {
        var toRemove = new List<ActiveEffect>();
        foreach (var effect in ActiveEffects)
        {
            if (effect.Type == "poison")
            {
                CurrentHp -= effect.Value;
                await SendAsync($"☠ Jed tě poškozuje za {effect.Value} HP. (HP: {CurrentHp}/{MaxHp})");
                if (CurrentHp <= 0) { await HandleDeathAsync(); return; }
            }
            else if (effect.Type == "regen")
            {
                CurrentHp = Math.Min(MaxHp, CurrentHp + effect.Value);
                await SendAsync($"💚 Regenerace +{effect.Value} HP. (HP: {CurrentHp}/{MaxHp})");
            }
            effect.RemainingTurns--;
            if (effect.RemainingTurns <= 0) toRemove.Add(effect);
        }
        foreach (var e in toRemove) ActiveEffects.Remove(e);
    }

    public async Task HandleDeathAsync()
    {
        CurrentHp = MaxHp / 2;
        Inventory.Clear();
        CurrentRoomId = _world.StartRoomId;
        ActiveEffects.Clear();
        await SendAsync("☠ Zemřel jsi! Probouzíš se zpět ve vstupní síni bez vybavení...");
        await SendAsync($"HP obnoveno na {CurrentHp}/{MaxHp}.");
    }

    public int GetTotalAttack()
    {
        int bonus = Inventory.Sum(id =>
        {
            var item = _world.GetItem(id);
            return item?.AttackBonus ?? 0;
        });
        int buff = ActiveEffects.Where(e => e.Type == "buff_attack").Sum(e => e.Value);
        return _config.PlayerBaseAttack + bonus + buff;
    }

    public int GetTotalDefense()
    {
        int bonus = Inventory.Sum(id =>
        {
            var item = _world.GetItem(id);
            return item?.DefenseBonus ?? 0;
        });
        int buff = ActiveEffects.Where(e => e.Type == "buff_defense").Sum(e => e.Value);
        return bonus + buff;
    }

    public async Task SaveAsync()
    {
        if (PlayerName == null) return;
        await _persistence.SaveAsync(new PlayerData
        {
            Name = PlayerName,
            PasswordHash = (await _persistence.LoadAsync(PlayerName))?.PasswordHash ?? "",
            CurrentRoomId = CurrentRoomId,
            Inventory = Inventory,
            CurrentHp = CurrentHp,
            GameCompleted = GameCompleted
        });
    }

    public async Task SendAsync(string message)
    {
        if (_writer == null) return;
        try { await _writer.WriteLineAsync(message); }
        catch (IOException) { }
    }
}
