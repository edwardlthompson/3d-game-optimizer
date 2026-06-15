import type { CatalogGame } from "../types";
import { DISPLAY_LABEL } from "../constants";
import { playMethodsForGame } from "../game-accessors";

export const NO_DATA = "(No data)";

export function priceBucket(price: number | null | undefined): string {
  if (price == null || Number.isNaN(price)) return NO_DATA;
  const start = Math.floor(price / 5) * 5;
  return `$${start}–$${(start + 4.99).toFixed(2)}`;
}

export function reviewBucket(percent: number | null | undefined): string {
  if (percent == null) return NO_DATA;
  if (percent >= 100) return "100%";
  const start = Math.floor(percent / 10) * 10;
  return `${start}–${start + 9}%`;
}

export function playerBucket(count: number | null | undefined): string {
  if (count == null) return NO_DATA;
  if (count === 0) return "0";
  if (count < 100) return "1–99";
  const start = Math.floor(count / 100) * 100;
  if (start >= 10000) return "10000+";
  return `${start}–${start + 99}`;
}

export function releaseYearBucket(date: string | undefined): string {
  if (!date) return NO_DATA;
  const match = date.match(/\b(19|20)\d{2}\b/);
  return match ? match[0] : NO_DATA;
}

function scoreBucket(score: number | null | undefined): string {
  if (score == null || Number.isNaN(score)) return NO_DATA;
  if (score >= 100) return "100";
  const start = Math.floor(score / 10) * 10;
  return `${start}–${start + 9}`;
}

function buildScoreBucketOptions(): string[] {
  const options: string[] = [];
  for (let start = 0; start < 100; start += 10) {
    options.push(`${start}–${start + 9}`);
  }
  options.push("100", NO_DATA);
  return options;
}

export function rank3DBucket(score: number | null | undefined): string {
  return scoreBucket(score);
}

export function buildRank3DBucketOptions(): string[] {
  return buildScoreBucketOptions();
}

export function gameRankBucket(score: number | null | undefined): string {
  return scoreBucket(score);
}

export function buildGameRankBucketOptions(): string[] {
  return buildScoreBucketOptions();
}

export function hardwareSummary(game: CatalogGame): string {
  return game.hardwareRequirements.displays.map((id) => DISPLAY_LABEL[id] ?? id).join(", ");
}

export function buildPriceBucketOptions(games: CatalogGame[]): string[] {
  let max = 0;
  for (const g of games) {
    const p = g.steamStats?.priceUsd;
    if (p != null) max = Math.max(max, p);
  }
  const cap = Math.max(60, Math.ceil(max / 5) * 5);
  const options: string[] = [];
  for (let start = 0; start <= cap; start += 5) {
    options.push(`$${start}–$${(start + 4.99).toFixed(2)}`);
  }
  options.push(NO_DATA);
  return options;
}

export function buildReviewBucketOptions(): string[] {
  const options: string[] = [];
  for (let start = 0; start < 100; start += 10) {
    options.push(`${start}–${start + 9}%`);
  }
  options.push("100%", NO_DATA);
  return options;
}

export function buildPlayerBucketOptions(): string[] {
  const hundreds = Array.from({ length: 99 }, (_, i) => {
    const start = (i + 1) * 100;
    return `${start}–${start + 99}`;
  });
  return ["0", "1–99", ...hundreds, "10000+", NO_DATA];
}

export function collectUniqueValues(games: CatalogGame[]): Record<string, string[]> {
  const playMethods = new Set<string>();
  const hardware = new Set<string>();
  const release = new Set<string>();

  for (const game of games) {
    for (const m of playMethodsForGame(game)) playMethods.add(m.key);
    hardware.add(hardwareSummary(game));
    release.add(releaseYearBucket(game.steamStats?.releaseDate));
  }

  const sort = (s: Set<string>) => [...s].sort((a, b) => a.localeCompare(b));
  return {
    rank3d: buildRank3DBucketOptions(),
    gameRank: buildGameRankBucketOptions(),
    playMethods: sort(playMethods),
    hardware: sort(hardware),
    releaseDate: sort(release),
    reviewPercent: buildReviewBucketOptions(),
    currentPlayers: buildPlayerBucketOptions(),
    priceUsd: buildPriceBucketOptions(games),
  };
}

export function playMethodKeysForGame(game: CatalogGame): string[] {
  return playMethodsForGame(game).map((m) => m.key);
}
