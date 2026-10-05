using MineStatLib;

namespace MinecraftServerManager.Services;

public class ServerStatusResult
{
    public bool IsOnline { get; set; }
    public string Motd { get; set; } = string.Empty;
    public string[] OnlinePlayers { get; set; } = [];
    public int MaxPlayers { get; set; }
    public string Version {get; set;} = string.Empty;
}

public class MinecraftQueryService
{
    private readonly IConfiguration _config;

    public MinecraftQueryService(IConfiguration config)
    {
        _config = config;
    }

    // ip and port are optional so the frontend can target any server, falls back to appsettings when not given
    public async Task<ServerStatusResult> QueryServerAsync(string? ip = null, int? port = null)
    {
        string host = string.IsNullOrWhiteSpace(ip) ? (_config["Minecraft:ServerIp"] ?? "127.0.0.1") : ip.Trim();

        // RECOMMENDED: Switch to TCP Server List Ping (25565) to bypass UDP headaches.
        // If you strictly want UDP Query, use your query port (25566).
        int qport = port ?? int.Parse(_config["Minecraft:QueryPort"] ?? "25565");

        var result = new ServerStatusResult { IsOnline = false };

        if (qport < 1 || qport > 65535)
        {
            Console.WriteLine($"[ERROR] [QueryService] Port {qport} is out of range");
            return result;
        }

        try
        {
            // MineStat handles the connections, timeouts, and multi-packet data streams automatically
            var ms = new MineStat(host, (ushort)qport);

            if (ms.ServerUp)
            {
                result.IsOnline = true;
                result.Motd = ms.Motd;
                result.OnlinePlayers = ms.PlayerList ?? [];
                result.MaxPlayers = int.Parse(ms.MaximumPlayers ?? "0");
                result.Version = ms.Version;
            }

            Console.WriteLine($"[INFO] [QueryService] MineStat Response: {result.IsOnline}, Players: {string.Join(", ", result.OnlinePlayers)}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] [QueryService] MineStat query failed: {ex.Message}");
            return result;
        }
    }
}