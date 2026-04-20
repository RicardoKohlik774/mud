using System.Text.Json;

namespace MudServer;

/// <summary>
/// Sdílený herní svět – načítá data ze souborů a uchovává runtime stav
/// (předměty v místnostech, HP NPC). Thread-safe přes lock.
/// </summary>
public class GameWorld
{
    private readonly Dictionary<string, Room> _rooms = new();
    private readonly Dictionary<string, ItemDefinition> _items = new();
    private readonly Dictionary<string, NpcDefinition> _npcDefs = new();

    // Runtime: NPC instance per místnost (klíč = roomId)
    private readonly Dictionary<string, List<NpcInstance>> _roomNpcs = new();

    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Načítání ──────────────────────────────────────────────────────────────

    public static async Task<GameWorld> LoadAsync(string dataDir)
    {
        var world = new GameWorld();
        await world.LoadRoomsAsync(Path.Combine(dataDir, "rooms.json"));
        await world.LoadItemsAsync(Path.Combine(dataDir, "items.json"));
        await world.LoadNpcsAsync(Path.Combine(dataDir, "npcs.json"));
        world.SpawnNpcInstances();
        return world;
    }

    private async Task LoadRoomsAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var list = JsonSerializer.Deserialize<List<Room>>(json, JsonOpts) ?? new();
        foreach (var r in list)
            _rooms[r.Id] = r;
    }

    private async Task LoadItemsAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var list = JsonSerializer.Deserialize<List<ItemDefinition>>(json, JsonOpts) ?? new();
        foreach (var i in list)
            _items[i.Id] = i;
    }

    private async Task LoadNpcsAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var list = JsonSerializer.Deserialize<List<NpcDefinition>>(json, JsonOpts) ?? new();
        foreach (var n in list)
            _npcDefs[n.Id] = n;
    }

    private void SpawnNpcInstances()
    {
        foreach (var room in _rooms.Values)
        {
            var instances = new List<NpcInstance>();
            foreach (var npcId in room.Npcs)
            {
                if (_npcDefs.TryGetValue(npcId, out var def))
                    instances.Add(new NpcInstance(def));
            }
            _roomNpcs[room.Id] = instances;
        }
    }

    // ── Read-only přístup ──────────────────────────────────────────────────────

    public Room? GetRoom(string id)
    {
        lock (_lock) { _rooms.TryGetValue(id, out var r); return r; }
    }

    public ItemDefinition? GetItem(string id)
    {
        _items.TryGetValue(id, out var item);
        return item;
    }

    public string StartRoomId => "vstupni_sin";

    // ── NPC ───────────────────────────────────────────────────────────────────

    public List<NpcInstance> GetNpcsInRoom(string roomId)
    {
        lock (_lock)
        {
            _roomNpcs.TryGetValue(roomId, out var list);
            return list ?? new List<NpcInstance>();
        }
    }

    /// <summary>Najde živého NPC v místnosti podle jména (case-insensitive).</summary>
    public NpcInstance? FindNpcInRoom(string roomId, string name)
    {
        lock (_lock)
        {
            if (!_roomNpcs.TryGetValue(roomId, out var list)) return null;
            return list.FirstOrDefault(n =>
                n.IsAlive &&
                n.Def.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    // ── Pohyb ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Zkusí přesunout hráče daným směrem.
    /// Vrací true a cílové roomId, nebo false s důvodem.
    /// hasKey = hráč má platný klíč (zjistíme v ClientSession).
    /// </summary>
    public bool TryMove(string fromRoomId, string direction, bool hasKey,
        out string toRoomId, out string? failReason)
    {
        toRoomId = "";
        failReason = null;

        lock (_lock)
        {
            if (!_rooms.TryGetValue(fromRoomId, out var room))
            {
                failReason = "Aktuální místnost nenalezena.";
                return false;
            }

            if (!room.Exits.TryGetValue(direction.ToLower(), out var target))
            {
                failReason = $"Směr '{direction}' odtud nevede nikam.";
                return false;
            }

            if (!_rooms.TryGetValue(target, out var targetRoom))
            {
                failReason = "Cílová místnost neexistuje.";
                return false;
            }

            if (targetRoom.Locked)
            {
                if (hasKey)
                {
                    // Dveře se odemknou natrvalo
                    targetRoom.Locked = false;
                    toRoomId = target;
                    return true;
                }

                failReason = targetRoom.LockKey != null
                    ? $"Dveře jsou zamčené. Potřebuješ '{GetItemName(targetRoom.LockKey)}'."
                    : "Dveře jsou zamčené.";
                return false;
            }

            toRoomId = target;
            return true;
        }
    }

    // ── Předměty v místnostech ────────────────────────────────────────────────

    /// <summary>Vezme předmět z místnosti do inventáře. Hledá podle jména.</summary>
    public bool TakeItem(string roomId, string itemName, out string? itemId)
    {
        itemId = null;
        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return false;

            var found = room.Items.FirstOrDefault(id =>
            {
                _items.TryGetValue(id, out var def);
                return def?.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) == true
                    || id.Equals(itemName, StringComparison.OrdinalIgnoreCase);
            });

            if (found == null) return false;
            room.Items.Remove(found);
            itemId = found;
            return true;
        }
    }

    public void DropItem(string roomId, string itemId)
    {
        lock (_lock)
        {
            if (_rooms.TryGetValue(roomId, out var room))
                room.Items.Add(itemId);
        }
    }

    // ── Popis místnosti ───────────────────────────────────────────────────────

    public RoomDescription DescribeRoom(string roomId, IEnumerable<string> otherPlayerNames)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomId, out var room))
                return new RoomDescription { Name = "???" };

            var npcs = _roomNpcs.TryGetValue(roomId, out var npcList)
                ? npcList.Where(n => n.IsAlive).Select(n => n.Def.Name).ToList()
                : new List<string>();

            var items = room.Items
                .Select(id => _items.TryGetValue(id, out var def) ? def.Name : id)
                .ToList();

            return new RoomDescription
            {
                Name = room.Name,
                Description = room.Description,
                Exits = room.Exits.Keys.ToList(),
                Items = items,
                Npcs = npcs,
                OtherPlayers = otherPlayerNames.ToList()
            };
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    public string GetItemName(string itemId)
    {
        return _items.TryGetValue(itemId, out var def) ? def.Name : itemId;
    }

    public bool IsValidRoom(string roomId)
    {
        lock (_lock) { return _rooms.ContainsKey(roomId); }
    }
}

public class RoomDescription
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Exits { get; set; } = new();
    public List<string> Items { get; set; } = new();
    public List<string> Npcs { get; set; } = new();
    public List<string> OtherPlayers { get; set; } = new();
}
