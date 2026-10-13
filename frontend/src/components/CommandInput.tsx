import React, { useState } from 'react';
import { sendCommand } from '../services/API'

interface CommandInputProps {
    onCommandExecution: (errorMessage: string) => void;
}

export const CommandInput: React.FC<CommandInputProps> = ({
    onCommandExecution,
}) => {
    // command history for up/down arrow
    const [history, setHistory] = useState<string[]>([]); // the list of commands
    const [historyIndex, setHistoryIndex] = useState<number>(-1); // which previous command is selected
    const [tempInput, setTempInput] = useState<string>(''); // the current input

    // the command input string
    const [commandInput, setCommandInput] = useState('');


    const handleSendCommand = async (e: React.FormEvent) => {
        e.preventDefault();
        const trim = commandInput.trim();

        try {
            // send command to back end via API post call
            await sendCommand(onCommandExecution, trim);

            // add command to history
            setHistory((prev) => (prev[prev.length - 1] === commandInput ? prev : [...prev, commandInput]));

            setCommandInput('');
            setHistoryIndex(-1);
            setTempInput('');
        } catch (err: any) {
            onCommandExecution(err.Message || 'Failed to send command');
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (history.length === 0) return;

        if (e.key === 'ArrowUp') {
            e.preventDefault(); // prevents cursor moving to start of input field

            // save current unsent input draft before entering history navigation
            if (historyIndex === -1) {
                setTempInput(commandInput);
            }

            const nextIndex = historyIndex === -1 ? history.length - 1 : Math.max(0, historyIndex - 1);
            setHistoryIndex(nextIndex);
            setCommandInput(history[nextIndex]);
        } else if (e.key === 'ArrowDown') {
            e.preventDefault();

            if (historyIndex === -1) return;

            const nextIndex = historyIndex + 1;

            if (nextIndex >= history.length) {
                // reached bottom of history, restore saved draft
                setHistoryIndex(-1);
                setCommandInput(tempInput);
            } else {
                setHistoryIndex(nextIndex);
                setCommandInput(history[nextIndex]);
            }
        }
    };

    return (
        <form onSubmit={handleSendCommand} className="border-t border-slate-800 p-2 bg-slate-950 flex items-center gap-2">
            <span className="text-emerald-400 font-mono pl-2">{'>'}</span>
            <input
                type="text"
                value={commandInput}
                onChange={(e) => {
                    setCommandInput(e.target.value);
                    if (historyIndex !== -1) setHistoryIndex(-1);
                }}
                onKeyDown={handleKeyDown}
                placeholder="Type server command (Press ↑/↓ for history)..."
                style={{ color: '#ffffff', backgroundColor: '#020617' }}
                className="flex-1 text-white text-sm font-mono focus:outline-none px-2 py-1 placeholder:text-slate-400 disabled:opacity-50"
            />
            <button
                type="submit"
                className="px-4 py-1.5 bg-slate-800 hover:bg-slate-700 disabled:opacity-50 text-slate-200 font-mono text-xs rounded transition"
            >
                Send
            </button>
        </form>
    );

};
