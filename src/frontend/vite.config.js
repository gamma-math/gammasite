import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

export default defineConfig({
  plugins: [react()],
  base: "/react-assets/",
  build: {
    outDir: path.resolve(__dirname, "../backend/GamMaSite/wwwroot/react-assets"),
    emptyOutDir: true,
    rollupOptions: {
      output: {
        entryFileNames: "app.js",
        chunkFileNames: "chunks/[name].js",
        assetFileNames: "app.css"
      }
    }
  },
  server: {
    proxy: {
      "/api": "https://localhost:5001",
      "/Identity": "https://localhost:5001"
    }
  }
});
