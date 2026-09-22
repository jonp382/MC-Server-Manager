using System.Diagnostics;
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
    event Action<string>? OutputReceived;
}

public class MinecraftProcessService(IHubContext<ConsoleHub> hubContext) : IMinecraftProcessService
{
    private Process? _process;
    public bool IsRunning => _process != null && !_process.HasExited;
    public event Action<string>? OutputReceived;
    private readonly IHubContext<ConsoleHub> _hubContext = hubContext;
    private string _currentStatus = "Stopped";

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

        _process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data)) OutputReceived?.Invoke(args.Data);
        };

        _process.Exited += OnProcessExited;

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        UpdateStatus("Running");

        return true;
    }

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
        UpdateStatus("Stopped");

        Task.Run(async() =>
        {
            await _hubContext.Clients.All.SendAsync("ReceiveLogLine", "[SYSTEM] Process has exited.");
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
