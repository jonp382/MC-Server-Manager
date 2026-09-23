import React, { useEffect, useState, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { Play, Square, Terminal, Power, Users, UserCheck } from 'lucide-react';

const API_URL = 'http://localhost:5029'; // Update port to match your dotnet run output

interface LogMessage {
  id: string;
  text: string;
  timestamp: string;
}

const isServerRunning = (status: string) => {
  const s = status.toLowerCase();
  return s === 'running' || s === 'starting';
};

export default function App() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [serverStatus, setServerStatus] = useState<'Stopped' | 'Starting' | 'Running'>('Stopped');
  const [logs, setLogs] = useState<LogMessage[]>([]);
  const [commandInput, setCommandInput] = useState('');
  const [players, setPlayers] = useState<string[]>([]);
  
  const terminalEndRef = useRef<HTMLDivElement>(null);

  // establish SignalR connection on component mount
  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/console`)
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

        // Listen for log lines from C# ProcessManager
        connection.on('ReceiveLogLine', (line: string) => {
          ConsoleLog(line);
        });

        // Listen for server state changes
        connection.on('StatusChanged', (status: 'Stopped' | 'Starting' | 'Running') => {
          setServerStatus(status);
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

  // auto-scroll terminal to bottom when new logs arrive
  useEffect(() => {
    terminalEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [logs]);

  const ConsoleLog = (message: string) => {
    const newLog: LogMessage = {
      id: Math.random().toString(36).substring(2, 9),
      text: message,
      timestamp: new Date().toLocaleTimeString(),
    };
    setLogs((prev) => [...prev.slice(-499), newLog]); // Retain last 500 lines
  }

  // fetch server status on page load and reset the start/stop buttons
  useEffect(() => {
    fetch(`${API_URL}/api/server/status`)
    .then((res) => res.json())
    .then((data) => setServerStatus(data.status))
    .catch((err) => console.error("Error fetching server status:", err));
  }, []);

  // fetch players on page load
  useEffect(() => {
    fetch(`${API_URL}/api/server/players`)
    .then((res) => res.json())
    .then((data) => setPlayers(data.players || []))
    .catch((err) => console.error("Error fetching player list:", err));
  }, []);

  // react to SignalR events relating to players
  useEffect(() => {
    if (!connection) return;

    connection.on("PlayerJoined", (username: string) => {
      setPlayers((prev) => [...new Set([...prev, username])]);
    });

    connection.on("PlayerLeft", (username: string) => {
      setPlayers((prev) => prev.filter((p) => p !== username));
    });

    connection.on("PlayerListUpdated", (newList: string[]) => {
      setPlayers(newList);
    });
    }, [connection]);

  // command & action Handlers
  const handleStart = async () => {
    try {
      const response = await fetch(`${API_URL}/api/server/start`, { method: 'POST' });
      const data = await response.json();

      switch (response.status){
        case 202: // accepted
          ConsoleLog("Starting server...")
          break;
        case 409:
          ConsoleLog("[HTTP 409] Server is already started.")
          break;
        default:
          ConsoleLog(`[HTTP ${response.status}] ${data.message}`);
          break;
      }

    } catch (err) {
      console.error('Error starting server:', err);
    }
  };

  const handleStop = async () => {
    try {
      const response = await fetch(`${API_URL}/api/server/stop`, { method: 'POST' });
      const data = await response.json();

      switch (response.status){
        case 202: // accepted
          ConsoleLog("Stopping server...")
          break;
        case 409:
          ConsoleLog("[HTTP 409] Server is not running.")
          break;
        default:
          ConsoleLog(`[HTTP ${response.status}] ${data.message}`);
          break;
      }
    } catch (err) {
      console.error('Error stopping server:', err);
    }
  };

  const handleSendCommand = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!commandInput.trim() || !connection) return;

    try {
      await connection.invoke('SendServerCommand', commandInput);
      setCommandInput('');
    } catch (err) {
      console.error('Error sending command:', err);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 p-6 flex flex-col font-sans">
      {/* Top Header / Control Bar */}
      <header className="flex flex-col sm:flex-row justify-between items-start sm:items-center pb-6 mb-6 border-b border-slate-800 gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <Terminal className="text-emerald-400" /> Minecraft Server Console
          </h1>
          <p className="text-xs text-slate-400 mt-1 flex items-center gap-2">
            SignalR Status: 
            <span className={isConnected ? "text-emerald-400 font-semibold" : "text-rose-400 font-semibold"}>
              {isConnected ? 'Online' : 'Disconnected'}
            </span>
          </p>
          <p className="text-xs text-slate-400 mt-1 flex items-center gap-2">
            Server Status: 
            <span className={isConnected ? "text-emerald-400 font-semibold" : "text-rose-400 font-semibold"}>
              {serverStatus}
            </span>
          </p>
        </div>

        {/* Start / Stop Actions */}
        <div className="flex items-center gap-3">
          <button
            onClick={handleStart}
            disabled={isServerRunning(serverStatus)}
            className="flex items-center gap-1.5 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-40 disabled:hover:bg-emerald-600 text-white font-medium rounded-lg text-sm transition cursor-pointer disabled:cursor-not-allowed"
          >
            <Play size={16} /> Start
          </button>
          <button
            onClick={handleStop}
            disabled={!isServerRunning(serverStatus)}
            className="flex items-center gap-1.5 px-4 py-2 bg-rose-600 hover:bg-rose-500 disabled:opacity-40 disabled:hover:bg-rose-600 text-white font-medium rounded-lg text-sm transition cursor-pointer disabled:cursor-not-allowed"
          >
            <Square size={16} /> Stop
          </button>
        </div>
      </header>

      {/* Main Terminal Box */}
      <main className="flex-1 flex flex-col bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-2xl">
        <div className="bg-slate-900/80 px-4 py-2.5 border-b border-slate-800 text-xs text-slate-400 font-mono flex justify-between items-center">
          <span>LIVE CONSOLE OUTPUT</span>
          <span>Status: <strong className="text-emerald-400">{serverStatus}</strong></span>
        </div>

        {/* Scrollable Logs */}
        <div className="flex-1 p-4 font-mono text-xs sm:text-sm overflow-y-auto space-y-1 max-h-[60vh] min-h-[400px]">
          {logs.length === 0 ? (
            <div className="text-slate-600 italic">No console logs received yet. Click 'Start' to launch the process...</div>
          ) : (
            logs.map((log) => (
              <div key={log.id} className="leading-relaxed hover:bg-slate-800/40 px-1 rounded flex gap-3">
                <span className="text-slate-500 select-none">{log.timestamp}</span>
                <span className="text-slate-200 whitespace-pre-wrap">{log.text}</span>
              </div>
            ))
          )}
          <div ref={terminalEndRef} />
        </div>

        {/* Command Input Form */}
        <form onSubmit={handleSendCommand} className="border-t border-slate-800 p-2 bg-slate-950/60 flex items-center gap-2">
          <span className="text-emerald-400 font-mono pl-2">{'>'}</span>
          <input
            type="text"
            value={commandInput}
            onChange={(e) => setCommandInput(e.target.value)}
            disabled={!isConnected}
            placeholder="Type server command (e.g. list, say Hello)..."
            className="flex-1 bg-transparent text-sm font-mono text-slate-100 focus:outline-none px-2 py-1 placeholder:text-slate-600 disabled:opacity-50"
          />
          <button
            type="submit"
            disabled={!isConnected || !commandInput.trim()}
            className="px-4 py-1.5 bg-slate-800 hover:bg-slate-700 disabled:opacity-50 text-slate-200 font-mono text-xs rounded transition"
          >
            Send
          </button>
        </form>

        {/* Online Players Sidebar*/}
        <main className="bg-slate-900 border border-slate-800 rounded-xl p-4 flex flex-col shadow-2xl">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-4">
            <h2 className="text-sm font-bold text-slate-200 flex items-center gap-2">
              <Users size={18} className="text-emerald-400" /> Online Players
            </h2>
            <span className="bg-slate-800 text-slate-300 text-xs px-2 py-0.5 rounded-full font-mono">
              {players.length}
            </span>
          </div>

          <div className="flex-1 overflow-y-auto space-y-2">
            {players.length === 0 ? (
              <p className="text-xs text-slate-500 italic">No players online.</p>
            ) : (
              players.map((player) => (
                <div key={player} className="flex items-center gap-2.5 p-2 bg-slate-950/50 border border-slate-800/80 rounded-lg text-sm text-slate-200">
                  {/* Player avatar using Visage API */}
                  {/* <img
                    src={`https://visage.surgeplay.com/face/24/${player}`}
                    alt={player}
                    className="w-6 h-6 rounded bg-slate-800"
                    onError={(e) => { (e.target as HTMLElement).style.display = 'none'; }}
                  /> */}
                  <span className="font-mono text-xs font-medium">{player}</span>
                </div>
              ))
            )}
          </div>
        </main>
      </main>
    </div>
  );
}