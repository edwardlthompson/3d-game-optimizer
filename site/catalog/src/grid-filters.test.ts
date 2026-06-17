import { describe, expect, it } from "vitest";
import { matchesCatalogGlobalFilter } from "./grid-filters";
import type { CatalogGame } from "./types";

function game(partial: Partial<CatalogGame> & { id: string; title: string }): CatalogGame {
  return {
    bestLevel: "experimental3d",
    bestExperience: { platformKey: "manual", label: "Test", level: "experimental3d" },
    platformSupport: [],
    sources: [],
    hardwareRequirements: { displays: [] },
    ...partial,
  } as CatalogGame;
}

const baseCtx = {
  wishlist: new Set<string>(),
  library: new Set<string>(),
  options: {
    wishlistFilter: "all" as const,
    libraryFilter: "all" as const,
    ultraOnly: false,
    visionCertifiedOnly: false,
  },
  globalFilter: "",
};

describe("matchesCatalogGlobalFilter", () => {
  it("filters wishlist-only mode", () => {
    const g = game({ id: "a", title: "Alpha" });
    const ctx = { ...baseCtx, wishlist: new Set(["a"]), options: { ...baseCtx.options, wishlistFilter: "only" as const } };
    expect(matchesCatalogGlobalFilter(g, ctx)).toBe(true);
    expect(matchesCatalogGlobalFilter(game({ id: "b", title: "Beta" }), ctx)).toBe(false);
  });

  it("filters ultra-only mode", () => {
    const ultra = game({ id: "u", title: "Ultra", bestLevel: "ultra3d" });
    const legacy = game({ id: "l", title: "Legacy", bestLevel: "legacy3d" });
    const ctx = { ...baseCtx, options: { ...baseCtx.options, ultraOnly: true } };
    expect(matchesCatalogGlobalFilter(ultra, ctx)).toBe(true);
    expect(matchesCatalogGlobalFilter(legacy, ctx)).toBe(false);
  });

  it("filters vision-certified titles", () => {
    const certified = game({
      id: "v",
      title: "Vision",
      sources: [{ sourceId: "nvidia-3d-vision", label: "3D Vision Ready" }],
    });
    const ctx = { ...baseCtx, options: { ...baseCtx.options, visionCertifiedOnly: true } };
    expect(matchesCatalogGlobalFilter(certified, ctx)).toBe(true);
    expect(matchesCatalogGlobalFilter(game({ id: "x", title: "Other" }), ctx)).toBe(false);
  });

  it("matches global search text", () => {
    const g = game({ id: "s", title: "Half-Life 2", platformSupport: [{ platformKey: "steam", label: "SteamVR" }] });
    const ctx = { ...baseCtx, globalFilter: "steamvr" };
    expect(matchesCatalogGlobalFilter(g, ctx)).toBe(true);
    expect(matchesCatalogGlobalFilter(g, { ...ctx, globalFilter: "portal" })).toBe(false);
  });

  it("matches steam app id in global search", () => {
    const g = game({ id: "dota", title: "Dota 2", steamAppId: 570, steamMatchConfidence: 0.99 });
    const ctx = { ...baseCtx, globalFilter: "570" };
    expect(matchesCatalogGlobalFilter(g, ctx)).toBe(true);
  });

  it("matches title case-insensitively when filter is mixed case", () => {
    const g = game({ id: "hl2", title: "Half-Life 2" });
    const ctx = { ...baseCtx, globalFilter: "HALF-life" };
    expect(matchesCatalogGlobalFilter(g, ctx)).toBe(true);
  });
});
