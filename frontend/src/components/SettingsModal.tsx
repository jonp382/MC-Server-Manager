import React, { useState } from 'react';
import { Settings, X, Radio, ShieldCheck, EyeOff, Eye } from 'lucide-react';

export interface ServerSettings {
  queryPort: number;
  rconPort: number;
  rconPassword: string;
}

interface SettingsModalProps {
  settings: ServerSettings;
  onSave: (newSettings: ServerSettings) => void;
}

export const SettingsModal: React.FC<SettingsModalProps> = ({ settings, onSave }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [formState, setFormState] = useState<ServerSettings>(settings);
  const [showRconPassword, setShowRconPassword] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(formState);
    setIsOpen(false);
  };

  return (
    <>
      {/* settings button */}
      <button
        onClick={() => setIsOpen(true)}
        className="p-2 bg-slate-900 hover:bg-slate-800 text-slate-300 border border-slate-800 rounded-lg transition-colors flex items-center gap-2 text-xs font-mono"
        title="Server Settings"
      >
        <Settings size={16} className="text-slate-400 hover:rotate-45 transition-transform duration-300" />
        <span className="hidden sm:inline">Settings</span>
      </button>

      {/* modal window */}
      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-md p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-xl shadow-2xl w-full max-w-md overflow-hidden animate-in fade-in zoom-in-95 duration-150">
            
            <div className="bg-slate-900 px-5 py-4 border-b border-slate-800 flex items-center justify-between">
              <h3 className="text-sm font-bold text-slate-200 flex items-center gap-2">
                <Settings size={18} className="text-emerald-400" /> Connection Settings
              </h3>
              <button
                onClick={() => setIsOpen(false)}
                className="text-slate-400 hover:text-white transition-colors"
              >
                <X size={18} />
              </button>
            </div>

            {/* settings form */}
            <form onSubmit={handleSubmit} className="p-5 space-y-4 text-xs font-mono">
              
              {/* query port */}
              <div>
                <label className="block text-slate-400 mb-1 flex items-center gap-1.5">
                  <Radio size={14} className="text-sky-400" /> Query Port (UDP)
                </label>
                <input
                  type="number"
                  value={formState.queryPort}
                  onChange={(e) => setFormState({ ...formState, queryPort: Number(e.target.value) })}
                  placeholder="25565"
                  required
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-100 placeholder-slate-600 focus:outline-none focus:border-slate-600"
                />
              </div>

              {/* RCON port */}
              <div>
                <label className="block text-slate-400 mb-1 flex items-center gap-1.5">
                  <ShieldCheck size={14} className="text-emerald-400" /> RCON Port (TCP)
                </label>
                <input
                  type="number"
                  value={formState.rconPort}
                  onChange={(e) => setFormState({ ...formState, rconPort: Number(e.target.value) })}
                  placeholder="25575"
                  required
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-100 placeholder-slate-600 focus:outline-none focus:border-slate-600"
                />
              </div>

              {/* RCON password */}
              <div>
                <label className="block text-slate-400 mb-1 flex items-center gap-1.5">
                  <ShieldCheck size={14} className="text-emerald-400" /> RCON Password
                </label>
                <div className="relative">
                  <input
                    type={showRconPassword ? 'text' : 'password'}
                    value={formState.rconPassword}
                    onChange={(e) => setFormState({ ...formState, rconPassword: e.target.value })}
                    placeholder=""
                    required
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-100 placeholder-slate-600 focus:outline-none focus:border-slate-600"
                  />
                  <button
                    type="button"
                    onClick={() => setShowRconPassword((prev) => !prev)}
                    className="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200 transition-colors p-1 rounded-md focus:outline-none"
                    title={showRconPassword ? 'Hide password' : 'Show password'}
                  >
                    {showRconPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
              </div>

              {/* action buttons */}
              <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-800/80">
                <button
                  type="button"
                  onClick={() => setIsOpen(false)}
                  className="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg transition-colors font-medium"
                >
                  Save Connection Settings
                </button>
              </div>

            </form>

          </div>
        </div>
      )}
    </>
  );
};