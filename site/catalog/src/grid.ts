import {
  type ColumnDef,
  type ColumnFiltersState,
  type PaginationState,
  type SortingState,
  createTable,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
} from "@tanstack/table-core";
import { PAGE_SIZES } from "./constants";
import { createColumns } from "./catalog-columns";
import { collectUniqueValues } from "./filters/buckets";
import { ColumnFilterPopover, formatPlayMethodOption } from "./filters/ColumnFilterPopover";
import type { PriceHistoryDocument } from "./price-chart";
import { showPriceChart } from "./price-chart";
import { rank3DLabel } from "./rank-3d";
import { loadLibrary, toggleLibrary } from "./library";
import { matchesListFilter, type ListFilterMode } from "./list-filter";
import type { CatalogGame } from "./types";
import { loadWishlist, toggleWishlist } from "./wishlist";

export interface GridStatus {
  filtered: number;
  total: number;
  page: number;
  pageCount: number;
}

export interface GridOptions {
  wishlistFilter: ListFilterMode;
  libraryFilter: ListFilterMode;
  ultraOnly: boolean;
  visionCertifiedOnly: boolean;
}

export class CatalogGrid {
  private readonly root: HTMLElement;
  private readonly appRoot: HTMLElement;
  private readonly data: CatalogGame[];
  private columns: ColumnDef<CatalogGame>[];
  private filterOptions: Record<string, string[]>;
  private wishlist: Set<string>;
  private library: Set<string>;
  private priceHistory: PriceHistoryDocument | null;
  private options: GridOptions = {
    wishlistFilter: "all",
    libraryFilter: "all",
    ultraOnly: false,
    visionCertifiedOnly: false,
  };
  private sorting: SortingState = [{ id: "title", desc: false }];
  private columnFilters: ColumnFiltersState = [];
  private pagination: PaginationState = { pageIndex: 0, pageSize: 50 };
  private globalFilter = "";
  private filterRevision = 0;
  private tableWrap!: HTMLDivElement;
  private tbody!: HTMLTableSectionElement;
  private pager!: HTMLDivElement;
  private onStatus?: (status: GridStatus) => void;
  private table;

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
    const self = this;
    this.table = createTable({
      get data() {
        return self.data;
      },
      get columns() {
        return self.columns;
      },
      state: {
        get sorting() {
          return self.sorting;
        },
        get columnFilters() {
          return self.columnFilters;
        },
        get pagination() {
          return self.pagination;
        },
        get globalFilter() {
          return String(self.filterRevision);
        },
        get columnPinning() {
          return { left: [] as string[], right: [] as string[] };
        },
      },
      initialState: { columnPinning: { left: [], right: [] } },
      onSortingChange: (updater) => {
        self.sorting = typeof updater === "function" ? updater(self.sorting) : updater;
        self.draw();
      },
      onColumnFiltersChange: (updater) => {
        self.columnFilters = typeof updater === "function" ? updater(self.columnFilters) : updater;
        self.pagination = { ...self.pagination, pageIndex: 0 };
        self.draw();
      },
      onPaginationChange: (updater) => {
        self.pagination = typeof updater === "function" ? updater(self.pagination) : updater;
        self.draw();
      },
      getCoreRowModel: getCoreRowModel(),
      getFilteredRowModel: getFilteredRowModel(),
      getSortedRowModel: getSortedRowModel(),
      getPaginationRowModel: getPaginationRowModel(),
      globalFilterFn: (row) => self.matchesGlobal(row.original),
      onStateChange: () => undefined,
      renderFallbackValue: null,
    });
    this.mount();
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

  private bumpFilters(): void {
    this.filterRevision += 1;
    this.draw();
  }

  private matchesGlobal(game: CatalogGame): boolean {
    if (!matchesListFilter(this.wishlist.has(game.id), this.options.wishlistFilter)) return false;
    if (!matchesListFilter(this.library.has(game.id), this.options.libraryFilter)) return false;
    if (this.options.ultraOnly && game.bestLevel !== "ultra3d" && game.bestLevel !== "native3d") {
      return false;
    }
    if (this.options.visionCertifiedOnly) {
      const nvidia = game.sources.find((s) => s.sourceId === "nvidia-3d-vision");
      if (!nvidia || nvidia.label !== "3D Vision Ready") return false;
    }
    if (!this.globalFilter) return true;
    const hay = [game.title, rank3DLabel(game), ...(game.platformSupport ?? []).map((p) => p.label)]
      .join(" ")
      .toLowerCase();
    return hay.includes(this.globalFilter);
  }

  private mount(): void {
    this.tableWrap = document.createElement("div");
    this.tableWrap.className = "table-wrap";
    this.tableWrap.innerHTML = `
      <table>
        <thead></thead>
        <tbody></tbody>
      </table>`;
    this.tbody = this.tableWrap.querySelector("tbody")!;
    this.pager = document.createElement("div");
    this.pager.className = "pager";
    this.root.append(this.tableWrap, this.pager);
    this.renderHeader();
    this.draw();
  }

  private renderHeader(): void {
    const thead = this.tableWrap.querySelector("thead")!;
    const sortRow = document.createElement("tr");
    const filterRow = document.createElement("tr");
    filterRow.className = "filter-row";

    for (const header of this.table.getHeaderGroups()[0]?.headers ?? []) {
      const th = document.createElement("th");
      const meta = header.column.columnDef.meta as { steam?: boolean } | undefined;
      if (meta?.steam) th.dataset.source = "steam-store";
      if (header.column.getCanSort()) {
        th.addEventListener("click", (event) => {
          header.column.toggleSorting(undefined, event.shiftKey);
        });
      }
      th.textContent = String(header.column.columnDef.header ?? header.column.id);
      th.dataset.col = header.column.id;
      sortRow.append(th);

      const filterTh = document.createElement("th");
      if (header.column.getCanFilter()) {
        const options = this.filterOptions[header.column.id] ?? ["Has Steam link", "No link"];
        const format =
          header.column.id === "playMethods" || header.column.id === "rank3d"
            ? formatPlayMethodOption
            : (v: string) => v;
        const popover = new ColumnFilterPopover(
          options,
          String(header.column.columnDef.header ?? header.column.id),
          (selected) => header.column.setFilterValue(selected),
          format,
        );
        filterTh.append(popover.createButton());
      }
      filterRow.append(filterTh);
    }

    thead.replaceChildren(sortRow, filterRow);
  }

  private bindRowActions(): void {
    this.tbody.querySelectorAll<HTMLButtonElement>("button[data-wish]").forEach((btn) => {
      btn.addEventListener("click", () => {
        this.wishlist = toggleWishlist(this.wishlist, btn.dataset.wish!);
        this.draw();
      });
    });
    this.tbody.querySelectorAll<HTMLButtonElement>("button[data-library]").forEach((btn) => {
      btn.addEventListener("click", () => {
        this.library = toggleLibrary(this.library, btn.dataset.library!);
        this.draw();
      });
    });
    this.tbody.querySelectorAll<HTMLButtonElement>("button[data-price-game]").forEach((btn) => {
      btn.addEventListener("click", () => {
        const game = this.data.find((g) => g.id === btn.dataset.priceGame);
        if (!game) return;
        const appKey = game.steamAppId ? String(game.steamAppId) : "";
        showPriceChart(
          this.appRoot,
          game.title,
          game.steamAppId,
          this.priceHistory?.apps[appKey],
          game.steamStats?.priceUsd,
        );
      });
    });
  }

  private draw(): void {
    const rows = this.table.getRowModel().rows;
    const colCount = this.columns.length;
    if (rows.length === 0) {
      this.tbody.innerHTML = `<tr><td colspan="${colCount}" class="empty">No games match the current filters.</td></tr>`;
    } else {
      this.tbody.innerHTML = rows
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
            const value =
              typeof rendered === "function" ? rendered(cell.getContext()) : cell.getValue();
            return `<td${attrs}>${value ?? ""}</td>`;
          });
          return `<tr>${cells.join("")}</tr>`;
        })
        .join("");
      this.bindRowActions();
    }

    this.renderPager();
    this.onStatus?.({
      filtered: this.table.getFilteredRowModel().rows.length,
      total: this.data.length,
      page: this.pagination.pageIndex + 1,
      pageCount: this.table.getPageCount(),
    });
    this.fitColumnWidths();
  }

  /** Measure visible rows in auto layout, then lock column widths for this page. */
  private fitColumnWidths(): void {
    const table = this.tableWrap.querySelector("table")!;
    table.style.tableLayout = "auto";
    table.querySelector("colgroup")?.remove();

    requestAnimationFrame(() => {
      const thead = table.querySelector("thead")!;
      const headerRow = thead.querySelector("tr:first-child");
      if (!headerRow) return;

      const bodyRows = [...this.tbody.querySelectorAll("tr")].filter(
        (row) => !row.querySelector(".empty"),
      );
      const colCount = headerRow.children.length;
      if (colCount === 0 || bodyRows.length === 0) return;

      const widths: number[] = [];
      for (let i = 0; i < colCount; i += 1) {
        let max = (headerRow.children[i] as HTMLElement).offsetWidth;
        for (const row of bodyRows) {
          const cell = row.children[i] as HTMLElement | undefined;
          if (cell) max = Math.max(max, cell.offsetWidth);
        }
        widths.push(max);
      }

      const colgroup = document.createElement("colgroup");
      colgroup.replaceChildren(
        ...widths.map((width) => {
          const col = document.createElement("col");
          col.style.width = `${width}px`;
          return col;
        }),
      );
      table.insertBefore(colgroup, thead);
      table.style.tableLayout = "fixed";
    });
  }

  private renderPager(): void {
    const { pageIndex, pageSize } = this.pagination;
    const pageCount = this.table.getPageCount();
    this.pager.innerHTML = `
      <button type="button" data-nav="first" ${pageIndex === 0 ? "disabled" : ""}>First</button>
      <button type="button" data-nav="prev" ${pageIndex === 0 ? "disabled" : ""}>Prev</button>
      <span>Page ${pageIndex + 1} of ${Math.max(pageCount, 1)}</span>
      <button type="button" data-nav="next" ${pageIndex >= pageCount - 1 ? "disabled" : ""}>Next</button>
      <button type="button" data-nav="last" ${pageIndex >= pageCount - 1 ? "disabled" : ""}>Last</button>
      <label>Rows <select data-page-size>${PAGE_SIZES.map((n) => `<option value="${n}" ${n === pageSize ? "selected" : ""}>${n}</option>`).join("")}</select></label>
    `;
    this.pager.querySelectorAll<HTMLButtonElement>("button[data-nav]").forEach((btn) => {
      btn.addEventListener("click", () => {
        const nav = btn.dataset.nav;
        if (nav === "first") this.table.setPageIndex(0);
        if (nav === "prev") this.table.previousPage();
        if (nav === "next") this.table.nextPage();
        if (nav === "last") this.table.setPageIndex(Math.max(pageCount - 1, 0));
      });
    });
    this.pager.querySelector<HTMLSelectElement>("select[data-page-size]")?.addEventListener("change", (e) => {
      this.table.setPageSize(Number((e.target as HTMLSelectElement).value));
    });
  }
}
