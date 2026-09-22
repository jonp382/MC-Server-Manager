using System.Diagnostics;

namespace MinecraftServerManager.Services;

public interface IMinecraftProcessService
{
    bool IsRunning { get; }
    void StartServer();
    void StopServer();
    void SendCommand(string command);
    event Action<string>? OutputReceived;
}

public class MinecraftProcessService : IMinecraftProcessService
{
    private Process? _process;
    public bool IsRunning => _process != null && !_process.HasExited;
    public event Action<string>? OutputReceived;

    public void StartServer()
    {
        if (IsRunning) return;

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

        _process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data)) OutputReceived?.Invoke(args.Data);
        };

        _process.Start();
        _process.BeginOutputReadLine();
    }

    public void SendCommand(string command)
    {
        if (IsRunning)
        {
            _process!.StandardInput.WriteLine(command);
            _process.StandardInput.Flush();
        }
    }

    public void StopServer()
    {
        if (IsRunning)
        {
            SendCommand("stop");
        }
    }
}
