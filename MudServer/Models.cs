using System.Text.Json.Serialization;

namespace MudServer;

public class Room
{
    [JsonPropertyName("id")]    public string Id          { get; set; } = "";
    [JsonPropertyName("name")]  public string Name        { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("exits")] public Dictionary<string, string> Exits { get; set; } = new();
    [JsonPropertyName("items")] public List<string> Items  { get; set; } = new();
    [JsonPropertyName("npcs")]  public List<string> Npcs   { get; set; } = new();
    [JsonPropertyName("locked")] public bool Locked        { get; set; } = false;
    [JsonPropertyName("lockKey")] public string? LockKey   { get; set; }
}

public class ItemEffect
{
    [JsonPropertyName("type")]     public string Type     { get; set; } = "";
    [JsonPropertyName("value")]    public int Value       { get; set; } = 0;
    [JsonPropertyName("duration")] public int Duration    { get; set; } = 0;
}

public class ItemDefinition
{
    [JsonPropertyName("id")]           public string Id            { get; set; } = "";
    [JsonPropertyName("name")]         public string Name          { get; set; } = "";
    [JsonPropertyName("description")]  public string Description   { get; set; } = "";
    [JsonPropertyName("type")]         public string Type          { get; set; } = "misc";
    [JsonPropertyName("attackBonus")]  public int AttackBonus      { get; set; } = 0;
    [JsonPropertyName("defenseBonus")] public int DefenseBonus     { get; set; } = 0;
    [JsonPropertyName("effect")]       public ItemEffect? Effect   { get; set; }
}

public class NpcDefinition
{
    [JsonPropertyName("id")]          public string Id          { get; set; } = "";
    [JsonPropertyName("name")]        public string Name        { get; set; } = "";
    [JsonPropertyName("dialog")]      public string Dialog      { get; set; } = "";
    [JsonPropertyName("isHostile")]   public bool IsHostile     { get; set; } = false;
    [JsonPropertyName("isFinalBoss")] public bool IsFinalBoss   { get; set; } = false;
    [JsonPropertyName("maxHp")]       public int MaxHp          { get; set; } = 20;
    [JsonPropertyName("attack")]      public int Attack         { get; set; } = 5;
    [JsonPropertyName("defense")]     public int Defense        { get; set; } = 1;
    [JsonPropertyName("loot")]        public List<string> Loot  { get; set; } = new();
    [JsonPropertyName("xpReward")]    public int XpReward       { get; set; } = 0;
}

public class NpcInstance
{
    public NpcDefinition Def       { get; }
    public int CurrentHp           { get; set; }
    public bool IsAlive            => CurrentHp > 0;

    public NpcInstance(NpcDefinition def)
    {
        Def = def;
        CurrentHp = def.MaxHp;
    }
}

public class ActiveEffect
{
    public string Type             { get; set; } = "";
    public int Value               { get; set; }
    public int RemainingTurns      { get; set; }
}

public class LeaderboardEntry
{
    [JsonPropertyName("playerName")]      public string PlayerName      { get; set; } = "";
    [JsonPropertyName("completedAt")]     public string CompletedAt     { get; set; } = "";
    [JsonPropertyName("playTimeSeconds")] public long PlayTimeSeconds   { get; set; }
}

public class PlayerData
{
    [JsonPropertyName("name")]          public string Name          { get; set; } = "";
    [JsonPropertyName("passwordHash")]  public string PasswordHash  { get; set; } = "";
    [JsonPropertyName("currentRoomId")] public string CurrentRoomId { get; set; } = "vstupni_sin";
    [JsonPropertyName("inventory")]     public List<string> Inventory { get; set; } = new();
    [JsonPropertyName("currentHp")]     public int CurrentHp        { get; set; } = 100;
    [JsonPropertyName("gameCompleted")] public bool GameCompleted   { get; set; } = false;
}
