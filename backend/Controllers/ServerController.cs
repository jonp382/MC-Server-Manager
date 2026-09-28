using System;
using Microsoft.AspNetCore.Mvc;
using MinecraftServerManager.Services;
using System.Net;
using System.Diagnostics;

namespace MinecraftServerManager.Controllers;

public record CommandRequest(string Command);

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
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var status = await _queryService.GetServerStatusAsync();
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
        var logs = _processService.GetRecentLogs();
        return Ok(new { logs });
    }

    [HttpGet("players")]
    public IActionResult GetPlayers()
    {
        var players = _processService.GetOnlinePlayers();
        return Ok(new { players });
    }

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand([FromBody] CommandRequest req)
    {
        try
        {
            string response = await _processService.SendCommand(req.Command);
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
    public async Task<IActionResult> StopServer()
    {
        try
        {
            bool result = await _processService.StopServer();
            return Accepted(new {status = "Server stopped via RCON"});
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to stop server: {ex.Message}" });
        }
    }
}