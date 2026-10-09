# Minecraft Server Manager

A web UI for managing local Minecraft servers that run on their own.

## Features
- Live output of the server log, and a sync feature if the window is later refreshed
- Shows online players
- Filters the console by log level (info, warn, error) with counts.
- Command history in the input box (up/down arrows).

## Running with Docker

To set the program up via Docker, first review the `docker-compose.yml` file.
* Services > Backend > Environment
  * `MSM_SERVER_ROOT`: Set this to the absolute path of your server directories root (not where your server is, but one level up)
  * `MSM_SERVER_MOUNT`: Leave this as-is
* Services > Backend > Volumes
  * `server-data`: Leave this as-is
  * `/path/to/minecraft-servers`: Set this to the same path as `MSM_SERVER_ROOT` above.
* Services > Frontend > Ports
  * You can update the port to whatever you want. This will change the port you access the web UI from (e.g. `127.0.0.1:3000`)
* Services > Minecraft > Environment
  * `EULA=TRUE`: Leave as-is
  * `RCON_PORT=25575`: Leave as-is
  * `ENABLE_RCON=true`: Leave as-is
  * `RCON_PASSWORD`: Change to whatever you want, just make sure to set the same password in the Web UI settings. 
  * `VERSION`: Change to whatever minecraft version you want. Note that Mojang dropped the `1.xx.xx` prefix on their latest versions (e.g. `1.21.1` before, just `26.1` now).


Once these have been configured, in a terminal, run 

```docker compose up -d```

 It will launch each program, and the web UI will be available at `127.0.0.1:3000`. If you changed the port in the `docker-compose.yml`, use that port instead.

## Building from source

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

Open the dev server the frontend prints, point it at the backend on `localhost:5029`.

In dev, Vite proxies `/api` and `/hubs` to the backend, so the frontend uses relative URLs and works the same as in the docker stack.

## License

MIT. See [LICENSE](LICENSE).
