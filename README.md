# MC-Server-Manager

A web UI for managing Minecraft servers that run on their own, detached from this tool. It is a manager and a console, not a server. It ships no server software, no jar, no launcher. The Minecraft server runs somewhere else (a docker container, a systemd service, a remote box, a terminal you started by hand) and this tool points at it over RCON and the server query protocol.

The point is to watch and drive servers from a browser instead of a terminal window, and to manage several of them at once. The backend does not own the server process. If the backend crashes, the server keeps running.

[WIP]

## What it does

- Keeps a registry of server instances (name, directory, ip, query port, rcon port, rcon password) in `servers.json`.
- Adds, edits, and deletes instances from the web UI.
- Starts a server as a detached process so it survives the backend exiting.
- Sends commands to a running server over RCON.
- Tails the server log and streams it live to the browser over SignalR, tagged by server so consoles don't mix.
- Shows online players, refreshed from the server query protocol and player join/leave events.
- Filters the console by log level (info, warn, error) with counts.
- Command history in the input box (up/down arrows).

## Architecture

- `backend/` ASP.NET Core (.NET 10). REST API, SignalR hub, RCON via CoreRCON, server status via MineStat. Runs on `http://localhost:5029`.
- `frontend/` React 18 + TypeScript + Vite + Tailwind. Talks to the backend over HTTP and SignalR.
- `servers.json` is the source of truth for instances. Seeded from `appsettings.json` on first run.

## Running with Docker

`docker compose up -d` runs the whole stack. The frontend builds to static files and is served by nginx; nginx also proxies `/api` and `/hubs` to the backend, so the browser talks to one origin (`http://localhost:3000`) and there is no CORS. The backend runs on the compose network with no published ports; only nginx reaches it.

```
docker compose up -d
```

Two things to set per machine, in `docker-compose.yml`. Both appear in the `volumes:` and `environment:` blocks, and the two sides must be the same path:

- **Server folders.** The registry stores host paths (the folder you pick in the UI), and the backend container cannot see host paths directly. Mount one parent folder that contains all your server folders, and tell the backend what it is: `MSM_SERVER_ROOT` is the host folder (identical to the volume's left side), `MSM_SERVER_MOUNT` is the container path (identical to the volume's right side). The backend translates instance paths between the two at the filesystem boundary. Every server folder under the parent is covered, so adding a server in the UI needs no compose change. A `serverDir` outside the parent is not translated and fails with a missing-path error.
- **Java.** The image ships no Java, so `java` on PATH does not exist in the container and the Start button fails. The container spawns servers with `JAVA_HOME/bin/java` when set. Mount the JDK your servers need (Adoptium, Oracle, any flavor, any version) and point `JAVA_HOME` at the JDK's **home directory** (the folder containing `bin/java`), not at the `java` binary. If your JDK folder is nested (e.g. `jdk-21/jdk-21/`), point at the inner folder that holds `bin`.

```yaml
volumes:
  - /home/user/minecraft-servers:/servers
  - /home/user/jdk-21:/opt/jdk
environment:
  - MSM_SERVER_ROOT=/home/user/minecraft-servers
  - MSM_SERVER_MOUNT=/servers
  - JAVA_HOME=/opt/jdk
```

The backend writes `servers.json` to a named volume (`server-data`) so the registry survives rebuilds.

**RCON and status to servers on the host** need `network_mode: "host"` on the backend (it is commented out in the compose file): a container on the compose bridge cannot reach the host's ports, so RCON to a host-run server times out. On the host network the compose service name has no DNS, so nginx proxies through the host's docker bridge gateway instead of `backend` — edit `frontend/nginx.conf`'s `proxy_pass` targets to the gateway address (find it with `ip -br addr | grep docker0`). The host gateway is firewalled on some machines (UFW's default deny drops container-to-host traffic); if the proxy times out, allow the docker subnet in the firewall, keep the default bridge and set the instance `ip` to the host's LAN address, or run the server in a container on the compose network so RCON is container-to-container.

## Getting started

Backend:

```
cd backend
dotnet run
```

Frontend:

```
cd frontend
npm install
npm run dev
```

Open the dev server the frontend prints, point it at the backend on `localhost:5029`. Add a server instance in the UI, then start it.

In dev, Vite proxies `/api` and `/hubs` to the backend, so the frontend uses relative URLs and works the same as in the docker stack.

## Configuration

`servers.json` holds the instances. Each entry:

```json
{
  "id": "auto-generated",
  "name": "My Server",
  "serverDir": "/home/user/minecraft",
  "ip": "127.0.0.1",
  "queryPort": 25565,
  "rconPort": 25575,
  "rconPassword": "..."
}
```

The server must have RCON enabled (`enable-rcon=true`, a `rcon.port`, a `rcon.password` in its `server.properties`) for commands to work. The query port is used for status and player counts.

## Notes

- Detached start uses `setsid` on Linux and `cmd /c start` on Windows. The Linux path is tested, the Windows path is written but not tested.
- The log tail reads backwards from the end of the file, so a large log does not get read into memory in full.
- RCON ip, port, and password come from the instance, not from the browser.

## License

MIT. See [LICENSE](LICENSE).
