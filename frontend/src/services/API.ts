const API_URL = 'http://localhost:5029';

export const startServer = async (appendLog: (msg: string) => void) => {
  try {
    const response = await fetch(`${API_URL}/api/server/start`, { method: 'POST' });
    const data = await response.json();

    switch (response.status) {
      case 200: // OK
        appendLog("[SYSTEM] Starting server...");
        break;
      case 202: // ACCEPTED
        appendLog("[SYSTEM] Starting server...");
        break;
      case 409: // CONFLICT
        appendLog("[SYSTEM] [HTTP 409] Server is already started.");
        break;
      default:
        appendLog(`[SYSTEM] [HTTP ${response.status}] ${data.message || 'Error starting server'}`);
        break;
    }
  } catch (err) {
    console.error('[SYSTEM] Error starting server:', err);
    appendLog('[SYSTEM ERROR] Failed to connect to start endpoint.');
  }
};

export const stopServer = async (appendLog: (msg: string) => void) => {
  try {
    const response = await fetch(`${API_URL}/api/server/stop`, { method: 'POST' });
    const data = await response.json();

    switch (response.status) {
      case 202:
        appendLog("[SYSTEM] Stopping server...");
        break;
      case 409:
        appendLog("[SYSTEM] [HTTP 409] Server is not running.");
        break;
      default:
        appendLog(`[SYSTEM] [HTTP ${response.status}] ${data.message || 'Error stopping server'}`);
        break;
    }
  } catch (err) {
    console.error('[SYSTEM] Error stopping server:', err);
    appendLog('[SYSTEM ERROR] Failed to connect to stop endpoint.');
  }
};

export const sendCommand = async (appendLog: (msg: string) => void, cmd: string, rconPort: number, rconPassword: string) => {
  try {
    const response = await fetch(`${API_URL}/api/server/command`, { 
      method: 'POST', 
      headers: { 
        'Content-Type': 'application/json', 
      }, 
      body: JSON.stringify({ 
        command: cmd, 
        port: rconPort, 
        password: rconPassword
        }) 
      });

    const data = await response.json();
    appendLog(data.response);

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

  } catch (error: any) {
    console.error('[SYSTEM] Error sending command:', error);

  }
};