namespace MinecraftServerManager.Services;

// the registry stores host paths (the user picks a folder in the browser,
// which runs on the host). a containerized backend sees a mounted parent
// folder at a fixed container path, so filesystem touches translate
// host path -> container path. outside a container both env vars are
// unset and paths pass through unchanged.
public sealed class PathTranslator
{
    private readonly string? _root;   // host parent folder, e.g. /home/jonny/Games/Minecraft Servers
    private readonly string? _mount;  // container mount point, e.g. /servers

    public PathTranslator(IConfiguration config)
    {
        _root = config["MSM_SERVER_ROOT"];
        _mount = config["MSM_SERVER_MOUNT"];
    }

    // true when the backend runs in a container with a server root mounted
    public bool Enabled => !string.IsNullOrEmpty(_root) && !string.IsNullOrEmpty(_mount);

    public string ToContainerPath(string hostPath)
    {
        if (!Enabled) return hostPath;

        var root = _root!.TrimEnd('/');
        var path = hostPath.TrimEnd('/');
        if (path == root) return _mount!;
        if (path.StartsWith(root + "/")) return _mount!.TrimEnd('/') + path.Substring(root.Length);

        // outside the mounted root: the container cannot see it, pass through
        // and let the caller fail on the missing file with a clear path in the error
        return hostPath;
    }
}
