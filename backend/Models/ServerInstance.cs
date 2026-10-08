namespace MinecraftServerManager.Models;

// one server instance. the manager monitors servers that run regardless of this app,
// so an instance is just a target: where to find the server, not a process we own.
public class ServerInstance
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ServerDir { get; set; } = string.Empty;
    public string Ip { get; set; } = "127.0.0.1";
    public int QueryPort { get; set; } = 25565;
    public int RconPort { get; set; } = 25575;
    public string RconPassword { get; set; } = string.Empty;
}
