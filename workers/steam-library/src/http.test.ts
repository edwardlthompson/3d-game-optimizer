import { describe, expect, it } from "vitest";
import { redirectCatalog } from "./http";
import type { AllowedOriginEnv } from "./security";

const env: AllowedOriginEnv = {
  ALLOWED_ORIGIN: "https://edwardlthompson.github.io",
  CATALOG_RETURN_URL: "https://edwardlthompson.github.io/3d-game-optimizer/catalog/",
};

describe("redirectCatalog", () => {
  it("puts sync tokens in the URL fragment", () => {
    const resp = redirectCatalog(env, {}, { steam_sync_token: "abc-123" });
    expect(resp.status).toBe(302);
    const location = new URL(resp.headers.get("Location")!);
    expect(location.searchParams.get("steam_sync_token")).toBeNull();
    expect(new URLSearchParams(location.hash.slice(1)).get("steam_sync_token")).toBe("abc-123");
  });

  it("keeps error params in the query string", () => {
    const resp = redirectCatalog(env, { error: "openid_failed" });
    const location = new URL(resp.headers.get("Location")!);
    expect(location.searchParams.get("error")).toBe("openid_failed");
    expect(location.hash).toBe("");
  });
});
