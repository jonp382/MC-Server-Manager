import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

import { Header } from './components/Header';
import { PlayerList } from './components/PlayerList';
import { TerminalConsole, LogMessage } from './components/TerminalConsole';
import { startServer, stopServer } from './services/API';
import { ServerSettings } from './components/SettingsModal';

const API_URL = 'http://localhost:5029';

export default function App() {
  // signalR connection
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  const [isConnected, setIsConnected] = useState(false);
  const [serverStatus, setServerStatus] = useState<'Stopped' | 'Starting' | 'Running'>('Stopped');
  const [logs, setLogs] = useState<LogMessage[]>([]);
  const [players, setPlayers] = useState<string[]>([]);

  const [serverSettings, setServerSettings] = useState<ServerSettings>(() => {
    const saved = localStorage.getItem('mc_server_settings');
    return saved ? JSON.parse(saved) : { serverIp: '127.0.0.1', queryPort: 25565, rconPort: 25575 };
  });

  const handleSaveSettings = (newSettings: ServerSettings) => {
    setServerSettings(newSettings);
    localStorage.setItem('mc_server_settings', JSON.stringify(newSettings));
    
  };

  // append system/console logs
  const appendLog = (message: string) => {
    const newLog: LogMessage = {
      id: Math.random().toString(36).substring(2, 9),
      text: message,
      timestamp: new Date().toLocaleTimeString(),
    };
    setLogs((prev) => [...prev.slice(-499), newLog]); // Retain last 500 lines
  };

  const onClearLogs = () => {
    setLogs([]);
  };

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
    try {
      const response = await fetch(`${API_URL}/api/server/status`);
      if (!response.ok) {
        throw new Error(`Server returned HTTP ${response.status}`);
      }

      // Log the JSON data to the console
      const data = await response.json();
      console.info(`Returned JSON:`, data);

      setServerStatus(data.isOnline ? 'Running' : 'Stopped');
    } catch (err) {
      console.error("Error fetching server status:", err);
      setServerStatus('Stopped');
    }
  };

  useEffect(() => {
    fetchServerStatus();

    fetch(`${API_URL}/api/server/players`)
      .then((res) => res.json())
      .then((data) => setPlayers(data.players || []))
      .catch((err) => console.error("Error fetching player list:", err));
  }, []);

  const handleRetrieveLogs = async () => {
    try {
      // Replace with your actual backend endpoint URL
      const response = await fetch(`${API_URL}/api/server/logs`);
      if (!response.ok) {
        throw new Error(`Server returned HTTP ${response.status}`);
      }

      const json = await response.json();
      const rawLogs: string[] = json.logs || [];
      
      const parsedLogs: LogMessage[] = rawLogs.map((logText, index) => ({
        id: `${Date.now()}-${index}`,
        text: logText,
        timestamp: new Date().toLocaleTimeString(),
      }));
      
      // update state with fetched logs
      setLogs(parsedLogs);
    } catch (err) {
      console.error('Failed to retrieve logs:', err);
     
      setLogs((prev) => [
        ...prev,
        {
          id: Math.random().toString(36).substring(2, 9),
          text: `[SYSTEM ERROR] Failed to fetch historical logs: ${err}`,
          timestamp: new Date().toLocaleTimeString(),
        },
      ]);
    }
  }

  return (
    <div className="bg-slate-950 text-slate-100 p-6 flex flex-col font-sans overflow-hidden">
      <Header
        serverStatus={serverStatus}
        isConnected={isConnected}
        onStart={() => startServer(appendLog)}
        onStop={() => stopServer(appendLog)}
        settings={serverSettings}
        onSaveSettings={handleSaveSettings}
      />

      <div className="gap-6 flex-1 min-h-0">
        <TerminalConsole
          logs={logs}
          connection={connection}
          isConnected={isConnected}
          onCommandError={(errText) => setLogs((prev) => [...prev, { id: Math.random().toString(36).substring(2, 9), text: `[COMMAND ERROR] ${errText}`, timestamp: new Date().toLocaleTimeString() }])}
          onClearLogs={onClearLogs}
          onRetrieveLogs={handleRetrieveLogs}
        />

        <PlayerList players={players} />
      </div>
    </div>
  );
}