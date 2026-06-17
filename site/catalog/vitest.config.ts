import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "node",
    include: ["src/**/*.test.ts", "src/**/*.dom.test.ts"],
    exclude: ["src/smoke-game-rank.test.ts"],
  },
});
