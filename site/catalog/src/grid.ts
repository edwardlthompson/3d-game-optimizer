import {
  type ColumnDef,
  type ColumnFiltersState,
  type PaginationState,
  type SortingState,
  createColumnHelper,
  createTable,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
} from "@tanstack/table-core";
import { DISPLAY_LABEL, LEVEL_RANK, PAGE_SIZES, formatLevel } from "./constants";
import type { CatalogGame } from "./types";
import { escapeHtml, includesFilter } from "./utils";

const columnHelper = createColumnHelper<CatalogGame>();

function visionLabel(game: CatalogGame): string {
  const nvidia = game.sources.find((s) => s.sourceId === "nvidia-3d-vision");
  if (!nvidia) return "—";
  return nvidia.label;
}

function hardwareSummary(game: CatalogGame): string {
  return game.hardwareRequirements.displays.map((id) => DISPLAY_LABEL[id] ?? id).join(", ");
}

function steamBuyCell(game: CatalogGame): string {
  const url =
    game.purchaseLinks?.steam ??
    (game.steamAppId ? `https://store.steampowered.com/app/${game.steamAppId}/` : null);
  return url
    ? `<a href="${escapeHtml(url)}" rel="noopener noreferrer">Buy on Steam</a>`
    : "—";
}

const columns = [
  columnHelper.accessor("title", {
    id: "title",
    header: "Title",
    filterFn: (row, _id, value) => includesFilter(row.original.title, String(value ?? "")),
    cell: (info) => escapeHtml(info.getValue()),
  }),
  columnHelper.accessor("bestLevel", {
    id: "bestLevel",
    header: "3D level",
    sortingFn: (a, b) => LEVEL_RANK[a.original.bestLevel] - LEVEL_RANK[b.original.bestLevel],
    filterFn: (row, _id, value) =>
      includesFilter(formatLevel(row.original.bestLevel), String(value ?? "")),
    cell: (info) => {
      const game = info.row.original;
      const levelClass = game.bestLevel === "ultra3d" ? "badge ultra" : "badge";
      const legacy = game.sources.some((s) => s.supportStatus === "legacy")
        ? `<span class="badge legacy">Legacy 3D Vision</span>`
        : "";
      return `<span class="${levelClass}">${escapeHtml(formatLevel(game.bestLevel))}</span>${legacy}`;
    },
  }),
  columnHelper.accessor((g) => g.bestExperience?.label ?? formatLevel(g.bestLevel), {
    id: "bestExperience",
    header: "Best experience",
    filterFn: (row, _id, value) => {
      const label = row.original.bestExperience?.label ?? formatLevel(row.original.bestLevel);
      return includesFilter(label, String(value ?? ""));
    },
    cell: (info) => escapeHtml(String(info.getValue())),
  }),
  columnHelper.accessor((g) => g.trueGameLabel ?? "—", {
    id: "trueGame",
    header: "TrueGame",
    filterFn: (row, _id, value) =>
      includesFilter(row.original.trueGameLabel ?? "—", String(value ?? "")),
    cell: (info) => escapeHtml(String(info.getValue())),
  }),
  columnHelper.accessor((g) => visionLabel(g), {
    id: "vision",
    header: "3D Vision",
    filterFn: (row, _id, value) => includesFilter(visionLabel(row.original), String(value ?? "")),
    cell: (info) => escapeHtml(String(info.getValue())),
  }),
  columnHelper.accessor((g) => g.platforms.join(" "), {
    id: "platforms",
    header: "Platforms",
    filterFn: (row, _id, value) =>
      includesFilter(row.original.platforms.join(" "), String(value ?? "")),
    cell: (info) =>
      info.row.original.platforms
        .map((p) => `<span class="badge">${escapeHtml(p)}</span>`)
        .join(""),
  }),
  columnHelper.accessor((g) => hardwareSummary(g), {
    id: "hardware",
    header: "Hardware",
    filterFn: (row, _id, value) =>
      includesFilter(hardwareSummary(row.original), String(value ?? "")),
    cell: (info) => {
      const game = info.row.original;
      const exclusive =
        game.hardwareRequirements.exclusiveTo.length > 0
          ? `<span class="badge exclusive">Exclusive hardware</span>`
          : "";
      return `${escapeHtml(hardwareSummary(game))}${exclusive}`;
    },
  }),
  columnHelper.accessor((g) => g.steamStats?.reviewPercent ?? null, {
    id: "reviewPercent",
    header: "Reviews",
    meta: { steam: true },
    filterFn: (row, _id, value) => {
      const review = row.original.steamStats?.reviewPercent;
      return includesFilter(review != null ? `${review}%` : "—", String(value ?? ""));
    },
    cell: (info) => {
      const review = info.row.original.steamStats?.reviewPercent;
      return review != null ? `${review}%` : "—";
    },
  }),
  columnHelper.accessor((g) => g.steamStats?.currentPlayers ?? null, {
    id: "currentPlayers",
    header: "Players",
    meta: { steam: true },
    filterFn: (row, _id, value) => {
      const players = row.original.steamStats?.currentPlayers;
      return includesFilter(players != null ? players.toLocaleString() : "—", String(value ?? ""));
    },
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
      includesFilter(row.original.steamStats?.releaseDate ?? "—", String(value ?? "")),
    cell: (info) => escapeHtml(String(info.getValue())),
  }),
  columnHelper.accessor((g) => g.steamStats?.priceUsd ?? null, {
    id: "priceUsd",
    header: "Price",
    meta: { steam: true },
    filterFn: (row, _id, value) => {
      const price = row.original.steamStats?.priceUsd;
      return includesFilter(price != null ? `$${price.toFixed(2)}` : "—", String(value ?? ""));
    },
    cell: (info) => {
      const price = info.row.original.steamStats?.priceUsd;
      return price != null ? `$${price.toFixed(2)}` : "—";
    },
  }),
  columnHelper.display({
    id: "buy",
    header: "Buy",
    enableColumnFilter: false,
    enableSorting: false,
    cell: (info) => steamBuyCell(info.row.original),
  }),
] as ColumnDef<CatalogGame>[];

export interface GridStatus {
  filtered: number;
  total: number;
  page: number;
  pageCount: number;
}

export interface QuickFilters {
  ultraOnly: boolean;
  visionCertifiedOnly: boolean;
  platforms: Set<string>;
  hardware: Set<string>;
}

export class CatalogGrid {
  private readonly root: HTMLElement;
  private readonly data: CatalogGame[];
  private sorting: SortingState = [{ id: "title", desc: false }];
  private columnFilters: ColumnFiltersState = [];
  private pagination: PaginationState = { pageIndex: 0, pageSize: 50 };
  private globalFilter = "";
  private filterRevision = 0;
  private quickFilters: QuickFilters = {
    ultraOnly: false,
    visionCertifiedOnly: false,
    platforms: new Set(),
    hardware: new Set(),
  };
  private tableWrap!: HTMLDivElement;
  private tbody!: HTMLTableSectionElement;
  private pager!: HTMLDivElement;
  private onStatus?: (status: GridStatus) => void;
  private table;

  constructor(root: HTMLElement, data: CatalogGame[], onStatus?: (status: GridStatus) => void) {
    this.root = root;
    this.data = data;
    this.onStatus = onStatus;
    const self = this;
    this.table = createTable({
      get data() {
        return self.data;
      },
      columns,
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
      },
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
      globalFilterFn: (row) => self.matchesQuickFilters(row.original),
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

  setQuickFilters(filters: QuickFilters): void {
    this.quickFilters = filters;
    this.pagination = { ...this.pagination, pageIndex: 0 };
    this.bumpFilters();
  }

  private bumpFilters(): void {
    this.filterRevision += 1;
    this.draw();
  }

  private matchesQuickFilters(game: CatalogGame): boolean {
    if (this.quickFilters.ultraOnly && game.bestLevel !== "ultra3d" && game.bestLevel !== "native3d") {
      return false;
    }
    if (this.quickFilters.visionCertifiedOnly) {
      const nvidia = game.sources.find((s) => s.sourceId === "nvidia-3d-vision");
      if (!nvidia || nvidia.label !== "3D Vision Ready") return false;
    }
    if (this.quickFilters.platforms.size > 0) {
      const hit = [...this.quickFilters.platforms].some((p) => game.platforms.includes(p));
      if (!hit) return false;
    }
    if (this.quickFilters.hardware.size > 0) {
      const hit = [...this.quickFilters.hardware].some((h) =>
        game.hardwareRequirements.displays.includes(h),
      );
      if (!hit) return false;
    }
    if (this.globalFilter) {
      const hay = [
        game.title,
        game.platforms.join(" "),
        game.steamTags?.join(" ") ?? "",
        game.steamStats?.tags?.join(" ") ?? "",
        game.bestExperience?.label ?? "",
      ]
        .join(" ")
        .toLowerCase();
      if (!hay.includes(this.globalFilter)) return false;
    }
    return true;
  }

  private mount(): void {
    this.tableWrap = document.createElement("div");
    this.tableWrap.className = "table-wrap";
    this.tableWrap.innerHTML = `<table><thead></thead><tbody></tbody></table>`;
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
      if ((header.column.columnDef.meta as { steam?: boolean } | undefined)?.steam) {
        th.dataset.source = "steam-store";
      }
      if (header.column.getCanSort()) {
        th.dataset.sort = header.column.id;
        th.addEventListener("click", (event) => {
          header.column.toggleSorting(undefined, event.shiftKey);
        });
      }
      th.textContent = String(header.column.columnDef.header ?? header.column.id);
      sortRow.append(th);

      const filterTh = document.createElement("th");
      if (header.column.getCanFilter()) {
        const input = document.createElement("input");
        input.type = "search";
        input.placeholder = "Filter…";
        input.setAttribute("aria-label", `Filter ${header.column.id}`);
        input.value = String(header.column.getFilterValue() ?? "");
        input.addEventListener("input", () => {
          header.column.setFilterValue(input.value || undefined);
        });
        filterTh.append(input);
      }
      filterRow.append(filterTh);
    }

    thead.replaceChildren(sortRow, filterRow);
  }

  private draw(): void {
    const rows = this.table.getRowModel().rows;
    if (rows.length === 0) {
      this.tbody.innerHTML = `<tr><td colspan="${columns.length}" class="empty">No games match the current filters.</td></tr>`;
    } else {
      this.tbody.innerHTML = rows
        .map((row) => {
          const cells = row.getVisibleCells().map((cell) => {
            const steam = (cell.column.columnDef.meta as { steam?: boolean } | undefined)?.steam;
            const attr = steam ? ' data-source="steam-store"' : "";
            const rendered = cell.column.columnDef.cell;
            const value =
              typeof rendered === "function" ? rendered(cell.getContext()) : cell.getValue();
            return `<td${attr}>${value ?? ""}</td>`;
          });
          return `<tr>${cells.join("")}</tr>`;
        })
        .join("");
    }

    this.renderPager();
    this.onStatus?.({
      filtered: this.table.getFilteredRowModel().rows.length,
      total: this.data.length,
      page: this.pagination.pageIndex + 1,
      pageCount: this.table.getPageCount(),
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
      <label>Rows
        <select data-page-size>
          ${PAGE_SIZES.map((n) => `<option value="${n}" ${n === pageSize ? "selected" : ""}>${n}</option>`).join("")}
        </select>
      </label>
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

    this.pager.querySelector<HTMLSelectElement>("select[data-page-size]")?.addEventListener(
      "change",
      (e) => {
        this.table.setPageSize(Number((e.target as HTMLSelectElement).value));
      },
    );
  }
}
