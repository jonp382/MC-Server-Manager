namespace MinecraftServerManager.Services;

using Microsoft.AspNetCore.SignalR;
using MinecraftServerManager.Hubs;
using System.Diagnostics;
using System.Text;

public sealed class LogWatcherService : BackgroundService
{
    private readonly IHubContext<ConsoleHub> _hubContext;
    private readonly ILogger<LogWatcherService> _logger;

    private readonly string _logFilePath =
        "/home/jonny/Games/Minecraft Servers/Test Server/logs/latest.log";

    public LogWatcherService(
        IHubContext<ConsoleHub> hubContext,
        ILogger<LogWatcherService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "LogWatcherService started watching {Path}",
            _logFilePath);

        long position = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                await using var stream = new FileStream(
                    _logFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 4096,
                    options: FileOptions.SequentialScan);

                // seek to end to read new lines
                stream.Seek(0, SeekOrigin.End);
                position = stream.Position;

                var identity = GetFileIdentity(_logFilePath);

                _logger.LogInformation(
                    "Attached to Minecraft log. Identity={Identity}, Position={Position}",
                    identity,
                    position);

                var buffer = new byte[8192];
                var pending = new StringBuilder();

                while (!stoppingToken.IsCancellationRequested)
                {
                    // check whether the latest.log file was replaced by a new server instance
                    if (!File.Exists(_logFilePath))
                    {
                        _logger.LogInformation(
                            "Minecraft log disappeared. Reattaching...");

                        break;
                    }

                    var currentIdentity = GetFileIdentity(_logFilePath);

                    if (currentIdentity != identity)
                    {
                        _logger.LogInformation(
                            "Minecraft log was replaced. Old={OldIdentity}, New={NewIdentity}",
                            identity,
                            currentIdentity);

                        break;
                    }

                    var currentLength = new FileInfo(_logFilePath).Length;

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

                    var bytesRead = await stream.ReadAsync(
                        buffer,
                        stoppingToken);

                    if (bytesRead == 0)
                    {
                        await Task.Delay(100, stoppingToken);
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

                        await _hubContext.Clients.All.SendAsync(
                            "ReceiveLogLine",
                            line,
                            stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
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
            await Task.Delay(500, stoppingToken);
        }

        _logger.LogInformation("LogWatcherService stopped.");
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
