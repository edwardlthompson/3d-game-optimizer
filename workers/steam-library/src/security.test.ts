import { describe, expect, it } from "vitest";
import { corsOrigin, isAllowedOrigin } from "./security";

const env = {
  ALLOWED_ORIGIN: "https://edwardlthompson.github.io",
  CATALOG_RETURN_URL: "https://edwardlthompson.github.io/3d-game-optimizer/catalog/",
  STEAM_WEB_API_KEY: "test",
} as const;

describe("isAllowedOrigin", () => {
  it("accepts configured origin", () => {
    expect(isAllowedOrigin("https://edwardlthompson.github.io", env)).toBe(true);
  });

  it("rejects missing origin", () => {
    expect(isAllowedOrigin(null, env)).toBe(false);
  });

  it("rejects foreign origin", () => {
    expect(isAllowedOrigin("https://evil.example", env)).toBe(false);
  });
});

describe("corsOrigin", () => {
  it("falls back to allowed origin when absent", () => {
    expect(corsOrigin(null, env)).toBe(env.ALLOWED_ORIGIN);
  });

  it("rejects foreign origin in ACAO", () => {
    expect(corsOrigin("https://evil.example", env)).toBe(env.ALLOWED_ORIGIN);
  });
});
