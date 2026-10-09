using System.Text.Json;
using MinecraftServerManager.Models;

namespace MinecraftServerManager.Services;

// servers.json on disk is the source of truth, no database.
// single server: the registry holds one instance, seeded from appsettings on first run.
public class ServerRegistry
{
    private readonly string _filePath;
    private readonly IConfiguration _config;
    private ServerInstance? _instance;
    private readonly object _lock = new();

    // fires when the instance is added/updated so the log watcher can reconcile
    public event Action? Changed;

    public ServerRegistry(IConfiguration config, IHostEnvironment env)
    {
        _config = config;
        // RegistryPath lets docker point the registry at a mounted volume
        var configured = config["RegistryPath"];
        _filePath = string.IsNullOrEmpty(configured)
            ? Path.Combine(env.ContentRootPath, "servers.json")
            : configured;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        Load();
    }

    public IEnumerable<ServerInstance> GetAll()
    {
        lock (_lock) return _instance is null ? [] : new[] { _instance };
    }

    public ServerInstance? Get(string id)
    {
        lock (_lock) return _instance?.Id == id ? _instance : null;
    }

    public ServerInstance Create(ServerInstance instance)
    {
        lock (_lock)
        {
            instance.Id = Guid.NewGuid().ToString("N");
            _instance = instance;
            Save();
        }
        Changed?.Invoke();
        return instance;
    }

    public bool Update(ServerInstance instance)
    {
        lock (_lock)
        {
            if (_instance == null || _instance.Id != instance.Id) return false;

            _instance = instance;
            Save();
        }
        Changed?.Invoke();
        return true;
    }

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            var loaded = JsonSerializer.Deserialize<List<ServerInstance>>(File.ReadAllText(_filePath));
            if (loaded is { Count: > 0 }) _instance = loaded[0];
        }

        // seed from appsettings so the old single-server config carries over
        if (_instance == null && _config["Minecraft:ServerDir"] != null)
        {
            _instance = new ServerInstance
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Default Server",
                ServerDir = _config["Minecraft:ServerDir"]!,
                Ip = _config["Minecraft:ServerIp"] ?? "127.0.0.1",
                QueryPort = int.TryParse(_config["Minecraft:QueryPort"], out var qp) ? qp : 25565,
            };
            Save();
        }
    }

    private void Save()
    {
        // keep the file a list so existing volume data loads unchanged
        var list = _instance is null ? new List<ServerInstance>() : new List<ServerInstance> { _instance };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
    }
}
