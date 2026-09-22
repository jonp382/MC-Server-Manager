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
        return Ok(new { isRunning = _processService.IsRunning });
    }

    [HttpPost("start")]
    public IActionResult StartServer()
    {
        _processService.StartServer();
        return Ok(new { message = "Server starting..." });
    }

    [HttpPost("stop")]
    public IActionResult StopServer()
    {
        _processService.StopServer();
        return Ok(new { message = "Stop command sent." });
    }
}