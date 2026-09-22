using System;
using Microsoft.AspNetCore.Mvc;
using MinecraftServerManager.Services;

namespace MinecraftServerManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServerController : ControllerBase
{
    private readonly IMinecraftProcessService _processService;

    public ServerController(IMinecraftProcessService processService)
    {
        _processService = processService;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = _processService.GetServerStatus();
        return Ok(new { status });
    }

    [HttpGet("players")]
    public IActionResult GetPlayers()
    {
        var players = _processService.GetOnlinePlayers();
        return Ok(new { players });
    }

    [HttpPost("start")]
    public IActionResult StartServer()
    {
        try
        {
            var val = _processService.StartServer();
            bool success = val.Result;

            if (success) // server was successfully started
            {
                return Accepted(new { message = "Server starting..." });
            }
            else // server is already running
            {
                return Conflict(new { conflict = "Server is already running." });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new {message = $"Failed to launch the server process: {ex.Message}"});
        }
    }

    [HttpPost("stop")]
    public IActionResult StopServer()
    {
        var val = _processService.StopServer();
        bool success = val.Result;

        if (success) // server was successfully stopped
        {
            return Accepted(new { message = "Stopping server..." });
        }
        else // server isn't running
        {
            return Conflict(new { conflict = "Server is not running." });
        }
    }
}