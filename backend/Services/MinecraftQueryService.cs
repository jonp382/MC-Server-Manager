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

    public async Task<ServerStatusResult> QueryServerAsync()
    {
        string host = _config["Minecraft:ServerIp"] ?? "127.0.0.1";
        
        // RECOMMENDED: Switch to TCP Server List Ping (25565) to bypass UDP headaches.
        // If you strictly want UDP Query, use your query port (25566).
        int port = int.Parse(_config["Minecraft:ServerPort"] ?? "25565"); 

        var result = new ServerStatusResult { IsOnline = false };

        try
        {
            // MineStat handles the connections, timeouts, and multi-packet data streams automatically
            var ms = new MineStat(host, (ushort)port);

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