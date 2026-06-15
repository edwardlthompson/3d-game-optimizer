import {
  type ColumnDef,
  createColumnHelper,
} from "@tanstack/table-core";
import { LEVEL_RANK, formatLevel } from "./constants";
import {
  buyBucket,
  hardwareSummary,
  playerBucket,
  playMethodKeysForGame,
  priceBucket,
  releaseYearBucket,
  reviewBucket,
} from "./filters/buckets";
import { matchesCheckboxFilter } from "./filters/checkbox-filter";
import { playMethodsForGame, playMethodsText } from "./game-accessors";
import { weightedReviewScore } from "./steam-ranking";
import type { CatalogGame } from "./types";
import { displayTitle, escapeHtml } from "./utils";

function steamBuyCell(game: CatalogGame): string {
  const appId = game.steamAppId;
  if (!appId) return "—";
  return `<a href="steam://store/${appId}" class="steam-app-link" aria-label="Open in Steam app">Open in Steam</a>`;
}

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
      cell: (info) => {
        const title = displayTitle(info.getValue());
        return `<span class="title-cell" title="${escapeHtml(title)}">${escapeHtml(title)}</span>`;
      },
    }),
    columnHelper.accessor("bestLevel", {
      id: "bestLevel",
      header: "3D level",
      sortingFn: (a, b) => LEVEL_RANK[a.original.bestLevel] - LEVEL_RANK[b.original.bestLevel],
      filterFn: (row, _id, value) =>
        matchesCheckboxFilter(formatLevel(row.original.bestLevel), value),
      cell: (info) => {
        const game = info.row.original;
        const levelClass = game.bestLevel === "ultra3d" ? "badge ultra" : "badge";
        return `<span class="${levelClass}">${escapeHtml(formatLevel(game.bestLevel))}</span>`;
      },
    }),
    columnHelper.accessor((g) => g.bestExperience?.label ?? formatLevel(g.bestLevel), {
      id: "bestExperience",
      header: "Best experience",
      meta: { wrap: true },
      filterFn: (row, _id, value) => {
        const label = row.original.bestExperience?.label ?? formatLevel(row.original.bestLevel);
        return matchesCheckboxFilter(label, value);
      },
      cell: (info) => escapeHtml(String(info.getValue())),
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
    columnHelper.accessor((g) => g.steamStats?.reviewPercent ?? null, {
      id: "reviewPercent",
      header: "Reviews",
      meta: { steam: true },
      filterFn: (row, _id, value) =>
        matchesCheckboxFilter(reviewBucket(row.original.steamStats?.reviewPercent), value),
      cell: (info) => {
        const review = info.row.original.steamStats?.reviewPercent;
        const count = info.row.original.steamStats?.reviewCount;
        if (review == null) return "—";
        return count ? `${review}% (${count.toLocaleString()})` : `${review}%`;
      },
    }),
    columnHelper.accessor((g) => weightedReviewScore(g.steamStats), {
      id: "weightedReview",
      header: "Rank",
      meta: { steam: true },
      sortingFn: (a, b) => {
        const av = weightedReviewScore(a.original.steamStats) ?? -1;
        const bv = weightedReviewScore(b.original.steamStats) ?? -1;
        return av - bv;
      },
      enableColumnFilter: false,
      cell: (info) => {
        const score = weightedReviewScore(info.row.original.steamStats);
        return score != null ? score.toFixed(1) : "—";
      },
    }),
    columnHelper.accessor((g) => g.steamStats?.currentPlayers ?? null, {
      id: "currentPlayers",
      header: "Players",
      meta: { steam: true },
      filterFn: (row, _id, value) =>
        matchesCheckboxFilter(playerBucket(row.original.steamStats?.currentPlayers), value),
      cell: (info) => {
        const players = info.row.original.steamStats?.currentPlayers;
        return players != null ? players.toLocaleString() : "—";
      },
    }),
    columnHelper.accessor((g) => g.steamStats?.releaseDate ?? "—", {
      id: "releaseDate",
      header: "Release",
      meta: { steam: true },
      filterFn: (row, _id, value) =>
        matchesCheckboxFilter(releaseYearBucket(row.original.steamStats?.releaseDate), value),
      cell: (info) => escapeHtml(String(info.getValue())),
    }),
    columnHelper.accessor((g) => g.steamStats?.priceUsd ?? null, {
      id: "priceUsd",
      header: "Price",
      meta: { steam: true, price: true },
      filterFn: (row, _id, value) =>
        matchesCheckboxFilter(priceBucket(row.original.steamStats?.priceUsd), value),
      cell: (info) => {
        const game = info.row.original;
        const price = game.steamStats?.priceUsd;
        if (price == null) return "—";
        return `<button type="button" class="price-btn" data-price-game="${escapeHtml(game.id)}">$${price.toFixed(2)}</button>`;
      },
    }),
    columnHelper.accessor((g) => buyBucket(g), {
      id: "buy",
      header: "Buy",
      enableSorting: false,
      filterFn: (row, _id, value) => matchesCheckboxFilter(buyBucket(row.original), value),
      cell: (info) => steamBuyCell(info.row.original),
    }),
  ] as ColumnDef<CatalogGame>[];
}
