import { type ColumnDef, createColumnHelper } from "@tanstack/table-core";
import { createSteamColumns } from "./catalog-steam-columns";
import { titleCell } from "./catalog-title-cell";
import {
  gameRankBucket,
  hardwareSummary,
  playMethodKeysForGame,
  rank3DBucket,
} from "./filters/buckets";
import { matchesCheckboxFilter } from "./filters/checkbox-filter";
import { playMethodsForGame, playMethodsText } from "./game-accessors";
import { compareGameRank, gameRankScore, GAME_RANK_TOOLTIP } from "./game-ranking";
import { rank3DForGame, rank3DScore } from "./rank-3d";
import type { CatalogGame } from "./types";
import { displayTitle, escapeHtml } from "./utils";

const columnHelper = createColumnHelper<CatalogGame>();

export interface ColumnCallbacks {
  isWishlisted: (id: string) => boolean;
  isInLibrary: (id: string) => boolean;
  onToggleWishlist: (id: string) => void;
  onPriceClick: (game: CatalogGame) => void;
}

export function createColumns(callbacks: ColumnCallbacks): ColumnDef<CatalogGame>[] {
  return [
    columnHelper.display({
      id: "library",
      header: "Lib",
      enableSorting: false,
      enableColumnFilter: false,
      cell: (info) => {
        const game = info.row.original;
        const on = callbacks.isInLibrary(game.id);
        return `<button type="button" class="library-btn${on ? " on" : ""}" data-library="${escapeHtml(game.id)}" aria-label="In library">${on ? "✓" : "○"}</button>`;
      },
    }),
    columnHelper.display({
      id: "wishlist",
      header: "★",
      enableSorting: false,
      enableColumnFilter: false,
      cell: (info) => {
        const game = info.row.original;
        const on = callbacks.isWishlisted(game.id);
        return `<button type="button" class="wish-btn${on ? " on" : ""}" data-wish="${escapeHtml(game.id)}" aria-label="Wishlist">${on ? "★" : "☆"}</button>`;
      },
    }),
    columnHelper.accessor("title", {
      id: "title",
      header: "Title",
      enableColumnFilter: false,
      meta: { wrap: true },
      cell: (info) => titleCell(info.row.original, displayTitle(info.getValue())),
    }),
    columnHelper.accessor((g) => gameRankScore(g), {
      id: "gameRank",
      header: "Game Rank",
      meta: { tooltip: GAME_RANK_TOOLTIP },
      sortingFn: (a, b) => compareGameRank(a.original, b.original),
      filterFn: (row, _id, value) =>
        matchesCheckboxFilter(gameRankBucket(gameRankScore(row.original)), value),
      cell: (info) => {
        const score = gameRankScore(info.row.original);
        if (score == null) return "—";
        return `<span class="game-rank-value" title="${escapeHtml(GAME_RANK_TOOLTIP)}">${score.toFixed(1)}</span>`;
      },
    }),
    columnHelper.accessor((g) => rank3DScore(g), {
      id: "rank3d",
      header: "3D Rank",
      meta: { wrap: true },
      sortingFn: (a, b) => rank3DScore(a.original) - rank3DScore(b.original),
      filterFn: (row, _id, value) =>
        matchesCheckboxFilter(rank3DBucket(rank3DScore(row.original)), value),
      cell: (info) => {
        const game = info.row.original;
        const rank = rank3DForGame(game);
        const levelClass = rank.score >= 95 ? "badge ultra" : rank.score >= 80 ? "badge" : "badge legacy";
        return `<span class="${levelClass}">${rank.score}</span> ${escapeHtml(rank.label)}`;
      },
    }),
    columnHelper.accessor((g) => playMethodsText(g), {
      id: "playMethods",
      header: "Play methods",
      meta: { wrap: true },
      filterFn: (row, _id, value) =>
        matchesCheckboxFilter(playMethodKeysForGame(row.original), value),
      cell: (info) =>
        playMethodsForGame(info.row.original)
          .map((m) => `<span class="badge">${escapeHtml(m.label)}</span>`)
          .join(""),
    }),
    columnHelper.accessor((g) => hardwareSummary(g), {
      id: "hardware",
      header: "Hardware",
      meta: { wrap: true },
      filterFn: (row, _id, value) => matchesCheckboxFilter(hardwareSummary(row.original), value),
      cell: (info) => escapeHtml(hardwareSummary(info.row.original)),
    }),
    ...createSteamColumns(),
  ] as ColumnDef<CatalogGame>[];
}
