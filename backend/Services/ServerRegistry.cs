using System.Text.Json;
using MinecraftServerManager.Models;

namespace MinecraftServerManager.Services;

// servers.json on disk is the source of truth, no database.
// seeded from appsettings on first run so an existing single-server setup keeps working.
public class ServerRegistry
{
    private readonly string _filePath;
    private readonly IConfiguration _config;
    private readonly List<ServerInstance> _instances = new();
    private readonly object _lock = new();

    // fires when instances are added/removed/updated so the log watcher can reconcile
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
        lock (_lock) return _instances.ToList();
    }

    public ServerInstance? Get(string id)
    {
        lock (_lock) return _instances.FirstOrDefault(i => i.Id == id);
    }

    public ServerInstance Create(ServerInstance instance)
    {
        lock (_lock)
        {
            instance.Id = Guid.NewGuid().ToString("N");
            _instances.Add(instance);
            Save();
        }
        Changed?.Invoke();
        return instance;
    }

    public bool Update(ServerInstance instance)
    {
        lock (_lock)
        {
            var existing = _instances.FirstOrDefault(i => i.Id == instance.Id);
            if (existing == null) return false;

            _instances[_instances.IndexOf(existing)] = instance;
            Save();
        }
        Changed?.Invoke();
        return true;
    }

    public bool Delete(string id)
    {
        lock (_lock)
        {
            var existing = _instances.FirstOrDefault(i => i.Id == id);
            if (existing == null) return false;

            _instances.Remove(existing);
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
            if (loaded != null) _instances.AddRange(loaded);
        }

        // seed from appsettings so the old single-server config carries over
        if (_instances.Count == 0 && _config["Minecraft:ServerDir"] != null)
        {
            _instances.Add(new ServerInstance
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Default Server",
                ServerDir = _config["Minecraft:ServerDir"]!,
                Ip = _config["Minecraft:ServerIp"] ?? "127.0.0.1",
                QueryPort = int.TryParse(_config["Minecraft:QueryPort"], out var qp) ? qp : 25565,
            });
            Save();
        }
    }

    private void Save()
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_instances, new JsonSerializerOptions { WriteIndented = true }));
    }
}
