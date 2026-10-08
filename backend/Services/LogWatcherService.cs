namespace MinecraftServerManager.Services;

using Microsoft.AspNetCore.SignalR;
using MinecraftServerManager.Hubs;
using MinecraftServerManager.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;

// one watcher per server instance, reconciled against the registry
public sealed class LogWatcherService : BackgroundService
{
    private readonly IHubContext<ConsoleHub> _hubContext;
    private readonly ILogger<LogWatcherService> _logger;
    private readonly ServerRegistry _registry;
    private readonly PathTranslator _paths;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _watchers = new();
    private CancellationToken _stoppingToken;

    public LogWatcherService(
        IHubContext<ConsoleHub> hubContext,
        ILogger<LogWatcherService> logger,
        ServerRegistry registry,
        PathTranslator paths)
    {
        _hubContext = hubContext;
        _logger = logger;
        _registry = registry;
        _paths = paths;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        _registry.Changed += Reconcile;
        Reconcile();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        foreach (var cts in _watchers.Values) cts.Cancel();
        _registry.Changed -= Reconcile;

        _logger.LogInformation("LogWatcherService stopped.");
    }

    private void Reconcile()
    {
        var instances = _registry.GetAll().ToDictionary(i => i.Id);

        // stop watchers for removed instances
        foreach (var id in _watchers.Keys.ToList())
        {
            if (!instances.ContainsKey(id))
            {
                _watchers[id].Cancel();
                _watchers.TryRemove(id, out _);
            }
        }

        // attach watchers for new instances
        foreach (var instance in instances.Values)
        {
            if (_watchers.ContainsKey(instance.Id)) continue;

            var cts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken);
            _watchers[instance.Id] = cts;
            _ = WatchLoop(instance, cts.Token);
        }
    }

    private async Task WatchLoop(ServerInstance instance, CancellationToken token)
    {
        // registry stores host paths; watch the mounted container path
        var logFilePath = Path.Combine(_paths.ToContainerPath(instance.ServerDir), "logs", "latest.log");

        _logger.LogInformation(
            "LogWatcherService started watching {Path} for {Name}",
            logFilePath,
            instance.Name);

        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!File.Exists(logFilePath))
                {
                    await Task.Delay(500, token);
                    continue;
                }

                await using var stream = new FileStream(
                    logFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 4096,
                    options: FileOptions.SequentialScan);

                // seek to end to read new lines
                stream.Seek(0, SeekOrigin.End);
                long position = stream.Position;

                var identity = GetFileIdentity(logFilePath);
                var lastIdentityCheck = DateTime.UtcNow;

                _logger.LogInformation(
                    "Attached to Minecraft log. Identity={Identity}, Position={Position}",
                    identity,
                    position);

                var buffer = new byte[8192];
                var pending = new StringBuilder();

                while (!token.IsCancellationRequested)
                {
                    // check whether the latest.log file was replaced by a new server instance
                    if (!File.Exists(logFilePath))
                    {
                        _logger.LogInformation(
                            "Minecraft log disappeared. Reattaching...");

                        break;
                    }

                    // stat spawns a process, so only check the identity every couple of seconds
                    if ((DateTime.UtcNow - lastIdentityCheck).TotalSeconds >= 2)
                    {
                        lastIdentityCheck = DateTime.UtcNow;

                        var currentIdentity = GetFileIdentity(logFilePath);

                        if (currentIdentity != identity)
                        {
                            _logger.LogInformation(
                                "Minecraft log was replaced. Old={OldIdentity}, New={NewIdentity}",
                                identity,
                                currentIdentity);

                            break;
                        }
                    }

                    var currentLength = new FileInfo(logFilePath).Length;

                    // file was truncated in-place.
                    if (currentLength < position)
                    {
                        _logger.LogInformation(
                            "Minecraft log was truncated. OldPosition={Position}, NewLength={Length}",
                            position,
                            currentLength);

                        break;
                    }

                    stream.Position = position;

                    var bytesRead = await stream.ReadAsync(buffer, token);

                    if (bytesRead == 0)
                    {
                        await Task.Delay(100, token);
                        continue;
                    }

                    position += bytesRead;

                    pending.Append(
                        Encoding.UTF8.GetString(buffer, 0, bytesRead));

                    while (true)
                    {
                        var newlineIndex = pending.ToString().IndexOf('\n');

                        if (newlineIndex < 0)
                            break;

                        var line = pending
                            .ToString(0, newlineIndex)
                            .TrimEnd('\r');

                        pending.Remove(0, newlineIndex + 1);

                        if (line.Length == 0)
                            continue;

                        _logger.LogInformation(
                            "[MC LOG] {Line}",
                            line);

                        // lines carry the instance id so the frontend can filter by active server
                        await _hubContext.Clients.All.SendAsync(
                            "ReceiveLogLine",
                            instance.Id,
                            line,
                            token);
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error watching Minecraft log. Retrying...");
            }

            // small delay before trying to attach again.
            try { await Task.Delay(500, token); } catch (OperationCanceledException) { break; }
        }
    }

    private static string GetFileIdentity(string path)
    {
        // linux-specific, `stat` gives us the device + inode of the actual file.
        var psi = new ProcessStartInfo
        {
            FileName = "/usr/bin/stat",
            Arguments = $"-c \"%d:%i\" \"{path}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start stat.");

        var output = process.StandardOutput.ReadToEnd().Trim();

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new IOException($"stat failed for {path}");

        return output;
    }
}
