import { rank3DScore } from "./rank-3d";
import { weightedReviewScore } from "./steam-ranking";
import type { CatalogGame } from "./types";

/** Share of Game Rank from weighted Steam popularity (reviews + players). */
const STEAM_WEIGHT = 0.72;
/** Share of Game Rank from best 3D path score; breaks ties when Steam scores are close. */
const RANK3D_WEIGHT = 0.28;

export const GAME_RANK_TOOLTIP =
  "Game Rank = 72% weighted Steam score + 28% 3D Rank. Steam score shrinks review % for low vote counts and boosts titles with more players. 3D Rank is the single best play path (TrueGame 3D Ultra, UEVR Works Perfectly, etc.). When Game Ranks are close, the higher 3D Rank wins sort order.";

/**
 * Combined catalog score 0–100 blending Steam popularity with best 3D path quality.
 */
export function gameRankScore(game: CatalogGame): number | null {
  const steam = weightedReviewScore(game.steamStats);
  const rank3d = rank3DScore(game);

  if (steam == null && rank3d <= 0) return null;

  const steamPart = steam ?? rank3d * 0.55;
  const score = steamPart * STEAM_WEIGHT + rank3d * RANK3D_WEIGHT;
  return Math.round(score * 10) / 10;
}

/** Sort by Game Rank, then 3D Rank as tiebreaker. */
export function compareGameRank(a: CatalogGame, b: CatalogGame): number {
  const av = gameRankScore(a) ?? -1;
  const bv = gameRankScore(b) ?? -1;
  if (av !== bv) return av - bv;
  return rank3DScore(a) - rank3DScore(b);
}
