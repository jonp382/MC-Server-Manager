const API_URL = '';

export interface ServerInstance {
  id: string;
  name: string;
  serverDir: string;
  ip: string;
  queryPort: number;
  rconPort: number;
  rconPassword: string;
}

// single server: the backend keeps one instance in servers.json.
// the frontend reads it and saves settings back to it.
export const fetchServers = async (): Promise<ServerInstance[]> => {
  const res = await fetch(`${API_URL}/api/server/servers`);
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
};

// first-run bootstrap: creates the single instance when the registry is empty
export const createServer = async (instance: ServerInstance): Promise<ServerInstance> => {
  const res = await fetch(`${API_URL}/api/server/servers`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(instance),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
};

export const updateServer = async (instance: ServerInstance): Promise<ServerInstance> => {
  const res = await fetch(`${API_URL}/api/server/servers`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(instance),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
};

export const fetchStatus = async (id: string): Promise<boolean> => {
  const res = await fetch(`${API_URL}/api/server/status?id=${id}`);
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const data = await res.json();
  return data.isOnline;
};

export const fetchPlayers = async (id: string): Promise<string[]> => {
  const res = await fetch(`${API_URL}/api/server/players?id=${id}`);
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const data = await res.json();
  return data.onlinePlayers;
};

export const fetchLogs = async (id: string): Promise<string[]> => {
  const res = await fetch(`${API_URL}/api/server/logs?id=${id}`);
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const data = await res.json();
  return data.logs || [];
};

export const startServer = async (appendLog: (msg: string) => void, id: string) => {
  try {
    const response = await fetch(`${API_URL}/api/server/start?id=${id}`, { method: 'POST' });
    const data = await response.json();

    switch (response.status) {
      case 202: // ACCEPTED
        appendLog('[SYSTEM] Starting server as detached process...');
        break;
      case 409: // CONFLICT
        appendLog('[SYSTEM] [HTTP 409] Server is already started.');
        break;
      default:
        appendLog(`[SYSTEM] [HTTP ${response.status}] ${data.message || data.error || 'Error starting server'}`);
        break;
    }
  } catch (err) {
    console.error('[SYSTEM] Error starting server:', err);
    appendLog('[SYSTEM ERROR] Failed to connect to start endpoint.');
  }
};

export const sendCommand = async (appendLog: (msg: string) => void, cmd: string, id: string) => {
  try {
    const response = await fetch(`${API_URL}/api/server/command`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ serverId: id, command: cmd }),
    });

    const data = await response.json();
    appendLog(data.success ? 'Successfully sent command: ' + data.response : 'Failed to execute command: ' + data.error);

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
  } catch (error: any) {
    console.error('[SYSTEM] Error sending command:', error);
  }
};

export const stopServer = async (appendLog: (msg: string) => void, id: string) => {
  appendLog('[SYSTEM] Stopping server...');
  return sendCommand(appendLog, 'stop', id);
};
