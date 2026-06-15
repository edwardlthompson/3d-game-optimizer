import { playMethodDisplay, playMethodKey } from "./game-accessors";
import type { CatalogGame, SupportLevel } from "./types";

const LEVEL_SCORE: Record<SupportLevel, number> = {
  ultra3d: 88,
  native3d: 72,
  optimized3d: 58,
  playable3d: 42,
  experimental3d: 26,
  unsupported2d: 8,
};

/** Explicit scores for platform + label pairs (higher = better 3D experience). */
const METHOD_SCORE: Record<string, number> = {
  "truegame|3D Ultra": 100,
  "truegame|Acer TrueGame · 3D Ultra": 100,
  "truegame|3D+": 82,
  "truegame|Acer TrueGame · 3D": 84,
  "uevr|Works Perfectly": 97,
  "uevr|UEVR · 3D Ultra": 97,
  "uevr|Works Well": 66,
  "uevr|UEVR · Optimized": 66,
  "uevr|Works OK": 49,
  "uevr|UEVR · Playable": 49,
  "uevr|Works Poorly": 27,
  "uevr|UEVR · Experimental": 27,
  "uevr|VRto3D wiki": 46,
  "uevr|VRto3D · Playable": 46,
  "odyssey-hub|Odyssey 3D Hub": 94,
  "odyssey-hub|Samsung Odyssey 3D Hub · 3D": 78,
  "nvidia-3d-vision|3D Vision Ready": 88,
  "nvidia-3d-vision|NVIDIA 3D Vision · 3D": 86,
  "reshade-depth|Strong depth": 38,
  "reshade-depth|ReShade depth · Playable": 40,
  "reshade-depth|ReShade depth · Experimental": 28,
  "manual|Curated seed": 35,
  "manual|Curated · 3D": 74,
  "manual|Curated · Playable": 42,
  "manual|Curated · Experimental": 26,
  "manual|Curated · Unsupported": 8,
};

export interface Rank3DResult {
  score: number;
  label: string;
  filterKey: string;
}

function methodScore(platformKey: string, label: string, level: SupportLevel): number {
  const key = `${platformKey}|${label}`;
  return METHOD_SCORE[key] ?? LEVEL_SCORE[level] ?? 0;
}

function rankEntry(
  platformKey: string,
  label: string,
  level: SupportLevel,
): Rank3DResult {
  return {
    score: methodScore(platformKey, label, level),
    label: playMethodDisplay({ platformKey, label }),
    filterKey: playMethodKey({ platformKey, label }),
  };
}

/** Highest-scoring play path for a title (not cumulative across methods). */
export function rank3DForGame(game: CatalogGame): Rank3DResult {
  let best: Rank3DResult = {
    score: LEVEL_SCORE[game.bestLevel] ?? 0,
    label: game.bestExperience?.label ?? game.bestLevel,
    filterKey: game.bestExperience
      ? playMethodKey({
          platformKey: game.bestExperience.platformKey,
          label: game.bestExperience.label,
        })
      : game.bestLevel,
  };

  if (game.bestExperience) {
    best = rankEntry(
      game.bestExperience.platformKey,
      game.bestExperience.label,
      game.bestExperience.level,
    );
  }

  for (const entry of game.platformSupport ?? []) {
    const candidate = rankEntry(entry.platformKey, entry.label, entry.level);
    if (
      candidate.score > best.score ||
      (candidate.score === best.score && candidate.label.localeCompare(best.label) < 0)
    ) {
      best = candidate;
    }
  }

  return best;
}

export function rank3DScore(game: CatalogGame): number {
  return rank3DForGame(game).score;
}

export function rank3DLabel(game: CatalogGame): string {
  return rank3DForGame(game).label;
}
