import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // dev: vite forwards api and signalr to the backend, same relative
    // urls the nginx container uses in production
    proxy: {
      '/api': { target: 'http://localhost:5029', changeOrigin: true },
      '/hubs': { target: 'http://localhost:5029', changeOrigin: true, ws: true },
    },
  },
})
