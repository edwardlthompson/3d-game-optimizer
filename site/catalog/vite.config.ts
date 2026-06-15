import { defineConfig } from "vite";

export default defineConfig({
  base: process.env.VITE_BASE_PATH || "/catalog/",
  build: {
    outDir: "dist",
    emptyOutDir: true,
  },
});
