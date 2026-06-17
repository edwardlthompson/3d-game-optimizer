import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { gameRankScore } from "./game-ranking";
import { rank3DScore } from "./rank-3d";
import { weightedReviewScore } from "./steam-ranking";
import type { CatalogGame } from "./types";

const fixturesPath = join(
  dirname(fileURLToPath(import.meta.url)),
  "..",
  "..",
  "..",
  "data",
  "rank-golden",
  "fixtures.json",
);
const fixtures = JSON.parse(readFileSync(fixturesPath, "utf8")) as {
  weightedReview: Array<{
    reviewPercent: number;
    reviewCount: number;
    currentPlayers: number;
    expectedMin: number;
    expectedMax: number;
  }>;
  gameRank: Array<{
    reviewPercent: number | null;
    reviewCount: number | null;
    currentPlayers: number | null;
    rank3DScore: number;
    expectedMin: number;
    expectedMax: number;
  }>;
  rank3d: Array<{
    bestLevel: string;
    bestExperience: { platformKey: string; label: string; level: string };
    expectedScore: number;
  }>;
};

describe("golden rank fixtures", () => {
  it("weightedReview scores stay in expected ranges", () => {
    for (const item of fixtures.weightedReview) {
      const score = weightedReviewScore({
        reviewPercent: item.reviewPercent,
        reviewCount: item.reviewCount,
        currentPlayers: item.currentPlayers,
      });
      expect(score).not.toBeNull();
      expect(score!).toBeGreaterThanOrEqual(item.expectedMin);
      expect(score!).toBeLessThanOrEqual(item.expectedMax);
    }
  });

  it("gameRank scores stay in expected ranges", () => {
    for (const item of fixtures.gameRank) {
      const game: CatalogGame = {
        id: "test",
        title: "Test",
        bestLevel: item.rank3DScore >= 72 ? "ultra3d" : "experimental3d",
        bestExperience:
          item.rank3DScore >= 72
            ? {
                platformKey: "truegame",
                label: "Acer TrueGame · 3D Ultra",
                level: "ultra3d",
              }
            : {
                platformKey: "manual",
                label: "Curated · Experimental",
                level: "experimental3d",
              },
        platformSupport: [],
        steamStats:
          item.reviewPercent == null
            ? undefined
            : {
                reviewPercent: item.reviewPercent,
                reviewCount: item.reviewCount ?? 0,
                currentPlayers: item.currentPlayers ?? 0,
              },
      } as CatalogGame;

      const score = gameRankScore(game);
      expect(score).not.toBeNull();
      expect(score!).toBeGreaterThanOrEqual(item.expectedMin);
      expect(score!).toBeLessThanOrEqual(item.expectedMax);
    }
  });

  it("rank3d scores match golden expected values", () => {
    for (const item of fixtures.rank3d) {
      const game = {
        bestLevel: item.bestLevel,
        bestExperience: item.bestExperience,
        platformSupport: [],
      } as CatalogGame;
      expect(rank3DScore(game)).toBe(item.expectedScore);
    }
  });
});
