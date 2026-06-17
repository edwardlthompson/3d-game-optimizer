import type { ColumnDef, SortingState, ColumnFiltersState, PaginationState, Table } from "@tanstack/table-core";
import {
  createTable,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
} from "@tanstack/table-core";
import type { CatalogGame } from "./types";

export interface CatalogTableHost {
  readonly data: CatalogGame[];
  readonly columns: ColumnDef<CatalogGame>[];
  sorting: SortingState;
  columnFilters: ColumnFiltersState;
  pagination: PaginationState;
  filterRevision: number;
  onDraw(): void;
  matchesGlobal(game: CatalogGame): boolean;
}

export interface CatalogTableSource {
  readonly data: CatalogGame[];
  readonly columns: ColumnDef<CatalogGame>[];
  sorting: SortingState;
  columnFilters: ColumnFiltersState;
  pagination: PaginationState;
  filterRevision: number;
  redraw(): void;
  matchesGlobal(game: CatalogGame): boolean;
}

export function createCatalogTableHost(source: CatalogTableSource): CatalogTableHost {
  return {
    get data() {
      return source.data;
    },
    get columns() {
      return source.columns;
    },
    get sorting() {
      return source.sorting;
    },
    set sorting(value) {
      source.sorting = value;
    },
    get columnFilters() {
      return source.columnFilters;
    },
    set columnFilters(value) {
      source.columnFilters = value;
    },
    get pagination() {
      return source.pagination;
    },
    set pagination(value) {
      source.pagination = value;
    },
    get filterRevision() {
      return source.filterRevision;
    },
    set filterRevision(value) {
      source.filterRevision = value;
    },
    onDraw: () => source.redraw(),
    matchesGlobal: (game) => source.matchesGlobal(game),
  };
}

export function createCatalogTable(host: CatalogTableHost): Table<CatalogGame> {
  return createTable({
    get data() {
      return host.data;
    },
    get columns() {
      return host.columns;
    },
    state: {
      get sorting() {
        return host.sorting;
      },
      get columnFilters() {
        return host.columnFilters;
      },
      get pagination() {
        return host.pagination;
      },
      get globalFilter() {
        return String(host.filterRevision);
      },
      get columnPinning() {
        return { left: [] as string[], right: [] as string[] };
      },
    },
    initialState: { columnPinning: { left: [], right: [] } },
    onSortingChange: (updater) => {
      host.sorting = typeof updater === "function" ? updater(host.sorting) : updater;
      host.onDraw();
    },
    onColumnFiltersChange: (updater) => {
      host.columnFilters = typeof updater === "function" ? updater(host.columnFilters) : updater;
      host.pagination = { ...host.pagination, pageIndex: 0 };
      host.onDraw();
    },
    onPaginationChange: (updater) => {
      host.pagination = typeof updater === "function" ? updater(host.pagination) : updater;
      host.onDraw();
    },
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    globalFilterFn: (row) => host.matchesGlobal(row.original),
    onStateChange: () => undefined,
    renderFallbackValue: null,
  });
}
