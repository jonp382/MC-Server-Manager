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

## Recent changes

- Server registry and instance model, `servers.json` as the source of truth.
- Server process service that starts the server detached, so the backend crashing does not kill the server.
- Log watcher and controller made per instance, log lines tagged with the server id.
- Frontend supports multiple server instances, a selector in the header, logs kept per server.
- Server ip targeting, start server works locally.

## License

MIT. See [LICENSE](LICENSE).
