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
    string GetServerStatus();
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
    private static readonly Regex JoinRegex = new(@"\b([A-Za-z0-9_]{2,16})\s+joined the game\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex LeaveRegex = new(@"\b([A-Za-z0-9_]{2,16})\s+left the game\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Queue<string> _logs = new();
    private static readonly int LogsToStore = 500;

    // todo: make these controllable by the server manager instead
    private const string ServerIp = "127.0.0.1";
    private const ushort RconPort = 25575;
    private const ushort QueryPort = 25565;
    private const string RconPassword = "test_password";

    public async Task<bool> StartServer()
    {
        string ServerDir = @"/home/jonny/Games/Minecraft Servers/Test Server";
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

    private void OnOutputDataReceived(object s, DataReceivedEventArgs e)
    {
        if(string.IsNullOrEmpty(e.Data)) return;

        Task.Run(async() =>
        {

            if(_logs.Count > LogsToStore)
            {
                _logs.Dequeue();
            }
            _logs.Enqueue(e.Data);

            await _hubContext.Clients.All.SendAsync("ReceiveLogLine", e.Data);

            var joinMatch = JoinRegex.Match(e.Data);
            if (joinMatch.Success)
            {
                string username = joinMatch.Groups[1].Value;
                Console.WriteLine($"[DEBUG] Detected player join: {username}");

                if(_onlinePlayers.TryAdd(username, 0))
                {
                    await _hubContext.Clients.All.SendAsync("PlayerJoined", username);
                }
                return;
            }

            var leaveMatch = LeaveRegex.Match(e.Data);
            if (leaveMatch.Success)
            {
                string username = leaveMatch.Groups[1].Value;
                Console.WriteLine($"[DEBUG] Detected player leave: {username}");

                if (_onlinePlayers.TryRemove(username, out _))
                {
                    await _hubContext.Clients.All.SendAsync("PlayerLeft", username);
                }
                return;
            }
        });
    }

    public IEnumerable<string> GetRecentLogs()
    {
        return _logs;
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

    public string GetServerStatus() => _currentStatus;
}
