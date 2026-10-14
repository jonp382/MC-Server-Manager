using Microsoft.AspNetCore.Mvc;
using MinecraftServerManager.Models;
using MinecraftServerManager.Services;

namespace MinecraftServerManager.Controllers;

public record CommandRequest(string Command);

[ApiController]
[Route("api/[controller]")]
public class ServerController : ControllerBase
{
    private readonly ServerRegistry _registry;
    private readonly ServerProcessService _processService;
    private readonly MinecraftQueryService _queryService;

    public ServerController(ServerRegistry registry, ServerProcessService processService, MinecraftQueryService queryService)
    {
        _registry = registry;
        _processService = processService;
        _queryService = queryService;
    }

    // the single instance, servers.json is the source of truth
    [HttpGet("servers")]
    public IActionResult GetServers() => Ok(_registry.GetAll());

    [HttpPost("servers")]
    public IActionResult CreateServer([FromBody] ServerInstance instance)
    {
        var created = _registry.Create(instance);
        return Ok(created);
    }

    [HttpPut("servers")]
    public IActionResult UpdateServer([FromBody] ServerInstance instance)
    {
        if (!_registry.Update(instance))
            return NotFound(new { error = "no server configured" });
        return Ok(instance);
    }

    private ServerInstance? Current() => _registry.GetAll().FirstOrDefault();

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var instance = Current();
        if (instance == null) return NotFound(new { error = "no server configured" });

        try
        {
            var status = await _queryService.QueryServerAsync(instance.Ip, instance.QueryPort);
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
        var instance = Current();
        if (instance == null) return NotFound(new { error = "no server configured" });

        try
        {
            var logs = _processService.GetRecentLogs(instance);
            return Ok(new { logs });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("players")]
    public async Task<IActionResult> GetPlayers()
    {
        var instance = Current();
        if (instance == null) return NotFound(new { error = "no server configured" });

        try
        {
            var status = await _queryService.QueryServerAsync(instance.Ip, instance.QueryPort);
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
        var instance = Current();
        if (instance == null) return NotFound(new { success = false, error = "no server configured" });

        try
        {
            // ip, rcon port and password come from the instance, not from the browser
            string response = await _processService.SendCommand(instance, req.Command);
            return Ok(new { success = true, response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartServer()
    {
        var instance = Current();
        if (instance == null) return NotFound(new { error = "no server configured" });

        try
        {
            bool result = await _processService.StartServer(instance);
            if (!result) return Conflict(new { message = "server is already started" });
            return Accepted(new { status = "Server started as detached process" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to start server: {ex.Message}" });
        }
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopServer([FromBody] CommandRequest req)
    {
        // use the same structure as sendcommand but just ignore the command and hard-code stop.
        req = req with { Command = "stop" };
        return await SendCommand(req);
    }
}
