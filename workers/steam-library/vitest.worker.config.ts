import path from "node:path";
import { fileURLToPath } from "node:url";
import { cloudflareTest } from "@cloudflare/vitest-pool-workers";
import { defineConfig } from "vitest/config";

const root = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [
    cloudflareTest({
      wrangler: { configPath: path.join(root, "wrangler.toml"), environment: "vitest" },
    }),
  ],
  test: {
    include: ["src/**/*.worker.test.ts"],
    deps: {
      optimizer: {
        ssr: {
          include: [/vitest/, /expect-type/],
        },
      },
    },
  },
});
