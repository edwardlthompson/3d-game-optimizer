import { mountCatalogShell } from "./catalog-mount";
import { loadToolbarPrefs, prefsToGridOptions, saveToolbarPrefs } from "./catalog-prefs";
import { checkCatalogSync, verifyCatalogIntegrity } from "./catalog-integrity";
import { CatalogGrid, type GridOptions } from "./grid";
import { loadPriceHistory, type PriceHistoryDocument } from "./price-chart";
import { libraryMergeMode } from "./catalog-shell";
import { loadSteamMeta } from "./library";
import type { CatalogDocument } from "./types";
import { handleSteamSyncReturn } from "./steam-library-sync";
import { showSteamBanner, showSteamLoading, type SteamUiContext } from "./steam-ui";
import { readSteamReturnParams } from "./steam-library-sync-url";

function registerServiceWorker(base: string): void {
  if (!("serviceWorker" in navigator)) return;
  navigator.serviceWorker.register(`${base}sw.js`, { scope: base }).catch(() => undefined);
}

export async function bootstrapCatalog(appRoot: HTMLElement): Promise<void> {
  let catalog: CatalogDocument | null = null;
  let priceHistory: PriceHistoryDocument | null = null;
  let grid: CatalogGrid | null = null;

  const prefs = loadToolbarPrefs();
  const gridOptions: GridOptions = prefsToGridOptions(prefs);

  const persistAndSync = (): void => {
    saveToolbarPrefs({
      wishlistFilter: gridOptions.wishlistFilter,
      libraryFilter: gridOptions.libraryFilter,
      ultraOnly: gridOptions.ultraOnly,
      visionCertifiedOnly: gridOptions.visionCertifiedOnly,
      trueGameOnly: gridOptions.trueGameOnly,
      uevrOnly: gridOptions.uevrOnly,
      minRank3D: gridOptions.minRank3D,
    });
    grid?.setOptions({ ...gridOptions });
  };

  const steamCtx = (): SteamUiContext => ({
    appRoot,
    getGames: () => catalog?.games ?? [],
    getMergeMode: () => libraryMergeMode(appRoot),
    refreshGrid: () => grid?.refreshLibrary(),
  });

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

  mountCatalogShell({
    appRoot,
    gridOptions,
    getGrid: () => grid,
    persistAndSync,
  });
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
  grid.setOptions({ ...gridOptions });

  const pendingSteam = readSteamReturnParams();
  if (pendingSteam.token || pendingSteam.error) {
    showSteamLoading(steamCtx());
  }
  const steamResult = await handleSteamSyncReturn(catalog.games, libraryMergeMode(appRoot));
  showSteamBanner(steamCtx(), steamResult.stats, steamResult.error, steamResult.emptyLibrary);
  if (steamResult.stats) {
    grid.refreshLibrary();
    const steamStatus = appRoot.querySelector<HTMLSpanElement>("#steam-connected-status");
    const meta = loadSteamMeta();
    if (steamStatus && meta.lastSyncAt) {
      steamStatus.textContent = `Last sync ${new Date(meta.lastSyncAt).toLocaleString()}`;
    }
  }

  const appId = new URLSearchParams(window.location.search).get("appId");
  if (appId) {
    const search = appRoot.querySelector<HTMLInputElement>("#search");
    if (search) search.value = appId;
    grid.setGlobalFilter(appId);
  }
}
