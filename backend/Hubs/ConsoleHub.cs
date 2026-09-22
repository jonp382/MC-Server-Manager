using Microsoft.AspNetCore.SignalR;
using MinecraftServerManager.Services;

namespace MinecraftServerManager.Hubs;

public class ConsoleHub : Hub
{
    private readonly IMinecraftProcessService _processService;

    public ConsoleHub(IMinecraftProcessService processService)
    {
        _processService = processService;
    }

    public async Task SendServerCommand(string command)
    {
        await _processService.SendCommand(command);
    }
}