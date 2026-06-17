import type { PaginationState, Table } from "@tanstack/table-core";
import { fitColumnWidths, renderPager } from "./grid-layout";
import { renderGridHeader } from "./grid-header";
import type { GridStatus } from "./grid-types";
import type { PriceHistoryDocument } from "./price-chart";
import { showPriceChart } from "./price-chart";
import type { CatalogGame } from "./types";

export function bindGridRowActions(
  tbody: HTMLTableSectionElement,
  data: CatalogGame[],
  priceHistory: PriceHistoryDocument | null,
  appRoot: HTMLElement,
  onWishlistToggle: (id: string) => void,
  onLibraryToggle: (id: string) => void,
): void {
  tbody.querySelectorAll<HTMLButtonElement>("button[data-wish]").forEach((btn) => {
    btn.addEventListener("click", () => onWishlistToggle(btn.dataset.wish!));
  });
  tbody.querySelectorAll<HTMLButtonElement>("button[data-library]").forEach((btn) => {
    btn.addEventListener("click", () => onLibraryToggle(btn.dataset.library!));
  });
  tbody.querySelectorAll<HTMLButtonElement>("button[data-price-game]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const game = data.find((g) => g.id === btn.dataset.priceGame);
      if (!game) return;
      const appKey = game.steamAppId ? String(game.steamAppId) : "";
      showPriceChart(
        appRoot,
        game.title,
        game.steamAppId,
        priceHistory?.apps[appKey],
        game.steamStats?.priceUsd,
      );
    });
  });
}

export function renderGridBody(
  tbody: HTMLTableSectionElement,
  rows: ReturnType<Table<CatalogGame>["getRowModel"]>["rows"],
  colCount: number,
): void {
  if (rows.length === 0) {
    tbody.innerHTML = `<tr><td colspan="${colCount}" class="empty">No games match the current filters.</td></tr>`;
    return;
  }

  tbody.innerHTML = rows
    .map((row) => {
      const cells = row.getVisibleCells().map((cell) => {
        const meta = cell.column.columnDef.meta as { steam?: boolean; wrap?: boolean } | undefined;
        const colId = cell.column.id;
        const classes = [meta?.wrap ? "cell-wrap" : ""].filter(Boolean).join(" ");
        const attrs = [
          meta?.steam ? ' data-source="steam-store"' : "",
          ` data-col="${colId}"`,
          classes ? ` class="${classes}"` : "",
        ].join("");
        const rendered = cell.column.columnDef.cell;
        const value = typeof rendered === "function" ? rendered(cell.getContext()) : cell.getValue();
        return `<td${attrs}>${value ?? ""}</td>`;
      });
      return `<tr>${cells.join("")}</tr>`;
    })
    .join("");
}

export interface CatalogGridPaintContext {
  readonly appRoot: HTMLElement;
  readonly root: HTMLElement;
  readonly data: CatalogGame[];
  readonly columns: { length: number };
  readonly priceHistory: PriceHistoryDocument | null;
  readonly filterOptions: Record<string, string[]>;
  readonly pagination: PaginationState;
  readonly table: Table<CatalogGame>;
  tableWrap: HTMLDivElement;
  tbody: HTMLTableSectionElement;
  pager: HTMLDivElement;
  wishlist: Set<string>;
  library: Set<string>;
  onStatus?: (status: GridStatus) => void;
  onWishlistToggle(id: string): void;
  onLibraryToggle(id: string): void;
  redraw(): void;
}

export function mountCatalogGrid(ctx: CatalogGridPaintContext): void {
  ctx.tableWrap = document.createElement("div");
  ctx.tableWrap.className = "table-wrap";
  ctx.tableWrap.innerHTML = `
      <table>
        <thead></thead>
        <tbody></tbody>
      </table>`;
  ctx.tbody = ctx.tableWrap.querySelector("tbody")!;
  ctx.pager = document.createElement("div");
  ctx.pager.className = "pager";
  ctx.root.append(ctx.tableWrap, ctx.pager);
  renderGridHeader(ctx.tableWrap, ctx.table, ctx.filterOptions);
  paintCatalogGrid(ctx);
}

export function paintCatalogGrid(ctx: CatalogGridPaintContext): void {
  const rows = ctx.table.getRowModel().rows;
  renderGridBody(ctx.tbody, rows, ctx.columns.length);
  if (rows.length > 0) {
    bindGridRowActions(
      ctx.tbody,
      ctx.data,
      ctx.priceHistory,
      ctx.appRoot,
      (id) => ctx.onWishlistToggle(id),
      (id) => ctx.onLibraryToggle(id),
    );
  }

  renderPager(ctx.pager, ctx.table, ctx.pagination);
  ctx.onStatus?.({
    filtered: ctx.table.getFilteredRowModel().rows.length,
    total: ctx.data.length,
    page: ctx.pagination.pageIndex + 1,
    pageCount: ctx.table.getPageCount(),
  });
  fitColumnWidths(ctx.tableWrap, ctx.tbody);
}
