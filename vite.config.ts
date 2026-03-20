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
      },
    },
  },
});
