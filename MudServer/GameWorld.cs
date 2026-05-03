using System.Text.Json;

namespace MudServer;

public class GameWorld
{
    private readonly Dictionary<string, Room> _rooms = new();
    private readonly Dictionary<string, ItemDefinition> _items = new();
    private readonly Dictionary<string, NpcDefinition> _npcDefs = new();
    private readonly Dictionary<string, List<NpcInstance>> _roomNpcs = new();
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public string StartRoomId => "vstupni_sin";

    public static async Task<GameWorld> LoadAsync(string dataDir)
    {
        var w = new GameWorld();
        await w.LoadRoomsAsync(Path.Combine(dataDir, "rooms.json"));
        await w.LoadItemsAsync(Path.Combine(dataDir, "items.json"));
        await w.LoadNpcsAsync(Path.Combine(dataDir, "npcs.json"));
        w.SpawnNpcs();
        return w;
    }

    private async Task LoadRoomsAsync(string path)
    {
        var list = JsonSerializer.Deserialize<List<Room>>(await File.ReadAllTextAsync(path), JsonOpts) ?? new();
        foreach (var r in list) _rooms[r.Id] = r;
    }

    private async Task LoadItemsAsync(string path)
    {
        var list = JsonSerializer.Deserialize<List<ItemDefinition>>(await File.ReadAllTextAsync(path), JsonOpts) ?? new();
        foreach (var i in list) _items[i.Id] = i;
    }

    private async Task LoadNpcsAsync(string path)
    {
        var list = JsonSerializer.Deserialize<List<NpcDefinition>>(await File.ReadAllTextAsync(path), JsonOpts) ?? new();
        foreach (var n in list) _npcDefs[n.Id] = n;
    }

    private void SpawnNpcs()
    {
        foreach (var room in _rooms.Values)
        {
            var instances = room.Npcs
                .Where(id => _npcDefs.ContainsKey(id))
                .Select(id => new NpcInstance(_npcDefs[id]))
                .ToList();
            _roomNpcs[room.Id] = instances;
        }
    }

    public Room? GetRoom(string id)
    {
        lock (_lock) { _rooms.TryGetValue(id, out var r); return r; }
    }

    public ItemDefinition? GetItem(string id)
    {
        _items.TryGetValue(id, out var item); return item;
    }

    public bool IsValidRoom(string id)
    {
        lock (_lock) { return _rooms.ContainsKey(id); }
    }

    public List<NpcInstance> GetNpcsInRoom(string roomId)
    {
        lock (_lock) { _roomNpcs.TryGetValue(roomId, out var list); return list ?? new(); }
    }

    public NpcInstance? FindNpcInRoom(string roomId, string name)
    {
        lock (_lock)
        {
            _roomNpcs.TryGetValue(roomId, out var list);
            return list?.FirstOrDefault(n => n.IsAlive &&
                n.Def.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool TryMove(string fromRoomId, string direction, List<string> inventory,
        out string toRoomId, out string? failReason)
    {
        toRoomId = ""; failReason = null;
        lock (_lock)
        {
            if (!_rooms.TryGetValue(fromRoomId, out var room))
            { failReason = "Aktuální místnost neexistuje."; return false; }

            if (!room.Exits.TryGetValue(direction.ToLower(), out var target))
            { failReason = $"Směr '{direction}' odtud nevede nikam."; return false; }

            if (!_rooms.TryGetValue(target, out var targetRoom))
            { failReason = "Cílová místnost neexistuje."; return false; }

            if (targetRoom.Locked)
            {
                bool hasKey = targetRoom.LockKey != null && inventory.Contains(targetRoom.LockKey);
                if (hasKey)
                {
                    targetRoom.Locked = false;
                }
                else
                {
                    failReason = targetRoom.LockKey != null
                        ? $"Dveře jsou zamčené. Potřebuješ '{GetItemName(targetRoom.LockKey)}'."
                        : "Dveře jsou zamčené.";
                    return false;
                }
            }

            toRoomId = target; return true;
        }
    }

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
        { if (_rooms.TryGetValue(roomId, out var room)) room.Items.Add(itemId); }
    }

    public void DropLoot(string roomId, List<string> loot)
    {
        lock (_lock)
        {
            if (_rooms.TryGetValue(roomId, out var room))
                room.Items.AddRange(loot);
        }
    }

    public RoomDescription DescribeRoom(string roomId, IEnumerable<string> otherPlayers)
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
                OtherPlayers = otherPlayers.ToList()
            };
        }
    }

    public string GetItemName(string? itemId)
    {
        if (itemId == null) return "?";
        return _items.TryGetValue(itemId, out var def) ? def.Name : itemId;
    }
}

public class RoomDescription
{
    public string Name             { get; set; } = "";
    public string Description      { get; set; } = "";
    public List<string> Exits      { get; set; } = new();
    public List<string> Items      { get; set; } = new();
    public List<string> Npcs       { get; set; } = new();
    public List<string> OtherPlayers { get; set; } = new();
}
