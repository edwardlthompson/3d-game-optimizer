import type { GridOptions } from "./grid-types";
import { matchesListFilter } from "./list-filter";
import { rank3DLabel } from "./rank-3d";
import type { CatalogGame } from "./types";

export interface GlobalFilterContext {
  wishlist: Set<string>;
  library: Set<string>;
  options: GridOptions;
  globalFilter: string;
}

export function matchesCatalogGlobalFilter(game: CatalogGame, ctx: GlobalFilterContext): boolean {
  if (!matchesListFilter(ctx.wishlist.has(game.id), ctx.options.wishlistFilter)) return false;
  if (!matchesListFilter(ctx.library.has(game.id), ctx.options.libraryFilter)) return false;
  if (ctx.options.ultraOnly && game.bestLevel !== "ultra3d" && game.bestLevel !== "native3d") {
    return false;
  }
  if (ctx.options.visionCertifiedOnly) {
    const nvidia = game.sources.find((s) => s.sourceId === "nvidia-3d-vision");
    if (!nvidia || nvidia.label !== "3D Vision Ready") return false;
  }
  if (!ctx.globalFilter) return true;
  const hay = [
    game.title,
    game.id,
    game.steamAppId != null ? String(game.steamAppId) : "",
    rank3DLabel(game),
    ...(game.platformSupport ?? []).map((p) => p.label),
  ]
    .join(" ")
    .toLowerCase();
  return hay.includes(ctx.globalFilter.toLowerCase());
}
