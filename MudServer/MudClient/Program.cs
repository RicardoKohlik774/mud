using System.Net.Sockets;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding  = Encoding.UTF8;

// ── Argumenty: MudClient.exe [host] [port] ────────────────────────────────────
string host = args.Length > 0 ? args[0] : "localhost";
int    port = args.Length > 1 && int.TryParse(args[1], out var p) ? p : 4000;

PrintBanner(host, port);

using var client = new TcpClient();
try
{
    await client.ConnectAsync(host, port);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"✗ Nelze se připojit na {host}:{port} — {ex.Message}");
    Console.ResetColor();
    return;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("✓ Připojeno!  Odejdi příkazem: Ctrl+C\n");
Console.ResetColor();

using var stream = client.GetStream();
var reader = new StreamReader(stream, Encoding.UTF8);
var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

using var cts = new CancellationTokenSource();

// ── Task 1: server → konzole ──────────────────────────────────────────────────
var readTask = Task.Run(async () =>
{
    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cts.Token);
            if (line == null) break;
            PrintServerLine(line);
        }
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"\n[READ ERROR] {ex.Message}");
        Console.ResetColor();
    }
    finally { cts.Cancel(); }
});

// ── Task 2: konzole → server ──────────────────────────────────────────────────
var writeTask = Task.Run(async () =>
{
    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            // ReadLine je blokující – proto ho spustíme v thread-pool,
            // aby šel zrušit přes CancellationToken
            var line = await Task.Run(() =>
            {
                try { return Console.ReadLine(); }
                catch { return null; }
            }, cts.Token);

            if (line == null) break;
            await writer.WriteLineAsync(line.AsMemory(), cts.Token);
        }
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"\n[WRITE ERROR] {ex.Message}");
        Console.ResetColor();
    }
    finally { cts.Cancel(); }
});

// Počkáme dokud jeden z tasků neskončí
await Task.WhenAny(readTask, writeTask);
cts.Cancel();

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\nOdpojeno od serveru. Stiskni Enter pro ukončení.");
Console.ResetColor();
Console.ReadLine();

// ── Pomocné funkce ────────────────────────────────────────────────────────────

static void PrintBanner(string host, int port)
{
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine("╔══════════════════════════════════════╗");
    Console.WriteLine("║   Zapomenuté podzemí — MUD klient    ║");
    Console.WriteLine("╚══════════════════════════════════════╝");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  Server: {host}:{port}");
    Console.ResetColor();
    Console.WriteLine();
}

static void PrintServerLine(string line)
{
    // Zbarvení podle obsahu řádku pro lepší čitelnost
    if (line.StartsWith("╔") || line.StartsWith("╚") || line.StartsWith("║"))
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
    }
    else if (line.StartsWith("══") || line.StartsWith("──"))
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
    }
    else if (line.StartsWith("✓"))
    {
        Console.ForegroundColor = ConsoleColor.Green;
    }
    else if (line.StartsWith("✗") || line.StartsWith("☠"))
    {
        Console.ForegroundColor = ConsoleColor.Red;
    }
    else if (line.StartsWith("⚠"))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
    }
    else if (line.StartsWith("⚔") || line.StartsWith("[KŘIK]"))
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
    }
    else if (line.StartsWith("[SERVER]"))
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
    }
    else if (line.StartsWith("  Tu:") || line.StartsWith("  Ty:"))
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
    }
    else if (line.StartsWith("HP:") || line.Contains("HP:"))
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
    }
    else if (line.StartsWith("> "))
    {
        // Prompt – zobrazíme bez newline (přepíše se vstupem)
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(line + " ");
        Console.ResetColor();
        return;
    }
    else if (line.StartsWith("🏆") || line.Contains("porazil Draka"))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
    }
    else if (line.StartsWith("┌") || line.StartsWith("│") || line.StartsWith("└"))
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
    }
    else
    {
        Console.ResetColor();
    }

    Console.WriteLine(line);
    Console.ResetColor();
}