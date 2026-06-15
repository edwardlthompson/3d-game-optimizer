import "./style.css";
import { DATA_COVERAGE_GAPS, DONATE_URL } from "./data-coverage";
import { CatalogGrid, type GridOptions } from "./grid";
import { checkCatalogSync, loadPriceHistory, type PriceHistoryDocument } from "./price-chart";
import type { ListFilterMode } from "./list-filter";
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
  wishlistFilter: "all",
  libraryFilter: "all",
  ultraOnly: false,
  visionCertifiedOnly: false,
};

function syncGridOptions(): void {
  grid?.setOptions({ ...gridOptions });
}

function readListFilter(select: HTMLSelectElement | null): ListFilterMode {
  const value = select?.value;
  if (value === "only" || value === "exclude") return value;
  return "all";
}

function bindToolbar(): void {
  appRoot.querySelector<HTMLInputElement>("#search")?.addEventListener("input", (e) => {
    grid?.setGlobalFilter((e.target as HTMLInputElement).value);
  });
  appRoot.querySelector<HTMLSelectElement>("#wishlist-filter")?.addEventListener("change", (e) => {
    gridOptions.wishlistFilter = readListFilter(e.target as HTMLSelectElement);
    syncGridOptions();
  });
  appRoot.querySelector<HTMLSelectElement>("#library-filter")?.addEventListener("change", (e) => {
    gridOptions.libraryFilter = readListFilter(e.target as HTMLSelectElement);
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
  appRoot.querySelector<HTMLAnchorElement>(".footer-summary a")?.addEventListener("click", (e) => {
    e.stopPropagation();
  });
}

function shell(): void {
  appRoot.innerHTML = `
    <header>
      <h1>3D Game Catalog</h1>
      <p>Lenticular 3D titles — filter by play method, track your library and wishlist locally, Steam prices over time.</p>
    </header>
    <div class="toolbar">
      <input id="search" type="search" placeholder="Search title, play method…" aria-label="Search games" />
      <label class="filter-select">Wishlist
        <select id="wishlist-filter" aria-label="Wishlist filter">
          <option value="all">All titles</option>
          <option value="only">Wishlist only</option>
          <option value="exclude">Exclude wishlist</option>
        </select>
      </label>
      <label class="filter-select">Library
        <select id="library-filter" aria-label="Library filter">
          <option value="all">All titles</option>
          <option value="only">Library only</option>
          <option value="exclude">Exclude library</option>
        </select>
      </label>
      <label><input id="ultra-only" type="checkbox" /> 3D Ultra / native only</label>
      <label><input id="vision-certified" type="checkbox" /> 3D Vision certified</label>
      <button type="button" id="export-wishlist">Export wishlist</button>
      <label class="import-label">Import <input id="import-wishlist" type="file" accept="application/json" hidden /></label>
    </div>
    <div class="banner" id="sync-banner" hidden></div>
    <div class="status"></div>
    <div id="grid-root"></div>
    <footer class="site-footer">
      <details class="footer-details">
        <summary class="footer-summary">
          <span>3D Rank = top path score · Steam Rank = weighted reviews · local wishlist/library</span>
          <span class="footer-summary-actions">
            <a href="${escapeHtml(DONATE_URL)}" target="_blank" rel="noopener noreferrer">Support on Venmo</a>
            <span class="footer-expand-hint">Details ▾</span>
          </span>
        </summary>
        <ul class="footer-notes">${DATA_COVERAGE_GAPS.map((g) => `<li>${escapeHtml(g)}</li>`).join("")}</ul>
      </details>
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
