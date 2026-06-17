import { describe, expect, it, vi, afterEach } from "vitest";
import {
  isSteamSyncEnabled,
  mapOwnedAppIdsToCatalogIds,
  steamSyncWorkerUrl,
} from "./steam-library-sync";
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

describe("steamSyncWorkerUrl", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("returns empty when unset", () => {
    vi.stubEnv("VITE_STEAM_SYNC_URL", "");
    expect(steamSyncWorkerUrl()).toBe("");
    expect(isSteamSyncEnabled()).toBe(false);
  });

  it("strips trailing slash", () => {
    vi.stubEnv("VITE_STEAM_SYNC_URL", "https://steam.example.workers.dev/");
    expect(steamSyncWorkerUrl()).toBe("https://steam.example.workers.dev");
    expect(isSteamSyncEnabled()).toBe(true);
  });
});

describe("mapOwnedAppIdsToCatalogIds", () => {
  const games: CatalogGame[] = [
    game({
      id: "a",
      title: "Linked A",
      steamAppId: 570,
      steamMatchConfidence: 0.95,
    }),
    game({
      id: "b",
      title: "Low confidence",
      steamAppId: 730,
      steamMatchConfidence: 0.5,
    }),
    game({
      id: "c",
      title: "No steam id",
      steamMatchConfidence: 0.99,
    }),
  ];

  it("matches owned app ids above confidence threshold", () => {
    const { matchedIds, stats } = mapOwnedAppIdsToCatalogIds(games, [570, 999]);
    expect(matchedIds).toEqual(["a"]);
    expect(stats.catalogMatched).toBe(1);
    expect(stats.ownedTotal).toBe(2);
    expect(stats.ownedUnmatched).toBe(1);
    expect(stats.catalogNoSteamLink).toBe(2);
  });

  it("returns empty matches for empty library", () => {
    const { matchedIds, stats } = mapOwnedAppIdsToCatalogIds(games, []);
    expect(matchedIds).toEqual([]);
    expect(stats.ownedTotal).toBe(0);
    expect(stats.catalogMatched).toBe(0);
  });
});
