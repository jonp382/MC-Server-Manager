import React from 'react';
import { Users } from 'lucide-react';

interface PlayerListProps {
    players: string[];
}

{/* Online Players List*/ }
export const PlayerList: React.FC<PlayerListProps> = ({ players }) => {
    return (
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
    );
};