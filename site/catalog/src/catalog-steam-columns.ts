import { createColumnHelper, type ColumnDef } from "@tanstack/table-core";
import {
  playerBucket,
  priceBucket,
  releaseYearBucket,
  reviewBucket,
} from "./filters/buckets";
import { matchesCheckboxFilter } from "./filters/checkbox-filter";
import type { CatalogGame } from "./types";
import { escapeHtml } from "./utils";

const columnHelper = createColumnHelper<CatalogGame>();

export function createSteamColumns(): ColumnDef<CatalogGame>[] {
  return [
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
  ] as ColumnDef<CatalogGame>[];
}
