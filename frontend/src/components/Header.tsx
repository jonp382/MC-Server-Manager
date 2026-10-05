import React from 'react';
import { Play, Square } from 'lucide-react';
import { SettingsModal } from './SettingsModal';
import { ServerInstance } from '../services/API';

interface HeaderProps {
    instances: ServerInstance[];
    activeId: string;
    onSelect: (id: string) => void;
    serverStatus: string;
    isConnected: boolean;
    onStart: () => void;
    onStop: () => void;
    onSaveSettings: (instance: ServerInstance) => void;
}

export const Header: React.FC<HeaderProps> = ({
    instances,
    activeId,
    onSelect,
    serverStatus,
    isConnected,
    onStart,
    onStop,
    onSaveSettings,
}) => {
    const active = instances.find((i) => i.id === activeId) ?? null;
    const isRunning = serverStatus === 'Running';
    const isStopped = serverStatus === 'Stopped';

    return (
        <header className="flex flex-col sm:flex-row justify-between items-start sm:items-center pb-6 mb-6 border-b border-slate-800 gap-4">
            <div>
                <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
                    Minecraft Server Console
                </h1>
                <p className="text-xs text-slate-400 mt-1 flex items-center gap-2">
                    SignalR Status:
                    <span className={isConnected ? "text-emerald-400 font-semibold" : "text-rose-400 font-semibold"}>
                        {isConnected ? 'Online' : 'Disconnected'}
                    </span>
                </p>
                <p className="text-xs text-slate-400 mt-1 flex items-center gap-2">
                    Server Status:
                    <span className={isRunning ? "text-emerald-400 font-semibold" : "text-rose-400 font-semibold"}>
                        {serverStatus}
                    </span>
                </p>
                {active && (
                    <p className="text-xs text-slate-400 mt-1 flex items-center gap-2">
                        Targeting:
                        <span className="text-slate-200 font-semibold font-mono">
                            {active.ip}:{active.queryPort}
                        </span>
                    </p>
                )}
            </div>

            {/* server selector + start/stop + settings */}
            <div className="flex items-center gap-3">
                <select
                    value={activeId}
                    onChange={(e) => onSelect(e.target.value)}
                    className="bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-100 text-xs font-mono focus:outline-none focus:border-slate-600"
                >
                    {instances.length === 0 && <option value="">no servers</option>}
                    {instances.map((i) => (
                        <option key={i.id} value={i.id}>{i.name}</option>
                    ))}
                    <option value="">+ add server</option>
                </select>

                <button
                    onClick={onStart}
                    disabled={!isStopped || !isConnected || !active}
                    className="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-40 disabled:hover:bg-emerald-600 text-white font-medium text-xs sm:text-sm rounded-lg transition shadow-lg shadow-emerald-950/40"
                >
                    <Play size={16} fill="currentColor" />
                    Start Server
                </button>

                <button
                    onClick={onStop}
                    disabled={!isRunning || !isConnected || !active}
                    className="flex items-center gap-2 px-4 py-2 bg-rose-600 hover:bg-rose-500 disabled:opacity-40 disabled:hover:bg-rose-600 text-white font-medium text-xs sm:text-sm rounded-lg transition shadow-lg shadow-rose-950/40"
                >
                    <Square size={16} fill="currentColor" />
                    Stop Server
                </button>

                {/* settings button, add mode when no server is active */}
                <SettingsModal instance={active} onSave={onSaveSettings} />

            </div>
        </header>
    );
};
