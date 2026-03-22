import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
  base: process.env.GITHUB_ACTIONS ? "./" : "/",
  root: ".",
  publicDir: "public",
  build: {
    outDir: "dist",
    rollupOptions: {
      input: {
        main: resolve(__dirname, "index.html"),
        trace: resolve(__dirname, "src/pages/trace.html"),
        queues: resolve(__dirname, "src/pages/queues.html"),
        databases: resolve(__dirname, "src/pages/databases.html"),
        logs: resolve(__dirname, "src/pages/logs.html"),
        operations: resolve(__dirname, "src/pages/operations.html"),
        admin: resolve(__dirname, "src/pages/admin.html"),
      },
    },
  },
  server: {
    proxy: {
      "/api": {
        target: "http://localhost:5000",
        changeOrigin: true,
      },
    },
  },
});
