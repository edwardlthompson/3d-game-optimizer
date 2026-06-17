import {
  type ColumnDef,
  type ColumnFiltersState,
  type PaginationState,
  type SortingState,
} from "@tanstack/table-core";
import { createColumns } from "./catalog-columns";
import { collectUniqueValues } from "./filters/buckets";
import { matchesCatalogGlobalFilter } from "./grid-filters";
import { mountCatalogGrid, paintCatalogGrid } from "./grid-render";
import { createCatalogTable, createCatalogTableHost, type CatalogTableSource } from "./grid-table";
import type { GridOptions, GridStatus } from "./grid-types";
import type { PriceHistoryDocument } from "./price-chart";
import { loadLibrary, toggleLibrary } from "./library";
import type { CatalogGame } from "./types";
import { loadWishlist, toggleWishlist } from "./wishlist";

export type { GridOptions, GridStatus } from "./grid-types";

export class CatalogGrid implements CatalogTableSource {
  readonly appRoot: HTMLElement;
  readonly root: HTMLElement;
  readonly data: CatalogGame[];
  columns: ColumnDef<CatalogGame>[];
  filterOptions: Record<string, string[]>;
  wishlist: Set<string>;
  library: Set<string>;
  priceHistory: PriceHistoryDocument | null;
  options: GridOptions = {
    wishlistFilter: "all",
    libraryFilter: "all",
    ultraOnly: false,
    visionCertifiedOnly: false,
  };
  sorting: SortingState = [{ id: "gameRank", desc: true }];
  columnFilters: ColumnFiltersState = [];
  pagination: PaginationState = { pageIndex: 0, pageSize: 50 };
  globalFilter = "";
  filterRevision = 0;
  tableWrap!: HTMLDivElement;
  tbody!: HTMLTableSectionElement;
  pager!: HTMLDivElement;
  onStatus?: (status: GridStatus) => void;
  table;

  constructor(
    appRoot: HTMLElement,
    gridRoot: HTMLElement,
    data: CatalogGame[],
    priceHistory: PriceHistoryDocument | null,
    onStatus?: (status: GridStatus) => void,
  ) {
    this.appRoot = appRoot;
    this.root = gridRoot;
    this.data = data;
    this.priceHistory = priceHistory;
    this.onStatus = onStatus;
    this.wishlist = loadWishlist();
    this.library = loadLibrary();
    this.filterOptions = collectUniqueValues(data);
    this.columns = createColumns({
      isWishlisted: (id) => this.wishlist.has(id),
      isInLibrary: (id) => this.library.has(id),
      onToggleWishlist: () => undefined,
      onPriceClick: () => undefined,
    });
    this.table = createCatalogTable(createCatalogTableHost(this));
    mountCatalogGrid(this);
  }

  setGlobalFilter(value: string): void {
    this.globalFilter = value.trim().toLowerCase();
    this.pagination = { ...this.pagination, pageIndex: 0 };
    this.bumpFilters();
  }

  setOptions(options: GridOptions): void {
    this.options = options;
    this.pagination = { ...this.pagination, pageIndex: 0 };
    this.bumpFilters();
  }

  refreshLibrary(): void {
    this.library = loadLibrary();
    paintCatalogGrid(this);
  }

  onWishlistToggle(id: string): void {
    this.wishlist = toggleWishlist(this.wishlist, id);
    paintCatalogGrid(this);
  }

  onLibraryToggle(id: string): void {
    this.library = toggleLibrary(this.library, id);
    paintCatalogGrid(this);
  }

  redraw(): void {
    paintCatalogGrid(this);
  }

  matchesGlobal(game: CatalogGame): boolean {
    return matchesCatalogGlobalFilter(game, {
      wishlist: this.wishlist,
      library: this.library,
      options: this.options,
      globalFilter: this.globalFilter,
    });
  }

  bumpFilters(): void {
    this.filterRevision += 1;
    paintCatalogGrid(this);
  }
}
