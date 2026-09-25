import React, { useState, useRef, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { Terminal, RefreshCw } from 'lucide-react';
import { CommandInput } from './CommandInput';
import { getLogLineStyle, isInfoLog, isWarnLog, isErrorLog } from '../utils/logUtils';

export interface LogMessage {
    id: string;
    text: string;
    timestamp: string;
}

interface TerminalConsoleProps {
    logs: LogMessage[];
    connection: signalR.HubConnection | null;
    isConnected: boolean;
    onCommandError: (errorMessage: string) => void;
    onClearLogs?: () => void;
    onRetrieveLogs: () => void;
}

export const TerminalConsole: React.FC<TerminalConsoleProps> = ({
    logs,
    connection,
    isConnected,
    onCommandError,
    onClearLogs,
    onRetrieveLogs,
}) => {
    const [logFilter, setLogFilter] = useState<'ALL' | 'INFO' | 'WARN' | 'ERROR'>('ALL');
    const terminalEndRef = useRef<HTMLDivElement | null>(null);

    // Auto-scroll to bottom as new logs arrive
    useEffect(() => {
        terminalEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [logs, logFilter]);

    // Compute category counts
    const counts = {
        ALL: logs.length,
        INFO: logs.filter((l) => isInfoLog(l.text)).length,
        WARN: logs.filter((l) => isWarnLog(l.text)).length,
        ERROR: logs.filter((l) => isErrorLog(l.text)).length,
    };

    // Derive filtered list
    const filteredLogs = logs.filter((log) => {
        if (logFilter === 'ERROR') return isErrorLog(log.text);
        if (logFilter === 'WARN') return isWarnLog(log.text);
        if (logFilter === 'INFO') return isInfoLog(log.text);
        return true; // ALL
    });

    return (
        <div className="flex-1 flex flex-col bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-2xl h-full min-h-0">
            {/* title */}
            <div className="bg-slate-900/90 px-4 py-2.5 border-b border-slate-800 flex flex-wrap items-center justify-between gap-2">
                    <span className="text-xs text-slate-400 font-mono flex items-center gap-1.5 font-semibold">
                        <Terminal size={14} className="text-emerald-400" />
                        LIVE CONSOLE
                    </span>
            </div>
            
            {/* Button row */}
            <div>
                    <div className="flex items-center gap-3 font-mono text-xs">
                        <button
                            onClick={() => setLogFilter('ALL')}
                            className={`px-2 py-0.5 rounded transition ${logFilter === 'ALL'
                                ? 'bg-slate-700 text-white font-bold'
                                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
                                }`}
                        >
                            ALL <span className="opacity-70">({counts.ALL})</span>
                        </button>

                        <button
                            onClick={() => setLogFilter('INFO')}
                            className={`px-2 py-0.5 rounded transition ${logFilter === 'INFO'
                                ? 'bg-sky-950 text-sky-300 border border-sky-800 font-bold'
                                : 'text-slate-400 hover:text-sky-300 hover:bg-slate-800'
                                }`}
                        >
                            INFO <span className="opacity-70">({counts.INFO})</span>
                        </button>

                        <button
                            onClick={() => setLogFilter('WARN')}
                            className={`px-2 py-0.5 rounded transition ${logFilter === 'WARN'
                                ? 'bg-amber-950 text-amber-300 border border-amber-800 font-bold'
                                : 'text-slate-400 hover:text-amber-300 hover:bg-slate-800'
                                }`}
                        >
                            WARN <span className="opacity-70">({counts.WARN})</span>
                        </button>

                        <button
                            onClick={() => setLogFilter('ERROR')}
                            className={`px-2 py-0.5 rounded transition ${logFilter === 'ERROR'
                                ? 'bg-rose-950 text-rose-300 border border-rose-800 font-bold'
                                : 'text-slate-400 hover:text-rose-300 hover:bg-slate-800'
                                }`}
                        >
                            ERROR <span className="opacity-70">({counts.ERROR})</span>
                        </button>
                    </div>
                </div>

                {/* sync and clear log buttons */}
                <div className="flex items-center gap-3 font-mono text-xs">
                    <button
                        onClick={onRetrieveLogs}
                        title="Sync Logs"
                        className="text-xs text-slate-500 hover:text-slate-300 transition flex items-center gap-1"
                    >
                        <RefreshCw size={12} /> Sync Logs
                    </button>

                    <button
                            onClick={onClearLogs}
                            title="Clear Console Output"
                            className="text-xs text-slate-500 hover:text-slate-300 transition flex items-center gap-1"
                        >
                            <RefreshCw size={12} /> Clear
                        </button>
                </div>


            {/* Scrollable Terminal */}
            <div className="flex-1 p-4 font-mono text-xs sm:text-sm overflow-y-auto space-y-1 max-h-[60vh] min-h-[400px] bg-slate-950/40">
                {filteredLogs.length === 0 ? (
                    <div className="text-slate-600 italic">
                        {logs.length === 0
                            ? "No console logs received yet. Click 'Start Server' to launch..."
                            : `No logs matching filter '${logFilter}'.`}
                    </div>
                ) : (
                    filteredLogs.map((log) => (
                        <div
                            key={log.id}
                            className="leading-relaxed hover:bg-slate-800/40 px-1 rounded flex gap-3"
                        >
                            <span className="text-slate-500 select-none shrink-0 font-mono text-xs pt-0.5">
                                {log.timestamp}
                            </span>
                            <span
                                className="whitespace-pre-wrap break-all"
                                style={getLogLineStyle(log.text)}
                            >
                                {log.text}
                            </span>
                        </div>
                    ))
                )}
                <div ref={terminalEndRef} />
            </div>

            {/* Terminal Input Form */}
            <CommandInput
                connection={connection}
                isConnected={isConnected}
                onCommandError={onCommandError}
            />
        </div>
    );
};