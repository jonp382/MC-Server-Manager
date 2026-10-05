using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using CoreRCON;
using MinecraftServerManager.Models;

namespace MinecraftServerManager.Services;

// the server runs regardless of this app, so starting one means spawning a detached
// process that outlives the backend. no std-in/std-out, the log watcher reads latest.log.
public class ServerProcessService
{
    private const int LogsToStore = 500;

    // per-instance flag, set on start, cleared on stop. the query service is the real source of online truth.
    private readonly ConcurrentDictionary<string, bool> _running = new();

    public bool IsRunning(ServerInstance instance) => _running.TryGetValue(instance.Id, out var running) && running;

    public Task<bool> StartServer(ServerInstance instance)
    {
        if (IsRunning(instance)) return Task.FromResult(false);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        { // windows untested. cmd /c start spawns an independent process
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c start \"Minecraft Server\" java -Xmx4G -Xms2G -jar server.jar nogui",
                WorkingDirectory = instance.ServerDir,
                UseShellExecute = true
            });
        }
        else
        {
            // setsid detaches from our process group, sh -c backgrounds java and redirects
            // its output to a file so nothing is tied to the backend
            var log = Path.Combine(instance.ServerDir, "manager-start.log");
            var psi = new ProcessStartInfo
            {
                FileName = "setsid",
                Arguments = $"sh -c \"java -Xmx4G -Xms2G -jar server.jar nogui >> '{log}' 2>&1 < /dev/null &\"",
                WorkingDirectory = instance.ServerDir,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(psi);
        }

        Debug.WriteLine($"Process started successfully for {instance.Name}.");
        _running[instance.Id] = true;
        return Task.FromResult(true);
    }

    public async Task<string> SendCommand(ServerInstance instance, string command)
    {
        if (instance.RconPort < 1 || instance.RconPort > 65535)
            throw new ArgumentOutOfRangeException(nameof(instance), $"RCON port {instance.RconPort} is out of range");

        var target = string.IsNullOrWhiteSpace(instance.Ip) ? "127.0.0.1" : instance.Ip.Trim();
        Console.WriteLine($"Sending command {command} to RCON {target}:{instance.RconPort}");

        // coreRCON wants an IPAddress, dns names get resolved first
        var address = IPAddress.TryParse(target, out var parsed)
            ? parsed
            : (await Dns.GetHostAddressesAsync(target))[0];

        using var rcon = new RCON(address, (ushort)instance.RconPort, instance.RconPassword);
        await rcon.ConnectAsync();
        var response = await rcon.SendCommandAsync(command);

        if (command.Trim() == "stop") _running[instance.Id] = false;

        return response;
    }

    public IEnumerable<string> GetRecentLogs(ServerInstance instance)
    {
        var logFilePath = Path.Combine(instance.ServerDir, "logs", "latest.log");
        if (!File.Exists(logFilePath)) return [];

        return TailLines(logFilePath, LogsToStore);
    }

    private static IEnumerable<string> TailLines(string path, int count)
    {
        // read backwards from the end of the file instead of materializing it all
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var chunk = new byte[64 * 1024];
        var text = new StringBuilder();
        long pos = fs.Length;

        while (pos > 0 && text.ToString().Count(c => c == '\n') <= count)
        {
            var read = (int)Math.Min(chunk.Length, pos);
            pos -= read;
            fs.Seek(pos, SeekOrigin.Begin);

            int filled = 0;
            while (filled < read)
            {
                int n = fs.Read(chunk, filled, read - filled);
                if (n == 0) break;
                filled += n;
            }

            text.Insert(0, Encoding.UTF8.GetString(chunk, 0, filled));
        }

        var lines = text.ToString().Split('\n');
        if (lines.Length > count) lines = lines[^count..];

        return lines.Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0);
    }
}
