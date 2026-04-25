import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { viteStaticCopy } from 'vite-plugin-static-copy'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    // Copy the TinyMCE self-hosted bundle into the build output.
    // TinyMCE loads its skins/plugins/themes dynamically at runtime via URL,
    // so they cannot be bundled — they must be present as static files.
    // https://www.tiny.cloud/docs/tinymce/latest/npm-projects/
    viteStaticCopy({
      targets: [
        {
          src: 'node_modules/tinymce/tinymce.min.js',
          dest: 'libs/tinymce',
        },
        {
          src: 'node_modules/tinymce/skins',
          dest: 'libs/tinymce',
        },
        {
          src: 'node_modules/tinymce/plugins',
          dest: 'libs/tinymce',
        },
        {
          src: 'node_modules/tinymce/themes',
          dest: 'libs/tinymce',
        },
        {
          src: 'node_modules/tinymce/icons',
          dest: 'libs/tinymce',
        },
        {
          src: 'node_modules/tinymce/models',
          dest: 'libs/tinymce',
        },
      ],
    }),
  ],

  // Assets are served from /spa/ by the .NET static files middleware
  base: '/spa/',

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
