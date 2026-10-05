using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CoreRCON;
using Microsoft.AspNetCore.SignalR;
using MinecraftServerManager.Hubs;

namespace MinecraftServerManager.Services;

public interface IMinecraftProcessService
{
    bool IsRunning {get; set; }
    Task<bool> StartServer();
    Task<string> SendCommand(string command, string? ip, long rconPort, string rconPassword);
    IEnumerable<string> GetOnlinePlayers();
    event Action<string>? OutputReceived;

    IEnumerable<string> GetRecentLogs();
}

public class MinecraftProcessService(IHubContext<ConsoleHub> hubContext, IConfiguration config) : IMinecraftProcessService
{
    public bool IsRunning {get; set; } = false;
    public event Action<string>? OutputReceived;
    private readonly IHubContext<ConsoleHub> _hubContext = hubContext;
    private readonly IConfiguration _config = config;
    private string _currentStatus = "Stopped";

    private readonly ConcurrentDictionary<string, byte> _onlinePlayers = new();
    private static readonly int LogsToStore = 500;

    // server dir and default ip come from appsettings, the frontend can override the ip per request
    private string ServerDir => _config["Minecraft:ServerDir"] ?? throw new InvalidOperationException("Minecraft:ServerDir is not set in appsettings");
    private string DefaultIp => _config["Minecraft:ServerIp"] ?? "127.0.0.1";

    public async Task<bool> StartServer()
    {
        
        if (IsRunning) 
        {
            UpdateStatus("Running");
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        { // windows untested
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c start \"Minecraft Server\" java -Xmx4G -Xms2G -jar server.jar nogui",
                WorkingDirectory = ServerDir,
                UseShellExecute = true
            });
        }
        else
        {
            var psi = new ProcessStartInfo
            {
                FileName = "setsid",
                Arguments = "xterm -e \"java -Xmx4G -Xms2G -jar server.jar nogui\"",
                WorkingDirectory = ServerDir,
                UseShellExecute = false,
                CreateNoWindow = true // todo: how to hide the xterm window, this field does nothing
            };

            Process.Start(psi);
        }
        Debug.WriteLine($"Process started successfully.");
        IsRunning = true;
        return true;
    }

    public IEnumerable<string> GetRecentLogs()
    {
        var logFilePath = $"{ServerDir}/logs/latest.log";
        var fileLogs = File.Exists(logFilePath) ? File.ReadLines(logFilePath) : []; // everything in log file

        var recentLogs = fileLogs.Skip(Math.Max(0, fileLogs.Count() - LogsToStore)); // skip everything but latest X lines

        return recentLogs;
    }
    

    public IEnumerable<string> GetOnlinePlayers() => _onlinePlayers.Keys;

   public async Task<string> SendCommand(string command, string? ip, long rconPort, string rconPassword)
    {
        if (rconPort < 1 || rconPort > 65535)
            throw new ArgumentOutOfRangeException(nameof(rconPort), $"RCON port {rconPort} is out of range");

        var target = string.IsNullOrWhiteSpace(ip) ? DefaultIp : ip.Trim();
        Console.WriteLine($"Sending command {command} to RCON {target}:{rconPort}");

        // coreRCON wants an IPAddress, dns names get resolved first
        var address = IPAddress.TryParse(target, out var parsed)
            ? parsed
            : (await Dns.GetHostAddressesAsync(target))[0];

        using var rcon = new RCON(address, (ushort)rconPort, rconPassword);
        await rcon.ConnectAsync();
        return await rcon.SendCommandAsync(command);
    }

    private void OnProcessExited(object? s, EventArgs e)
    {
        _onlinePlayers.Clear();
        UpdateStatus("Stopped");

        Task.Run(async() =>
        {
            await _hubContext.Clients.All.SendAsync("ReceiveLogLine", "[SYSTEM] Process has exited.");
            await _hubContext.Clients.All.SendAsync("PlayerListUpdated", Array.Empty<string>());
        });
    }

    private void UpdateStatus(string status)
    {
        _currentStatus = status;
        Task.Run(async () =>
        {
            await _hubContext.Clients.All.SendAsync("StatusChanged", status);
        });
    }
}
