import "./style.css";
import { checkCatalogSync, verifyCatalogIntegrity } from "./catalog-integrity";
import { DATA_COVERAGE_GAPS, DONATE_URL } from "./data-coverage";
import { CatalogGrid, type GridOptions } from "./grid";
import { loadPriceHistory, type PriceHistoryDocument } from "./price-chart";
import type { ListFilterMode } from "./list-filter";
import { exportLibrary, importLibrary, loadLibrary } from "./library";
import type { CatalogDocument } from "./types";
import {
  disconnectSteam,
  handleSteamSyncReturn,
  isSteamSyncEnabled,
  loadSteamMeta,
  startSteamConnect,
} from "./steam-library-sync";
import { showSteamBanner, type SteamUiContext } from "./steam-ui";
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

function libraryMergeMode(): "merge" | "replace" {
  return appRoot.querySelector<HTMLInputElement>("#replace-library")?.checked ? "replace" : "merge";
}

function steamCtx(): SteamUiContext {
  return {
    appRoot,
    getGames: () => catalog?.games ?? [],
    getMergeMode: libraryMergeMode,
    refreshGrid: () => grid?.refreshLibrary(),
  };
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
  appRoot.querySelector<HTMLButtonElement>("#export-library")?.addEventListener("click", () => {
    const blob = new Blob([exportLibrary(loadLibrary())], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "3d-catalog-library.json";
    a.click();
    URL.revokeObjectURL(url);
  });
  appRoot.querySelector<HTMLInputElement>("#import-library")?.addEventListener("change", (e) => {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    file.text().then((text) => {
      importLibrary(text);
      grid?.refreshLibrary();
    });
  });
  appRoot.querySelector<HTMLButtonElement>("#connect-steam")?.addEventListener("click", () => {
    startSteamConnect();
  });
  appRoot.querySelector<HTMLButtonElement>("#disconnect-steam")?.addEventListener("click", () => {
    disconnectSteam();
    const status = appRoot.querySelector<HTMLSpanElement>("#steam-connected-status");
    if (status) status.textContent = "";
  });
  appRoot.querySelector<HTMLAnchorElement>(".footer-summary a")?.addEventListener("click", (e) => {
    e.stopPropagation();
  });
}

function shell(): void {
  const steamEnabled = isSteamSyncEnabled();
  const steamMeta = loadSteamMeta();
  const connectedHint =
    steamMeta.steamId && steamMeta.lastSyncAt
      ? `Last sync ${new Date(steamMeta.lastSyncAt).toLocaleString()}`
      : "";

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
      ${steamEnabled ? `<button type="button" id="connect-steam">Connect Steam</button>
      <button type="button" id="disconnect-steam" class="muted-btn">Disconnect</button>
      <label><input id="replace-library" type="checkbox" /> Replace library on sync</label>
      <span id="steam-connected-status" class="muted">${escapeHtml(connectedHint)}</span>` : ""}
      <label><input id="ultra-only" type="checkbox" /> 3D Ultra / native only</label>
      <label><input id="vision-certified" type="checkbox" /> 3D Vision certified</label>
      <button type="button" id="export-wishlist">Export wishlist</button>
      <label class="import-label">Import wishlist <input id="import-wishlist" type="file" accept="application/json" hidden /></label>
      <button type="button" id="export-library">Export library</button>
      <label class="import-label">Import library <input id="import-library" type="file" accept="application/json" hidden /></label>
    </div>
    <div class="banner" id="sync-banner" hidden></div>
    <div class="banner steam-sync-banner" id="steam-sync-banner" hidden></div>
    <div class="status"></div>
    <div id="grid-root"></div>
    <footer class="site-footer">
      <details class="footer-details">
        <summary class="footer-summary">
          <span>Game Rank = Steam + 3D · local wishlist/library${steamEnabled ? " · Steam sync available" : ""}</span>
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
  navigator.serviceWorker.register(`${base}sw.js`, { scope: base }).catch(() => undefined);
}

async function loadCatalog(): Promise<void> {
  const base = import.meta.env.BASE_URL;
  registerServiceWorker(base);
  const catalogRes = await fetch(`${base}data/catalog-v2.json`);
  if (!catalogRes.ok) throw new Error(`Failed to load catalog: ${catalogRes.status}`);
  const catalogText = await catalogRes.text();
  const integrity = await verifyCatalogIntegrity(base, catalogText);
  if (!integrity.ok) {
    throw new Error("Catalog integrity check failed — data may be corrupted.");
  }
  catalog = JSON.parse(catalogText) as CatalogDocument;
  priceHistory = await loadPriceHistory(base);

  shell();
  checkCatalogSync(catalog.meta.mergedAt, integrity.hash, () => {
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

  const steamResult = await handleSteamSyncReturn(catalog.games, libraryMergeMode());
  showSteamBanner(steamCtx(), steamResult.stats, steamResult.error, steamResult.emptyLibrary);
  if (steamResult.stats) {
    grid.refreshLibrary();
    const steamStatus = appRoot.querySelector<HTMLSpanElement>("#steam-connected-status");
    const meta = loadSteamMeta();
    if (steamStatus && meta.lastSyncAt) {
      steamStatus.textContent = `Last sync ${new Date(meta.lastSyncAt).toLocaleString()}`;
    }
  }

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
