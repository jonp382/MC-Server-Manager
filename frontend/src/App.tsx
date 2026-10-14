import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

import { Header } from './components/Header';
import { PlayerList } from './components/PlayerList';
import { TerminalConsole, LogMessage } from './components/TerminalConsole';
import {
  ServerInstance,
  fetchServers,
  createServer,
  updateServer,
  startServer,
  stopServer,
  fetchStatus,
  fetchPlayers,
  fetchLogs,
} from './services/API';

// relative so vite's dev proxy and the nginx container proxy both work
const API_URL = '';

export default function App() {
  // signalR connection
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  const [isConnected, setIsConnected] = useState(false);
  const [serverStatus, setServerStatus] = useState<'Stopped' | 'Starting' | 'Running'>('Stopped');
  const [players, setPlayers] = useState<string[]>([]);

  // the single server instance from the registry
  const [instance, setInstance] = useState<ServerInstance | null>(null);

  const [logs, setLogs] = useState<LogMessage[]>([]);

  const appendLog = (message: string) => {
    const newLog: LogMessage = {
      id: Math.random().toString(36).substring(2, 9),
      text: message,
      timestamp: new Date().toLocaleTimeString(),
    };
    setLogs((prev) => [...prev.slice(-499), newLog]); // retain last 500 lines
  };

  const onClearLogs = () => setLogs([]);

  // load the registry on mount, point at the first server
  useEffect(() => {
    fetchServers()
      .then((list) => {
        if (list.length > 0) setInstance(list[0]);
      })
      .catch((err) => console.error('Failed to load servers:', err));
  }, []);

  // establish SignalR connection on component mount
  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/ConsoleHub`)
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  // start connection and register listeners
  useEffect(() => {
    if (!connection) return;

    connection.start()
      .then(() => {
        setIsConnected(true);
        console.log('Connected to SignalR ConsoleHub');

        // Console Log Stream
        connection.on('ReceiveLogLine', (line: string) => {
          appendLog(line);
        });

        // Server Status Stream
        connection.on('StatusChanged', (status: 'Stopped' | 'Starting' | 'Running') => {
          setServerStatus(status);
        });

        // Player Lifecycle Events
        connection.on('PlayerJoined', (username: string) => {
          setPlayers((prev) => [...new Set([...prev, username])]);
        });

        connection.on('PlayerLeft', (username: string) => {
          setPlayers((prev) => prev.filter((p) => p !== username));
        });

        connection.on('PlayerListUpdated', (newList: string[]) => {
          setPlayers(newList);
        });
      })
      .catch((err) => {
        console.error('SignalR Connection Error: ', err);
        setIsConnected(false);
      });

    return () => {
      connection.stop();
    };
  }, [connection]);

  const fetchServerStatus = async () => {
    if (!instance) return;
    try {
      const online = await fetchStatus();
      setServerStatus(online ? 'Running' : 'Stopped');
    } catch (err) {
      console.error('Error fetching server status:', err);
      setServerStatus('Stopped');
    }
  };

  const fetchCurrentPlayers = async () => {
    if (!instance) return;
    try {
      const list = await fetchPlayers();
      setPlayers(list);
    } catch (err) {
      console.error('Error fetching players:', err);
      setPlayers([]);
    }
  };

  useEffect(() => {
    fetchServerStatus();
    fetchCurrentPlayers();
  }, [instance]);

  // settings save: writes back to the instance, or creates it when the registry is empty
  const handleSaveSettings = async (saved: ServerInstance) => {
    try {
      const result = instance ? await updateServer(saved) : await createServer(saved);
      setInstance(result);
    } catch (err) {
      console.error('Failed to save server:', err);
    }
  };

  const handleRetrieveLogs = async () => {
    if (!instance) return;
    try {
      const rawLogs = await fetchLogs();

      const parsedLogs: LogMessage[] = rawLogs.map((logText, index) => ({
        id: `${Date.now()}-${index}`,
        text: logText,
        timestamp: new Date().toLocaleTimeString(),
      }));

      // update state with fetched logs
      setLogs(parsedLogs);
    } catch (err) {
      console.error('Failed to retrieve logs:', err);
      appendLog(`[SYSTEM ERROR] Failed to fetch historical logs: ${err}`);
    }
  };

  // Query the backend every 3 seconds
  useEffect(() => {
    const interval = setInterval(() => {
      fetchServerStatus();
      fetchCurrentPlayers();
    }, 3000);

    // Cleanup interval on component unmount
    return () => clearInterval(interval);
  }, [instance]);

  return (
    <div className="bg-slate-950 text-slate-100 p-6 w-screen h-screen font-sans overflow-hidden">
      <Header
        instance={instance}
        serverStatus={serverStatus}
        isConnected={isConnected}
        onStart={() => startServer((msg) => appendLog(msg))}
        onStop={() => stopServer((msg) => appendLog(msg))}
        onSaveSettings={handleSaveSettings}
      />

      <div className="gap-6 flex-1 min-h-0">
        <TerminalConsole
          logs={logs}
          isConnected={isConnected}
          onCommandExecution={(errText) => appendLog(errText)}
          onClearLogs={onClearLogs}
          onRetrieveLogs={handleRetrieveLogs}
        />

        <PlayerList players={players} />
      </div>
    </div>
  );
}
