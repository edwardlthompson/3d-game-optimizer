import "./style.css";
import { DATA_COVERAGE_GAPS } from "./data-coverage";
import { CatalogGrid, type GridOptions } from "./grid";
import { checkCatalogSync, loadPriceHistory, type PriceHistoryDocument } from "./price-chart";
import type { CatalogDocument } from "./types";
import { escapeHtml } from "./utils";
import { exportWishlist, importWishlist, loadWishlist } from "./wishlist";

const maybeRoot = document.querySelector<HTMLDivElement>("#app");
if (!maybeRoot) throw new Error("Missing #app root");
const appRoot: HTMLDivElement = maybeRoot;

let catalog: CatalogDocument | null = null;
let priceHistory: PriceHistoryDocument | null = null;
let grid: CatalogGrid | null = null;

const gridOptions: GridOptions = {
  wishlistOnly: false,
  ultraOnly: false,
  visionCertifiedOnly: false,
};

function syncGridOptions(): void {
  grid?.setOptions({ ...gridOptions });
}

function bindToolbar(): void {
  appRoot.querySelector<HTMLInputElement>("#search")?.addEventListener("input", (e) => {
    grid?.setGlobalFilter((e.target as HTMLInputElement).value);
  });
  appRoot.querySelector<HTMLInputElement>("#wishlist-only")?.addEventListener("change", (e) => {
    gridOptions.wishlistOnly = (e.target as HTMLInputElement).checked;
    syncGridOptions();
  });
  appRoot.querySelector<HTMLInputElement>("#ultra-only")?.addEventListener("change", (e) => {
    gridOptions.ultraOnly = (e.target as HTMLInputElement).checked;
    syncGridOptions();
  });
  appRoot.querySelector<HTMLInputElement>("#vision-certified")?.addEventListener("change", (e) => {
    gridOptions.visionCertifiedOnly = (e.target as HTMLInputElement).checked;
    syncGridOptions();
  });
  appRoot.querySelector<HTMLButtonElement>("#export-wishlist")?.addEventListener("click", () => {
    const blob = new Blob([exportWishlist(loadWishlist())], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "3d-catalog-wishlist.json";
    a.click();
    URL.revokeObjectURL(url);
  });
  appRoot.querySelector<HTMLInputElement>("#import-wishlist")?.addEventListener("change", (e) => {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    file.text().then((text) => {
      importWishlist(text);
      grid?.setOptions({ ...gridOptions });
    });
  });
}

function shell(): void {
  appRoot.innerHTML = `
    <header>
      <h1>3D Game Catalog</h1>
      <p>Lenticular 3D titles — filter by play method, wishlist locally, track Steam prices over time.</p>
    </header>
    <div class="toolbar">
      <input id="search" type="search" placeholder="Search title, play method…" aria-label="Search games" />
      <label><input id="wishlist-only" type="checkbox" /> Wishlist only</label>
      <label><input id="ultra-only" type="checkbox" /> 3D Ultra / native only</label>
      <label><input id="vision-certified" type="checkbox" /> 3D Vision certified</label>
      <button type="button" id="export-wishlist">Export wishlist</button>
      <label class="import-label">Import <input id="import-wishlist" type="file" accept="application/json" hidden /></label>
    </div>
    <div class="banner" id="sync-banner" hidden></div>
    <div class="status"></div>
    <div id="grid-root"></div>
    <footer>
      <div>Wishlist stored on this device only. Price history self-tracked each catalog sync (SteamDB backfill later).</div>
      <ul>${DATA_COVERAGE_GAPS.map((g) => `<li>${escapeHtml(g)}</li>`).join("")}</ul>
    </footer>
  `;
  bindToolbar();
}

function registerServiceWorker(base: string): void {
  if (!("serviceWorker" in navigator)) return;
  const swUrl = `${base}sw.js`;
  navigator.serviceWorker.register(swUrl, { scope: base }).catch(() => undefined);
}

async function loadCatalog(): Promise<void> {
  const base = import.meta.env.BASE_URL;
  registerServiceWorker(base);
  const [catalogRes, history] = await Promise.all([
    fetch(`${base}data/catalog-v2.json`),
    loadPriceHistory(base),
  ]);
  if (!catalogRes.ok) throw new Error(`Failed to load catalog: ${catalogRes.status}`);
  catalog = (await catalogRes.json()) as CatalogDocument;
  priceHistory = history;

  shell();
  checkCatalogSync(catalog.meta.mergedAt, () => {
    const banner = appRoot.querySelector<HTMLDivElement>("#sync-banner");
    if (banner) {
      banner.hidden = false;
      banner.textContent = `Catalog updated ${catalog!.meta.mergedAt}. Click a price to view history.`;
    }
  });

  const gridRoot = appRoot.querySelector<HTMLDivElement>("#grid-root");
  const status = appRoot.querySelector<HTMLDivElement>(".status");
  if (!gridRoot) throw new Error("Missing grid root");

  grid = new CatalogGrid(appRoot, gridRoot, catalog.games, priceHistory, (s) => {
    if (status) {
      status.textContent = `${s.filtered} of ${s.total} titles · page ${s.page}/${Math.max(s.pageCount, 1)} · sync ${catalog!.meta.syncStatus} · merged ${catalog!.meta.mergedAt}`;
    }
  });

  const params = new URLSearchParams(window.location.search);
  const appId = params.get("appId");
  if (appId) {
    const search = appRoot.querySelector<HTMLInputElement>("#search");
    if (search) search.value = appId;
    grid.setGlobalFilter(appId);
  }
}

loadCatalog().catch((error: unknown) => {
  appRoot.innerHTML = `<div class="empty">Could not load catalog: ${escapeHtml(String(error))}</div>`;
});
