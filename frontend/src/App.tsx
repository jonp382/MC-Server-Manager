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

const API_URL = 'http://localhost:5029';

export default function App() {
  // signalR connection
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  const [isConnected, setIsConnected] = useState(false);
  const [serverStatus, setServerStatus] = useState<'Stopped' | 'Starting' | 'Running'>('Stopped');
  const [players, setPlayers] = useState<string[]>([]);

  // instances from the registry, and which one the UI is pointed at
  const [instances, setInstances] = useState<ServerInstance[]>([]);
  const [activeId, setActiveId] = useState<string>('');

  // logs are kept per server so switching servers doesn't mix consoles
  const [logsByServer, setLogsByServer] = useState<Record<string, LogMessage[]>>({});

  const appendLog = (serverId: string, message: string) => {
    const newLog: LogMessage = {
      id: Math.random().toString(36).substring(2, 9),
      text: message,
      timestamp: new Date().toLocaleTimeString(),
    };
    setLogsByServer((prev) => ({
      ...prev,
      [serverId]: [...(prev[serverId] ?? []).slice(-499), newLog], // retain last 500 lines
    }));
  };

  const onClearLogs = () => {
    setLogsByServer((prev) => ({ ...prev, [activeId]: [] }));
  };

  // load the registry on mount, point at the first server
  useEffect(() => {
    fetchServers()
      .then((list) => {
        setInstances(list);
        if (list.length > 0) setActiveId(list[0].id);
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

        // Console Log Stream, lines arrive tagged with the server they came from
        connection.on('ReceiveLogLine', (serverId: string, line: string) => {
          appendLog(serverId, line);
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
    if (!activeId) return;
    try {
      const online = await fetchStatus(activeId);
      setServerStatus(online ? 'Running' : 'Stopped');
    } catch (err) {
      console.error('Error fetching server status:', err);
      setServerStatus('Stopped');
    }
  };

  const fetchCurrentPlayers = async () => {
    if (!activeId) return;
    try {
      const list = await fetchPlayers(activeId);
      setPlayers(list);
    } catch (err) {
      console.error('Error fetching players:', err);
      setPlayers([]);
    }
  };

  useEffect(() => {
    fetchServerStatus();
    fetchCurrentPlayers();
  }, [activeId]);

  // add mode (empty id) creates, edit mode saves back to the registry
  const handleSaveSettings = async (instance: ServerInstance) => {
    try {
      if (!instance.id) {
        const created = await createServer(instance);
        setActiveId(created.id);
      } else {
        await updateServer(instance);
      }
      setInstances(await fetchServers());
    } catch (err) {
      console.error('Failed to save server:', err);
    }
  };

  const handleRetrieveLogs = async () => {
    if (!activeId) return;
    try {
      const rawLogs = await fetchLogs(activeId);

      const parsedLogs: LogMessage[] = rawLogs.map((logText, index) => ({
        id: `${Date.now()}-${index}`,
        text: logText,
        timestamp: new Date().toLocaleTimeString(),
      }));

      // update state with fetched logs
      setLogsByServer((prev) => ({ ...prev, [activeId]: parsedLogs }));
    } catch (err) {
      console.error('Failed to retrieve logs:', err);
      appendLog(activeId, `[SYSTEM ERROR] Failed to fetch historical logs: ${err}`);
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
  }, [activeId]);

  const logs = logsByServer[activeId] ?? [];

  return (
    <div className="bg-slate-950 text-slate-100 p-6 w-screen h-screen font-sans overflow-hidden">
      <Header
        instances={instances}
        activeId={activeId}
        onSelect={setActiveId}
        serverStatus={serverStatus}
        isConnected={isConnected}
        onStart={() => startServer((msg) => appendLog(activeId, msg), activeId)}
        onStop={() => stopServer((msg) => appendLog(activeId, msg), activeId)}
        onSaveSettings={handleSaveSettings}
      />

      <div className="gap-6 flex-1 min-h-0">
        <TerminalConsole
          logs={logs}
          serverId={activeId}
          isConnected={isConnected}
          onCommandExecution={(errText) => appendLog(activeId, errText)}
          onClearLogs={onClearLogs}
          onRetrieveLogs={handleRetrieveLogs}
        />

        <PlayerList players={players} />
      </div>
    </div>
  );
}
