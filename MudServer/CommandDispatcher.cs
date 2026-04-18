namespace MudServer;


public class CommandDispatcher
{
    private readonly ClientSession _session;
    private readonly Server _server;
    private readonly Logger _logger;

   
    private readonly Dictionary<string, Func<string, Task>> _handlers;

    public CommandDispatcher(ClientSession session, Server server, Logger logger)
    {
        _session = session;
        _server = server;
        _logger = logger;

        _handlers = new Dictionary<string, Func<string, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            ["pomoc"]      = _ => HandlePomocAsync(),
            ["prozkoumej"] = _ => HandleProzkumejAsync(),
            ["jdi"]        = HandleJdiAsync,
            ["vezmi"]      = HandleVezmiAsync,
            ["odloz"]      = HandleOdlozAsync,
            ["inventar"]   = _ => HandleInventarAsync(),
            ["mluv"]       = HandleMluvAsync,
            ["rekni"]      = HandleRekniAsync,
            ["krik"]       = HandleKrikAsync,
        };
    }

    public async Task DispatchAsync(string input)
    {
        
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1].Trim() : "";

        if (_handlers.TryGetValue(command, out var handler))
        {
            await handler(args);
        }
        else
        {
            await _session.SendAsync($"Neznámý příkaz '{command}'. Napiš 'pomoc' pro nápovědu.");
        }
    }

    // ─────────────────────────────────────────────
    // HANDLERY
    // ─────────────────────────────────────────────

    private async Task HandlePomocAsync()
    {
        await _session.SendAsync("""
            ┌───  Dostupné příkazy ─────────────────────────────┐
            │ prozkoumej           - Zobraz popis místnosti     │
            │ jdi <směr>          - Pohyb (sever/jih/vychod...) │
            │ vezmi <předmět>     - Vezmi předmět do inventáře  │
            │ odloz <předmět>     - Odlož předmět z inventáře   │
            │ inventar            - Zobraz obsah inventáře      │
            │ mluv <jméno>        - Promluv s NPC               │
            │ rekni <zpráva>      - Zpráva hráčům v místnosti   │
            │ krik <zpráva>       - Zpráva všem připojeným      │
            │ pomoc               - Tato nápověda               │
            └───────────────────────────────────────────────────┘
            """);
    }

    private async Task HandleProzkumejAsync()
    {
        // TODO: Az bude WorldLoader, nacte skutecnou mistnost
        // Zatim placeholder pro testovani spojeni
        await _session.SendAsync($"""

            ═══ Vstupní síň ═══
            Vlhká kamenná síň s pochodněmi na zdech. Odněkud táhne chladný vzduch.

            Východy:  sever, východ
            Předměty: rezavý meč, svíčka
            NPC:      Starý strážce
            Hráči:    {GetOtherPlayersString()}
            """);
    }

    private async Task HandleJdiAsync(string args)
    {
        if (string.IsNullOrEmpty(args))
        {
            await _session.SendAsync("Použití: jdi <směr>  (např. jdi sever)");
            return;
        }

        // TODO: Az bude GameWorld, overit exits a presunout hrace
        await _session.SendAsync($"[TODO] Jdeš směrem '{args}'... (herní svět se teprve načítá)");
    }

    private async Task HandleVezmiAsync(string args)
    {
        if (string.IsNullOrEmpty(args))
        {
            await _session.SendAsync("Použití: vezmi <předmět>");
            return;
        }

        // TODO: Az bude inventar a herní svět
        await _session.SendAsync($"[TODO] Bereš '{args}'... (inventář se teprve implementuje)");
    }

    private async Task HandleOdlozAsync(string args)
    {
        if (string.IsNullOrEmpty(args))
        {
            await _session.SendAsync("Použití: odloz <předmět>");
            return;
        }

        await _session.SendAsync($"[TODO] Odkládáš '{args}'...");
    }

    private async Task HandleInventarAsync()
    {
        // TODO: Zobrazit skutecny inventar hrace
        await _session.SendAsync("""
            ── Inventář ──────────────────
            (prázdný)
            Kapacita: 0 / 10
            ──────────────────────────────
            """);
    }

    private async Task HandleMluvAsync(string args)
    {
        if (string.IsNullOrEmpty(args))
        {
            await _session.SendAsync("Použití: mluv <jméno NPC>");
            return;
        }

        // TODO: Lookup NPC podle jmena v aktualni mistnosti
        await _session.SendAsync($"[TODO] Mluvíš s '{args}'... (NPC systém se teprve implementuje)");
    }

    private async Task HandleRekniAsync(string args)
    {
        if (string.IsNullOrEmpty(args))
        {
            await _session.SendAsync("Použití: rekni <zpráva>");
            return;
        }

        var message = $"[{_session.CurrentRoomId}] {_session.PlayerName} říká: {args}";
        var others = _server.GetSessionsInRoom(_session.CurrentRoomId);

        if (others.Count == 0)
        {
            await _session.SendAsync("Nikdo tě neslyší...");
            return;
        }

        foreach (var other in others)
            await other.SendAsync(message);

        await _session.SendAsync($"Říkáš: {args}");
    }

    private async Task HandleKrikAsync(string args)
    {
        if (string.IsNullOrEmpty(args))
        {
            await _session.SendAsync("Použití: krik <zpráva>");
            return;
        }

        var message = $"[KŘIK] {_session.PlayerName}: {args}";
        await _server.BroadcastAsync(message, exclude: _session);
        await _session.SendAsync($"Křičíš: {args}");
        await _logger.LogAsync($"KRIK od '{_session.PlayerName}': {args}");
    }

   

    // Pomocny metody
  
    private string GetOtherPlayersString()
    {
        var others = _server.GetSessionsInRoom(_session.CurrentRoomId)
            .Where(s => s != _session && s.PlayerName != null)
            .Select(s => s.PlayerName!)
            .ToList();

        return others.Count > 0 ? string.Join(", ", others) : "nikdo další";
    }
}
