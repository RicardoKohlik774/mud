namespace MudServer;

public class CommandDispatcher
{
    private readonly ClientSession _s;
    private readonly Server _server;
    private readonly Logger _logger;
    private readonly GameWorld _world;
    private readonly Leaderboard _leaderboard;
    private readonly ServerConfig _config;
    private readonly Random _rng = new();

    private readonly Dictionary<string, Func<string, Task>> _handlers;

    public CommandDispatcher(ClientSession session, Server server, Logger logger,
        GameWorld world, Leaderboard leaderboard, ServerConfig config)
    {
        _s = session; _server = server; _logger = logger;
        _world = world; _leaderboard = leaderboard; _config = config;

        _handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["pomoc"]      = _ => HelpAsync(),
            ["prozkoumej"] = _ => LookAsync(),
            ["jdi"]        = GoAsync,
            ["vezmi"]      = TakeAsync,
            ["odloz"]      = DropAsync,
            ["inventar"]   = _ => InventoryAsync(),
            ["mluv"]       = TalkAsync,
            ["rekni"]      = SayAsync,
            ["krik"]       = ShoutAsync,
            ["utok"]       = AttackAsync,
            ["pouzij"]     = UseAsync,
            ["stav"]       = _ => StatusAsync(),
            ["zebricek"]   = _ => LeaderboardAsync(),
        };
    }

    public async Task DispatchAsync(string input)
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0];
        var args = parts.Length > 1 ? parts[1].Trim() : "";

        if (_handlers.TryGetValue(cmd, out var handler))
            await handler(args);
        else
            await _s.SendAsync($"Neznámý příkaz '{cmd}'. Napiš 'pomoc'.");
    }

    private async Task HelpAsync()
    {
        await _s.SendAsync("""
            ┌─── Příkazy ──────────────────────────────────────────┐
            │ prozkoumej              Popis aktuální místnosti     │
            │ jdi <směr>              Pohyb (sever/jih/vychod...)  │
            │ vezmi <předmět>         Vzít předmět                 │
            │ odloz <předmět>         Odložit předmět              │
            │ inventar                Zobraz inventář              │
            │ stav                    Zobraz HP a efekty           │
            │ mluv <jméno>            Promluv s NPC                │
            │ utok <jméno>            Zaútoč na NPC                │
            │ pouzij <předmět>        Použij předmět               │
            │ rekni <zpráva>          Zpráva hráčům v místnosti    │
            │ krik <zpráva>           Zpráva všem hráčům           │
            │ zebricek                Nejlepší hráči               │
            │ pomoc                   Tato nápověda                │
            └──────────────────────────────────────────────────────┘
            """);
    }

    private async Task LookAsync()
    {
        var others = _server.GetSessionsInRoom(_s.CurrentRoomId)
            .Where(x => x != _s).Select(x => x.PlayerName!);
        var desc = _world.DescribeRoom(_s.CurrentRoomId, others);

        await _s.SendAsync($"\n═══ {desc.Name} ═══");
        await _s.SendAsync(desc.Description);
        await _s.SendAsync($"Východy:  {(desc.Exits.Count > 0 ? string.Join(", ", desc.Exits) : "žádné")}");
        await _s.SendAsync($"Předměty: {(desc.Items.Count > 0 ? string.Join(", ", desc.Items) : "nic")}");
        await _s.SendAsync($"NPC:      {(desc.Npcs.Count > 0 ? string.Join(", ", desc.Npcs) : "nikdo")}");
        await _s.SendAsync($"Hráči:    {(desc.OtherPlayers.Count > 0 ? string.Join(", ", desc.OtherPlayers) : "nikdo další")}");
    }

    private async Task GoAsync(string dir)
    {
        if (string.IsNullOrEmpty(dir)) { await _s.SendAsync("Použití: jdi <směr>"); return; }

        if (!_world.TryMove(_s.CurrentRoomId, dir, _s.Inventory, out var toRoom, out var reason))
        {
            await _s.SendAsync($"⚠ {reason}");
            return;
        }

        var oldRoom = _s.CurrentRoomId;
        _s.CurrentRoomId = toRoom;

        await _server.BroadcastToRoomAsync(oldRoom, $"[{_s.PlayerName} odchází na {dir}.]", _s);
        await _server.BroadcastToRoomAsync(toRoom,  $"[{_s.PlayerName} vstupuje do místnosti.]", _s);

        await LookAsync();
    }

    private async Task TakeAsync(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) { await _s.SendAsync("Použití: vezmi <předmět>"); return; }

        if (_s.Inventory.Count >= _config.MaxInventorySize)
        { await _s.SendAsync("Inventář je plný!"); return; }

        if (!_world.TakeItem(_s.CurrentRoomId, itemName, out var itemId))
        { await _s.SendAsync($"Předmět '{itemName}' tu není."); return; }

        _s.Inventory.Add(itemId!);
        var def = _world.GetItem(itemId!);
        await _s.SendAsync($"Vzal jsi: {def?.Name ?? itemId}.");
        if (def != null) await _s.SendAsync($"  {def.Description}");
    }

    private async Task DropAsync(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) { await _s.SendAsync("Použití: odloz <předmět>"); return; }

        var found = _s.Inventory.FirstOrDefault(id =>
        {
            var def = _world.GetItem(id);
            return def?.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) == true
                || id.Equals(itemName, StringComparison.OrdinalIgnoreCase);
        });

        if (found == null) { await _s.SendAsync($"Nemáš '{itemName}'."); return; }

        _s.Inventory.Remove(found);
        _world.DropItem(_s.CurrentRoomId, found);
        await _s.SendAsync($"Odložil jsi: {_world.GetItemName(found)}.");
    }

    private async Task InventoryAsync()
    {
        await _s.SendAsync("── Inventář ──────────────────────────────");
        if (_s.Inventory.Count == 0)
            await _s.SendAsync("  (prázdný)");
        else
            foreach (var id in _s.Inventory)
            {
                var def = _world.GetItem(id);
                await _s.SendAsync($"  • {def?.Name ?? id}");
            }
        await _s.SendAsync($"Kapacita: {_s.Inventory.Count}/{_config.MaxInventorySize}");
        await _s.SendAsync("──────────────────────────────────────────");
    }

    private async Task StatusAsync()
    {
        await _s.SendAsync($"── Stav: {_s.PlayerName} ─────────────────────");
        await _s.SendAsync($"HP:     {_s.CurrentHp}/{_s.MaxHp}");
        await _s.SendAsync($"Útok:   {_s.GetTotalAttack()}");
        await _s.SendAsync($"Obrana: {_s.GetTotalDefense()}");
        if (_s.ActiveEffects.Count > 0)
        {
            await _s.SendAsync("Efekty:");
            foreach (var e in _s.ActiveEffects)
                await _s.SendAsync($"  • {e.Type} ({e.Value}) — {e.RemainingTurns} tahů");
        }
        await _s.SendAsync("──────────────────────────────────────────");
    }

    private async Task TalkAsync(string name)
    {
        if (string.IsNullOrEmpty(name)) { await _s.SendAsync("Použití: mluv <jméno NPC>"); return; }

        var npc = _world.FindNpcInRoom(_s.CurrentRoomId, name);
        if (npc == null) { await _s.SendAsync($"Žádné NPC jménem '{name}' tu není."); return; }

        await _s.SendAsync($"{npc.Def.Name}: \"{npc.Def.Dialog}\"");
    }

    private async Task SayAsync(string msg)
    {
        if (string.IsNullOrEmpty(msg)) { await _s.SendAsync("Použití: rekni <zpráva>"); return; }
        var others = _server.GetSessionsInRoom(_s.CurrentRoomId).Where(x => x != _s).ToList();
        if (others.Count == 0) { await _s.SendAsync("Nikdo tě neslyší..."); }
        else foreach (var o in others) await o.SendAsync($"[{_s.PlayerName} říká]: {msg}");
        await _s.SendAsync($"Říkáš: {msg}");
    }

    private async Task ShoutAsync(string msg)
    {
        if (string.IsNullOrEmpty(msg)) { await _s.SendAsync("Použití: krik <zpráva>"); return; }
        await _server.BroadcastAsync($"[KŘIK] {_s.PlayerName}: {msg}", _s);
        await _s.SendAsync($"Křičíš: {msg}");
    }

    private async Task AttackAsync(string name)
    {
        if (string.IsNullOrEmpty(name)) { await _s.SendAsync("Použití: utok <jméno NPC>"); return; }

        var npc = _world.FindNpcInRoom(_s.CurrentRoomId, name);
        if (npc == null) { await _s.SendAsync($"Žádné živé NPC '{name}' tu není."); return; }

        await _s.SendAsync($"\n⚔ Bojuješ s {npc.Def.Name}! (HP nepřítele: {npc.CurrentHp}/{npc.Def.MaxHp})");

        int playerAtk = Math.Max(1, _s.GetTotalAttack() - npc.Def.Defense + _rng.Next(-2, 3));
        npc.CurrentHp -= playerAtk;
        await _s.SendAsync($"  Způsobil jsi {playerAtk} poškození. HP nepřítele: {Math.Max(0, npc.CurrentHp)}/{npc.Def.MaxHp}");

        if (!npc.IsAlive)
        {
            await _s.SendAsync($"✓ Porazil jsi {npc.Def.Name}!");

            if (npc.Def.Loot.Count > 0)
            {
                _world.DropLoot(_s.CurrentRoomId, npc.Def.Loot);
                var lootNames = npc.Def.Loot.Select(id => _world.GetItemName(id));
                await _s.SendAsync($"  Nepřítel upustil: {string.Join(", ", lootNames)}");
            }

            if (npc.Def.IsFinalBoss)
                await HandleVictoryAsync();

            return;
        }

        int npcAtk = Math.Max(1, npc.Def.Attack - _s.GetTotalDefense() + _rng.Next(-2, 3));
        _s.CurrentHp -= npcAtk;
        await _s.SendAsync($"  {npc.Def.Name} útočí a způsobuje {npcAtk} poškození. Tvoje HP: {_s.CurrentHp}/{_s.MaxHp}");

        if (_s.CurrentHp <= 0)
            await _s.HandleDeathAsync();
    }

    private async Task UseAsync(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) { await _s.SendAsync("Použití: pouzij <předmět>"); return; }

        var found = _s.Inventory.FirstOrDefault(id =>
        {
            var def = _world.GetItem(id);
            return def?.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) == true
                || id.Equals(itemName, StringComparison.OrdinalIgnoreCase);
        });

        if (found == null) { await _s.SendAsync($"Nemáš '{itemName}'."); return; }

        var item = _world.GetItem(found);
        if (item?.Effect == null) { await _s.SendAsync($"{item?.Name ?? found} nemá žádný použitelný efekt."); return; }

        switch (item.Effect.Type)
        {
            case "heal":
                _s.CurrentHp = Math.Min(_s.MaxHp, _s.CurrentHp + item.Effect.Value);
                await _s.SendAsync($"💚 Použil jsi {item.Name}. HP: {_s.CurrentHp}/{_s.MaxHp} (+{item.Effect.Value})");
                _s.Inventory.Remove(found);
                break;

            case "poison":
                _s.ActiveEffects.Add(new ActiveEffect { Type = "poison", Value = item.Effect.Value, RemainingTurns = item.Effect.Duration });
                await _s.SendAsync($"☠ Použil jsi {item.Name}. Jed bude působit {item.Effect.Duration} tahů.");
                _s.Inventory.Remove(found);
                break;

            case "buff_attack":
                _s.ActiveEffects.RemoveAll(e => e.Type == "buff_attack");
                _s.ActiveEffects.Add(new ActiveEffect { Type = "buff_attack", Value = item.Effect.Value, RemainingTurns = item.Effect.Duration });
                await _s.SendAsync($"⚔ Použil jsi {item.Name}. Útok +{item.Effect.Value} po {item.Effect.Duration} tahů.");
                _s.Inventory.Remove(found);
                break;

            case "buff_defense":
                _s.ActiveEffects.RemoveAll(e => e.Type == "buff_defense");
                _s.ActiveEffects.Add(new ActiveEffect { Type = "buff_defense", Value = item.Effect.Value, RemainingTurns = item.Effect.Duration });
                await _s.SendAsync($"🛡 Použil jsi {item.Name}. Obrana +{item.Effect.Value} po {item.Effect.Duration} tahů.");
                _s.Inventory.Remove(found);
                break;

            case "light":
                await _s.SendAsync($"🕯 {item.Name} svítí jasným světlem po {item.Effect.Duration} tahů.");
                _s.Inventory.Remove(found);
                break;

            default:
                await _s.SendAsync($"Použil jsi {item.Name}, ale nic se nestalo.");
                break;
        }
    }

    private async Task HandleVictoryAsync()
    {
        _s.GameCompleted = true;
        var elapsed = (long)(DateTime.Now - _s.LoginTime).TotalSeconds;
        await _leaderboard.AddEntryAsync(_s.PlayerName!, elapsed);
        await _s.SaveAsync();

        await _s.SendAsync("");
        await _s.SendAsync("🏆═══════════════════════════════════════🏆");
        await _s.SendAsync($"   GRATULUJEME, {_s.PlayerName}!");
        await _s.SendAsync("   Porazil jsi Draka Záhuby a zachránil");
        await _s.SendAsync("   Zapomenuté podzemí!");
        await _s.SendAsync($"   Čas: {elapsed / 60}m {elapsed % 60}s");
        await _s.SendAsync("🏆═══════════════════════════════════════🏆");
        await _s.SendAsync("");
        await _server.BroadcastAsync($"[SERVER] 🏆 {_s.PlayerName} dokončil hru za {elapsed/60}m {elapsed%60}s!", _s);
    }

    private async Task LeaderboardAsync()
    {
        await _s.SendAsync(await _leaderboard.FormatTopAsync());
    }
}
