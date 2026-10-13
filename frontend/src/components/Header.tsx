import React from 'react';
import { Play, Square } from 'lucide-react';
import { SettingsModal } from './SettingsModal';
import { ServerInstance } from '../services/API';

interface HeaderProps {
    instance: ServerInstance | null;
    serverStatus: string;
    isConnected: boolean;
    onStart: () => void;
    onStop: () => void;
    onSaveSettings: (instance: ServerInstance) => void;
}

export const Header: React.FC<HeaderProps> = ({
    instance,
    serverStatus,
    isConnected,
    onStart,
    onStop,
    onSaveSettings,
}) => {
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
                {instance && (
                    <p className="text-xs text-slate-400 mt-1 flex items-center gap-2">
                        Targeting:
                        <span className="text-slate-200 font-semibold font-mono">
                            {instance.ip}:{instance.queryPort}
                        </span>
                    </p>
                )}
            </div>

            {/* start/stop + settings for the single server */}
            <div className="flex items-center gap-3">
                {/*
                <button
                    onClick={onStart}
                    disabled={!isStopped || !isConnected || !instance}
                    className="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-40 disabled:hover:bg-emerald-600 text-white font-medium text-xs sm:text-sm rounded-lg transition shadow-lg shadow-emerald-950/40"
                >
                    <Play size={16} fill="currentColor" />
                    Start Server
                </button>

                <button
                    onClick={onStop}
                    disabled={!isRunning || !isConnected || !instance}
                    className="flex items-center gap-2 px-4 py-2 bg-rose-600 hover:bg-rose-500 disabled:opacity-40 disabled:hover:bg-rose-600 text-white font-medium text-xs sm:text-sm rounded-lg transition shadow-lg shadow-rose-950/40"
                >
                    <Square size={16} fill="currentColor" />
                    Stop Server
                </button>
                */}

                <SettingsModal instance={instance} onSave={onSaveSettings} />

            </div>
        </header>
    );
};
