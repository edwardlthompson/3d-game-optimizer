import type { CatalogGrid } from "./grid";
import type { GridOptions } from "./grid-types";
import { clearSteamSyncedMarks, loadSteamMeta } from "./library";
import {
  disconnectSteam,
  isSteamSyncEnabled,
  startSteamConnect,
} from "./steam-library-sync";
import {
  applyToolbarPrefsToDom,
  bindCatalogToolbar,
  libraryMergeMode,
  renderCatalogShell,
  type CatalogToolbarHandlers,
} from "./catalog-shell";
import type { CatalogToolbarPrefs } from "./catalog-prefs";
import {
  confirmDisconnect,
  confirmReplaceLibrary,
} from "./steam-ui";
import { exportWishlist, importWishlist, loadWishlist } from "./wishlist";
import { exportLibrary, importLibrary, loadLibrary } from "./library";

function downloadJson(filename: string, json: string): void {
  const blob = new Blob([json], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

export function mountCatalogShell(args: {
  appRoot: HTMLElement;
  gridOptions: GridOptions;
  getGrid: () => CatalogGrid | null;
  persistAndSync: () => void;
}): void {
  const { appRoot, gridOptions, getGrid, persistAndSync } = args;
  const steamMeta = loadSteamMeta();
  const connectedHint =
    steamMeta.steamId && steamMeta.lastSyncAt
      ? `Last sync ${new Date(steamMeta.lastSyncAt).toLocaleString()}`
      : "";
  renderCatalogShell(appRoot, { steamEnabled: isSteamSyncEnabled(), connectedHint });
  applyToolbarPrefsToDom(appRoot, gridOptions as CatalogToolbarPrefs);

  const handlers: CatalogToolbarHandlers = {
    onSearch: (query) => getGrid()?.setGlobalFilter(query),
    onWishlistFilter: (mode) => {
      gridOptions.wishlistFilter = mode;
      persistAndSync();
    },
    onLibraryFilter: (mode) => {
      gridOptions.libraryFilter = mode;
      persistAndSync();
    },
    onUltraOnly: (enabled) => {
      gridOptions.ultraOnly = enabled;
      persistAndSync();
    },
    onVisionCertified: (enabled) => {
      gridOptions.visionCertifiedOnly = enabled;
      persistAndSync();
    },
    onTrueGameOnly: (enabled) => {
      gridOptions.trueGameOnly = enabled;
      persistAndSync();
    },
    onUevrOnly: (enabled) => {
      gridOptions.uevrOnly = enabled;
      persistAndSync();
    },
    onMinRank3D: (min) => {
      gridOptions.minRank3D = min;
      persistAndSync();
    },
    onExportWishlist: () => downloadJson("3d-catalog-wishlist.json", exportWishlist(loadWishlist())),
    onImportWishlist: (text) => {
      importWishlist(text);
      getGrid()?.setOptions({ ...gridOptions });
    },
    onExportLibrary: () => downloadJson("3d-catalog-library.json", exportLibrary(loadLibrary())),
    onImportLibrary: (text) => {
      importLibrary(text);
      getGrid()?.refreshLibrary();
    },
    onConnectSteam: () => {
      if (libraryMergeMode(appRoot) === "replace" && !confirmReplaceLibrary()) return;
      startSteamConnect();
    },
    onResyncSteam: () => {
      if (libraryMergeMode(appRoot) === "replace" && !confirmReplaceLibrary()) return;
      startSteamConnect();
    },
    onDisconnectSteam: () => {
      const choice = confirmDisconnect();
      if (choice === "cancel") return;
      if (choice === "clear") {
        clearSteamSyncedMarks(loadSteamMeta().lastMatchedIds ?? []);
        getGrid()?.refreshLibrary();
      }
      disconnectSteam();
      const status = appRoot.querySelector<HTMLSpanElement>("#steam-connected-status");
      if (status) status.textContent = "";
      const banner = appRoot.querySelector<HTMLDivElement>("#steam-sync-banner");
      if (banner) {
        banner.hidden = true;
        banner.textContent = "";
      }
    },
  };
  bindCatalogToolbar(appRoot, handlers);
}
