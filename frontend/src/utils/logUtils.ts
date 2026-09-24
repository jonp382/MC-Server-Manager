import React from 'react';

export const isErrorLog = (text: string) =>
    text.includes('ERROR]:') ||
    text.startsWith('[SYSTEM ERROR]') ||
    text.startsWith('[COMMAND ERROR]') ||
    text.includes('Exception in thread');

export const isWarnLog = (text: string) =>
    text.includes('WARN]:') || text.includes('WARNING');

export const isInfoLog = (text: string) =>
    !isErrorLog(text) && !isWarnLog(text);

// helper func to determine text color based on Minecraft log format
export const getLogLineStyle = (text: string): React.CSSProperties => {
    if (
        isErrorLog(text)
    ) {
        return { color: '#f43f5e', fontWeight: 600 }; // Rose-500 Red
    }

    if (isWarnLog(text)) {
        return { color: '#fbbf24', fontWeight: 600 }; // Amber-400 Yellow
    }

    if (text.includes('joined the game') || text.includes('left the game')) {
        return { color: '#34d399', fontWeight: 600 }; // Emerald-400 Green
    }

    if (text.startsWith('[SYSTEM]')) {
        return { color: '#22d3ee', fontStyle: 'italic' }; // Cyan-400 Blue
    }

    if (isInfoLog(text)) {
        return { color: '#e2e8f0' }; // Slate-200 Off-White
    }

    return { color: '#94a3b8' }; // Slate-400 Neutral Gray
};