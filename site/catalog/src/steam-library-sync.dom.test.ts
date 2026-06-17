// @vitest-environment happy-dom
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { loadLibrary, loadSteamMeta } from "./library";
import { handleSteamSyncReturn } from "./steam-library-sync";
import type { CatalogGame } from "./types";

function game(partial: Partial<CatalogGame> & { id: string; title: string }): CatalogGame {
  return {
    bestLevel: "experimental3d",
    bestExperience: { platformKey: "manual", label: "Test", level: "experimental3d" },
    platformSupport: [],
    sources: [],
    ...partial,
  } as CatalogGame;
}

describe("handleSteamSyncReturn", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.stubEnv("VITE_STEAM_SYNC_URL", "https://worker.test");
    window.history.replaceState({}, "", "/catalog/");
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  it("returns early when the URL has no sync params", async () => {
    const result = await handleSteamSyncReturn([], "merge");
    expect(result).toEqual({ stats: null, error: null, emptyLibrary: false });
  });

  it("strips error param and returns a user-facing message", async () => {
    window.history.replaceState({}, "", "/catalog/?error=openid_failed");
    const result = await handleSteamSyncReturn([], "merge");
    expect(result.error).toBe("Steam sign-in failed.");
    expect(result.stats).toBeNull();
    expect(window.location.pathname).toBe("/catalog/");
    expect(window.location.search).toBe("");
  });

  it("maps steam_api_failed to a specific message", async () => {
    window.history.replaceState({}, "", "/catalog/?error=steam_api_failed");
    const result = await handleSteamSyncReturn([], "merge");
    expect(result.error).toBe("Steam library API is unavailable. Try again later.");
    expect(window.location.search).toBe("");
  });

  it("preserves appId query param when clearing steam sync token from hash", async () => {
    window.history.replaceState({}, "", "/catalog/?appId=570#steam_sync_token=sync-tok");
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            appIds: [570],
            steamIdTruncated: "7656…01",
            emptyLibrary: false,
            source: "openid",
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      ),
    );

    await handleSteamSyncReturn(
      [game({ id: "dota", title: "Dota 2", steamAppId: 570, steamMatchConfidence: 0.95 })],
      "merge",
    );
    expect(window.location.search).toBe("?appId=570");
  });

  it("exchanges token from URL hash, merges library, saves meta, and strips the URL", async () => {
    const games = [
      game({
        id: "dota",
        title: "Dota 2",
        steamAppId: 570,
        steamMatchConfidence: 0.95,
      }),
    ];
    window.history.replaceState({}, "", "/catalog/#steam_sync_token=sync-tok");

    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            appIds: [570],
            steamIdTruncated: "7656…01",
            emptyLibrary: false,
            source: "openid",
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      ),
    );

    const result = await handleSteamSyncReturn(games, "merge");
    expect(result.error).toBeNull();
    expect(result.emptyLibrary).toBe(false);
    expect(result.stats?.catalogMatched).toBe(1);
    expect(loadLibrary().has("dota")).toBe(true);
    expect(loadSteamMeta().steamId).toBe("7656…01");
    expect(loadSteamMeta().lastSyncAt).toBeTruthy();
    expect(window.location.search).toBe("");
    expect(window.location.hash).toBe("");
    expect(fetch).toHaveBeenCalledWith(
      "https://worker.test/sync/exchange",
      expect.objectContaining({ method: "POST" }),
    );
  });

  it("replaces library when mode is replace", async () => {
    localStorage.setItem("3d-catalog-library-v1", JSON.stringify(["legacy-game"]));
    const games = [
      game({
        id: "dota",
        title: "Dota 2",
        steamAppId: 570,
        steamMatchConfidence: 0.95,
      }),
    ];
    window.history.replaceState({}, "", "/catalog/#steam_sync_token=sync-tok");

    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            appIds: [570],
            steamIdTruncated: "7656…01",
            emptyLibrary: false,
            source: "openid",
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      ),
    );

    const result = await handleSteamSyncReturn(games, "replace");
    expect(result.stats?.catalogMatched).toBe(1);
    const library = loadLibrary();
    expect(library.has("dota")).toBe(true);
    expect(library.has("legacy-game")).toBe(false);
  });

  it("still accepts legacy query-string sync tokens", async () => {
    window.history.replaceState({}, "", "/catalog/?steam_sync_token=legacy-tok");
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            appIds: [],
            steamIdTruncated: "7656…01",
            emptyLibrary: true,
            source: "openid",
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      ),
    );

    const result = await handleSteamSyncReturn([], "merge");
    expect(result.error).toBeNull();
    expect(window.location.search).toBe("");
  });

  it("returns an error when token exchange fails", async () => {
    window.history.replaceState({}, "", "/catalog/#steam_sync_token=bad-tok");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("", { status: 410 })));

    const result = await handleSteamSyncReturn([], "merge");
    expect(result.stats).toBeNull();
    expect(result.error).toBe("Could not complete Steam library sync.");
    expect(window.location.search).toBe("");
  });
});
