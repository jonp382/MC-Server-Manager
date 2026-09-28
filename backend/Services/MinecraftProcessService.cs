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
    Task<bool> StopServer();
    Task<string> SendCommand(string command);
    IEnumerable<string> GetOnlinePlayers();
    event Action<string>? OutputReceived;

    IEnumerable<string> GetRecentLogs();
}

public class MinecraftProcessService(IHubContext<ConsoleHub> hubContext) : IMinecraftProcessService
{
    public bool IsRunning {get; set; } = false;
    public event Action<string>? OutputReceived;
    private readonly IHubContext<ConsoleHub> _hubContext = hubContext;
    private string _currentStatus = "Stopped";

    private readonly ConcurrentDictionary<string, byte> _onlinePlayers = new();
    private static readonly int LogsToStore = 500;

    // todo: make these controllable by the server manager instead
    private const string ServerIp = "127.0.0.1";
    private const ushort RconPort = 25575;
    private const string RconPassword = "test_password";
    private const string ServerDir = @"/home/jonny/Games/Minecraft Servers/Test Server";

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

   public async Task<string> SendCommand(string command)
    {
        using var rcon = new RCON(IPAddress.Parse(ServerIp), RconPort, RconPassword);
        await rcon.ConnectAsync();
        return await rcon.SendCommandAsync(command);
    }

    public async Task<bool> StopServer()
    {
        if (IsRunning)
        {
            await SendCommand("stop");
            UpdateStatus("Stopped");
            IsRunning = false;
            return true;
        }

        return false;
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
