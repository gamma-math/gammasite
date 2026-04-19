import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],

  // Production build outputs into the .NET wwwroot so dotnet publish picks it up
  build: {
    outDir: '../wwwroot/spa',
    emptyOutDir: true,
  },

  // In development, proxy /api and /Identity requests to the .NET backend
  // so the SPA and API share the same origin (no CORS, cookies work)
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:5001',
        secure: false,
        changeOrigin: true,
      },
      '/Identity': {
        target: 'https://localhost:5001',
        secure: false,
        changeOrigin: true,
      },
    },
  },
})
