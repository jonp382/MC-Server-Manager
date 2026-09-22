using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using MinecraftServerManager.Hubs;

namespace MinecraftServerManager.Services;

public interface IMinecraftProcessService
{
    bool IsRunning { get; }
    Task<bool> StartServer();
    Task<bool> StopServer();
    Task<bool> SendCommand(string command);
    string GetServerStatus();
    IEnumerable<string> GetOnlinePlayers();
    event Action<string>? OutputReceived;
}

public class MinecraftProcessService(IHubContext<ConsoleHub> hubContext) : IMinecraftProcessService
{
    private Process? _process;
    public bool IsRunning => _process != null && !_process.HasExited;
    public event Action<string>? OutputReceived;
    private readonly IHubContext<ConsoleHub> _hubContext = hubContext;
    private string _currentStatus = "Stopped";

    private readonly ConcurrentDictionary<string, byte> _onlinePlayers = new();
    private static readonly Regex JoinRegex = new(@"\b([A-Za-z0-9_]{2,16})\s+joined the game\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex LeaveRegex = new(@"\b([A-Za-z0-9_]{2,16})\s+left the game\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<bool> StartServer()
    {
        if (IsRunning) 
        {
            UpdateStatus("Running");
            return false;
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = @"java",
                WorkingDirectory = @"/home/jonny/Games/Minecraft Servers/Test Server",
                Arguments = "-Xmx2G -Xms1G -jar server.jar nogui",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        UpdateStatus("Started");

        _process.OutputDataReceived += OnOutputDataReceived;

        _process.Exited += OnProcessExited;

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        UpdateStatus("Running");

        return true;
    }

    private void OnOutputDataReceived(object s, DataReceivedEventArgs e)
    {
        if(string.IsNullOrEmpty(e.Data)) return;

        Task.Run(async() =>
        {
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

    public IEnumerable<string> GetOnlinePlayers() => _onlinePlayers.Keys;

    public async Task<bool> SendCommand(string command)
    {
        if (IsRunning)
        {
            await _process!.StandardInput.WriteLineAsync(command);
            await _process.StandardInput.FlushAsync();

            return true;
        }
        return false;
    }

    public async Task<bool> StopServer()
    {
        if (IsRunning)
        {
            await SendCommand("stop");
            UpdateStatus("Stopped");
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
