import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  createColumnHelper,
  createTable,
  getCoreRowModel,
  getFilteredRowModel,
  getSortedRowModel,
} from "@tanstack/table-core";
import { describe, expect, it } from "vitest";
import { compareGameRank, gameRankScore } from "./game-ranking";
import type { CatalogGame } from "./types";

const catalogPath = join(
  dirname(fileURLToPath(import.meta.url)),
  "..",
  "public",
  "data",
  "catalog-v2.json",
);
const catalog = JSON.parse(readFileSync(catalogPath, "utf8")) as { games: CatalogGame[] };
const games = catalog.games ?? [];

describe("smoke game rank sort", () => {
  it("default descending Game Rank matches compareGameRank order", () => {
    expect(games.length).toBeGreaterThanOrEqual(400);

    const columnHelper = createColumnHelper<CatalogGame>();
    const table = createTable({
      data: games,
      columns: [
        columnHelper.accessor((g) => gameRankScore(g), {
          id: "gameRank",
          header: "Game Rank",
          sortingFn: (a, b) => compareGameRank(a.original, b.original),
        }),
      ],
      state: {
        sorting: [{ id: "gameRank", desc: true }],
        columnPinning: { left: [], right: [] },
      },
      initialState: { columnPinning: { left: [], right: [] } },
      getCoreRowModel: getCoreRowModel(),
      getFilteredRowModel: getFilteredRowModel(),
      getSortedRowModel: getSortedRowModel(),
      onStateChange: () => undefined,
      renderFallbackValue: null,
    });

    const sorted = table.getSortedRowModel().rows.slice(0, 40);
    const scores = sorted
      .map((row) => gameRankScore(row.original))
      .filter((score): score is number => score != null);
    expect(scores.length).toBeGreaterThanOrEqual(10);

    for (let i = 1; i < scores.length; i += 1) {
      expect(scores[i - 1]).toBeGreaterThanOrEqual(scores[i]!);
    }

    for (let i = 1; i < sorted.length; i += 1) {
      expect(compareGameRank(sorted[i - 1]!.original, sorted[i]!.original)).toBeGreaterThanOrEqual(0);
    }
  });
});
