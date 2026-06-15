import {
  type ColumnDef,
  createColumnHelper,
} from "@tanstack/table-core";
import {
  buyBucket,
  hardwareSummary,
  playerBucket,
  playMethodKeysForGame,
  priceBucket,
  rank3DBucket,
  releaseYearBucket,
  reviewBucket,
  steamRankBucket,
} from "./filters/buckets";
import { matchesCheckboxFilter } from "./filters/checkbox-filter";
import { playMethodsForGame, playMethodsText } from "./game-accessors";
import { rank3DForGame, rank3DScore } from "./rank-3d";
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
      header: "Steam Rank",
      meta: { steam: true },
      sortingFn: (a, b) => {
        const av = weightedReviewScore(a.original.steamStats) ?? -1;
        const bv = weightedReviewScore(b.original.steamStats) ?? -1;
        return av - bv;
      },
      filterFn: (row, _id, value) =>
        matchesCheckboxFilter(
          steamRankBucket(weightedReviewScore(row.original.steamStats)),
          value,
        ),
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
