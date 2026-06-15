import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import {
  createColumnHelper,
  createTable,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
} from "@tanstack/table-core";

const here = dirname(fileURLToPath(import.meta.url));
const catalogPath = resolve(here, "../public/data/catalog-v2.json");
const catalog = JSON.parse(readFileSync(catalogPath, "utf8"));
const games = catalog.games ?? [];

if (games.length < 400) {
  console.error(`smoke-grid: expected >= 400 games, got ${games.length}`);
  process.exit(1);
}

const columnHelper = createColumnHelper();
const columns = [columnHelper.accessor("title", { id: "title", header: "Title" })];

const sorting = [{ id: "title", desc: false }];
const pagination = { pageIndex: 0, pageSize: 50 };
const columnFilters = [];
let filterRevision = 0;

const table = createTable({
  data: games,
  columns,
  state: {
    sorting,
    pagination,
    columnFilters,
    globalFilter: String(filterRevision),
    columnPinning: { left: [], right: [] },
  },
  initialState: { columnPinning: { left: [], right: [] } },
  getCoreRowModel: getCoreRowModel(),
  getFilteredRowModel: getFilteredRowModel(),
  getSortedRowModel: getSortedRowModel(),
  getPaginationRowModel: getPaginationRowModel(),
  globalFilterFn: () => true,
  onStateChange: () => undefined,
  renderFallbackValue: null,
});

const rows = table.getRowModel().rows;
if (rows.length === 0) {
  console.error("smoke-grid: table returned zero rows on first page");
  process.exit(1);
}

const withSupport = games.filter((g) => (g.platformSupport ?? []).length > 0).length;
if (withSupport < games.length * 0.9) {
  console.error(`smoke-grid: expected platformSupport on most games, got ${withSupport}/${games.length}`);
  process.exit(1);
}

console.log(`smoke-grid: ok (${games.length} games, page rows ${rows.length}, platformSupport ${withSupport})`);
