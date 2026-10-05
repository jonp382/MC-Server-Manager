using System;
using Microsoft.AspNetCore.Mvc;
using MinecraftServerManager.Services;
using System.Net;
using System.Diagnostics;
using System.Globalization;

namespace MinecraftServerManager.Controllers;

public record CommandRequest(string Command, string? ServerIp, long rconPort, string rconPassword);

[ApiController]
[Route("api/[controller]")]
public class ServerController : ControllerBase
{
    private readonly IMinecraftProcessService _processService;
    private readonly MinecraftQueryService _queryService;

    public ServerController(IMinecraftProcessService processService, MinecraftQueryService queryService)
    {
        _processService = processService;
        _queryService = queryService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(string? ip = null, int? port = null)
    {
        try
        {
            var status = await _queryService.QueryServerAsync(ip, port);
            return Ok(new { status.IsOnline });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("logs")]
    public IActionResult GetLogs()
    {
        try
        {
            var logs = _processService.GetRecentLogs();
            return Ok(new { logs });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("players")]
    public async Task<IActionResult> GetPlayers(string? ip = null, int? port = null)
    {
        try
        {
            var status = await _queryService.QueryServerAsync(ip, port);
            return Ok(new { status.OnlinePlayers });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand([FromBody] CommandRequest req)
    {
        try
        {
            string response = await _processService.SendCommand(req.Command, req.ServerIp, req.rconPort, req.rconPassword);
            return Ok(new {success = true, response});
        }
        catch (Exception ex)
        {
            return StatusCode(500, new {success = false, error = ex.Message});
        }
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartServer()
    {
        try
        {
            bool result = await _processService.StartServer();
            return Accepted(new { status = "Server started as new process" });
        }
        catch(Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to start server: {ex.Message}" });
        }
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopServer([FromBody] CommandRequest req)
    {
        // use the same structure as sendcommand but just ignore the command and hard-code stop.
        try
        {
            CommandRequest customRequest = req with
            {
                Command="stop"
            };
            
            await SendCommand(customRequest);
            return Accepted(new {status = "Server stopped via RCON"});
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to stop server: {ex.Message}" });
        }
    }
}