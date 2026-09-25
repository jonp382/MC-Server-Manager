namespace MinecraftServerManager.Services;

using Microsoft.AspNetCore.SignalR;
using System.IO;
using System.Text;
using MinecraftServerManager.Hubs;

public class LogWatcherService : BackgroundService
{
    private readonly IHubContext<ConsoleHub> _hubContext;
    private readonly ILogger<LogWatcherService> _logger;
    private readonly string _logFilePath = @"/home/jonny/Games/Minecraft Servers/Test Server/logs/latest.log";

    public LogWatcherService(IHubContext<ConsoleHub> hubContext, ILogger<LogWatcherService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LogWatcherService started watching: {Path}", _logFilePath);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!File.Exists(_logFilePath))
            {
                await Task.Delay(2000, stoppingToken);
                continue;
            }

            try
            {
                using var stream = new FileStream(
                    _logFilePath, 
                    FileMode.Open, 
                    FileAccess.Read, 
                    FileShare.ReadWrite | FileShare.Delete
                );
                using var reader = new StreamReader(stream, Encoding.UTF8);

                // On initial open, seek to the end for LIVE streaming
                stream.Seek(0, SeekOrigin.End);

                _logger.LogInformation("Stream attached to latest.log successfully.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    // Check if file shrank or was recreated (e.g. truncated on server start)
                    if (File.Exists(_logFilePath))
                    {
                        var latestInfo = new FileInfo(_logFilePath);
                        
                        if (latestInfo.Length < stream.Position)
                        {
                            _logger.LogInformation("Log file truncated or recreated. Re-attaching stream...");
                            break; // Re-open stream
                        }
                    }

                    var line = await reader.ReadLineAsync(stoppingToken);

                    if (line != null)
                    {
                        _logger.LogInformation("[MC LOG] {Line}", line);
                        await _hubContext.Clients.All.SendAsync("ReceiveLogLine", line, cancellationToken: stoppingToken);
                    }
                    else
                    {
                        // Clear buffer so StreamReader pulls fresh data from OS on next iteration
                        reader.DiscardBufferedData();
                        await Task.Delay(250, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading latest.log. Retrying in 2 seconds...");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}