import React, { useState } from 'react';
import { Settings, X, Radio, ShieldCheck, Globe, EyeOff, Eye, Folder } from 'lucide-react';
import { ServerInstance } from '../services/API';

interface SettingsModalProps {
  instance: ServerInstance | null; // null = add a new server
  onSave: (instance: ServerInstance) => void;
}

const blank: ServerInstance = {
  id: '',
  name: '',
  serverDir: '',
  ip: '127.0.0.1',
  queryPort: 25565,
  rconPort: 25575,
  rconPassword: '',
};

// one editor for both add and edit. add gets a blank template, edit gets the active instance.
export const SettingsModal: React.FC<SettingsModalProps> = ({ instance, onSave }) => {
  const isAdd = instance === null;
  const [isOpen, setIsOpen] = useState(false);
  const [formState, setFormState] = useState<ServerInstance>(instance ?? blank);
  const [showRconPassword, setShowRconPassword] = useState(false);

  const open = () => {
    setFormState(instance ?? blank);
    setIsOpen(true);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(formState);
    setIsOpen(false);
  };

  return (
    <>
      {/* settings button */}
      <button
        onClick={open}
        className="p-2 bg-slate-900 hover:bg-slate-800 text-slate-300 border border-slate-800 rounded-lg transition-colors flex items-center gap-2 text-xs font-mono"
        title={isAdd ? 'Add Server' : 'Edit Server'}
      >
        <Settings size={16} className="text-slate-400 hover:rotate-45 transition-transform duration-300" />
        <span className="hidden sm:inline">{isAdd ? 'Add Server' : 'Settings'}</span>
      </button>

      {/* modal window */}
      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-md p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-xl shadow-2xl w-full max-w-md overflow-hidden animate-in fade-in zoom-in-95 duration-150">

            <div className="bg-slate-900 px-5 py-4 border-b border-slate-800 flex items-center justify-between">
              <h3 className="text-sm font-bold text-slate-200 flex items-center gap-2">
                <Settings size={18} className="text-emerald-400" />
                {isAdd ? 'Add Server' : 'Edit Server'}
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

              {/* name */}
              <div>
                <label className="block text-slate-400 mb-1 flex items-center gap-1.5">
                  <Globe size={14} className="text-violet-400" /> Name
                </label>
                <input
                  type="text"
                  value={formState.name}
                  onChange={(e) => setFormState({ ...formState, name: e.target.value })}
                  placeholder="My Server"
                  required
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-100 placeholder-slate-600 focus:outline-none focus:border-slate-600"
                />
              </div>

              {/* server directory */}
              <div>
                <label className="block text-slate-400 mb-1 flex items-center gap-1.5">
                  <Folder size={14} className="text-sky-400" /> Server Directory
                </label>
                <input
                  type="text"
                  value={formState.serverDir}
                  onChange={(e) => setFormState({ ...formState, serverDir: e.target.value })}
                  placeholder="/home/user/minecraft"
                  required
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-100 placeholder-slate-600 focus:outline-none focus:border-slate-600"
                />
                <p className="text-slate-500 mt-1">where server.jar and logs/ live. used for start and log tailing</p>
              </div>

              {/* server ip */}
              <div>
                <label className="block text-slate-400 mb-1 flex items-center gap-1.5">
                  <Globe size={14} className="text-violet-400" /> Server IP
                </label>
                <input
                  type="text"
                  value={formState.ip}
                  onChange={(e) => setFormState({ ...formState, ip: e.target.value })}
                  placeholder="127.0.0.1"
                  required
                  pattern="^[A-Za-z0-9.\-:]+$"
                  title="ip address or hostname, e.g. 127.0.0.1, localhost, mc.example.com"
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-100 placeholder-slate-600 focus:outline-none focus:border-slate-600"
                />
                <p className="text-slate-500 mt-1">where the server lives. defaults to 127.0.0.1</p>
              </div>

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
                  {isAdd ? 'Add Server' : 'Save'}
                </button>
              </div>

            </form>

          </div>
        </div>
      )}
    </>
  );
};
