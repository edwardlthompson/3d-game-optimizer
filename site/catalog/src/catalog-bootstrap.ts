import { bindCatalogToolbar, libraryMergeMode, renderCatalogShell } from "./catalog-shell";
import { checkCatalogSync, verifyCatalogIntegrity } from "./catalog-integrity";
import { CatalogGrid, type GridOptions } from "./grid";
import { loadPriceHistory, type PriceHistoryDocument } from "./price-chart";
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
import { exportWishlist, importWishlist, loadWishlist } from "./wishlist";

function downloadJson(filename: string, json: string): void {
  const blob = new Blob([json], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

function registerServiceWorker(base: string): void {
  if (!("serviceWorker" in navigator)) return;
  navigator.serviceWorker.register(`${base}sw.js`, { scope: base }).catch(() => undefined);
}

export async function bootstrapCatalog(appRoot: HTMLElement): Promise<void> {
  let catalog: CatalogDocument | null = null;
  let priceHistory: PriceHistoryDocument | null = null;
  let grid: CatalogGrid | null = null;

  const gridOptions: GridOptions = {
    wishlistFilter: "all",
    libraryFilter: "all",
    ultraOnly: false,
    visionCertifiedOnly: false,
  };

  const syncGridOptions = (): void => {
    grid?.setOptions({ ...gridOptions });
  };

  const steamCtx = (): SteamUiContext => ({
    appRoot,
    getGames: () => catalog?.games ?? [],
    getMergeMode: () => libraryMergeMode(appRoot),
    refreshGrid: () => grid?.refreshLibrary(),
  });

  const mountShell = (): void => {
    const steamMeta = loadSteamMeta();
    const connectedHint =
      steamMeta.steamId && steamMeta.lastSyncAt
        ? `Last sync ${new Date(steamMeta.lastSyncAt).toLocaleString()}`
        : "";
    renderCatalogShell(appRoot, { steamEnabled: isSteamSyncEnabled(), connectedHint });
    bindCatalogToolbar(appRoot, {
      onSearch: (query) => grid?.setGlobalFilter(query),
      onWishlistFilter: (mode) => {
        gridOptions.wishlistFilter = mode;
        syncGridOptions();
      },
      onLibraryFilter: (mode) => {
        gridOptions.libraryFilter = mode;
        syncGridOptions();
      },
      onUltraOnly: (enabled) => {
        gridOptions.ultraOnly = enabled;
        syncGridOptions();
      },
      onVisionCertified: (enabled) => {
        gridOptions.visionCertifiedOnly = enabled;
        syncGridOptions();
      },
      onExportWishlist: () => downloadJson("3d-catalog-wishlist.json", exportWishlist(loadWishlist())),
      onImportWishlist: (text) => {
        importWishlist(text);
        grid?.setOptions({ ...gridOptions });
      },
      onExportLibrary: () => downloadJson("3d-catalog-library.json", exportLibrary(loadLibrary())),
      onImportLibrary: (text) => {
        importLibrary(text);
        grid?.refreshLibrary();
      },
      onConnectSteam: () => startSteamConnect(),
      onDisconnectSteam: () => {
        disconnectSteam();
        const status = appRoot.querySelector<HTMLSpanElement>("#steam-connected-status");
        if (status) status.textContent = "";
      },
    });
  };

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

  mountShell();
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
