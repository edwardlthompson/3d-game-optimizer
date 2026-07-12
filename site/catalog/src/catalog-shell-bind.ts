import type { ListFilterMode } from "./list-filter";

export interface CatalogToolbarHandlers {
  onSearch: (query: string) => void;
  onWishlistFilter: (mode: ListFilterMode) => void;
  onLibraryFilter: (mode: ListFilterMode) => void;
  onUltraOnly: (enabled: boolean) => void;
  onVisionCertified: (enabled: boolean) => void;
  onTrueGameOnly: (enabled: boolean) => void;
  onUevrOnly: (enabled: boolean) => void;
  onMinRank3D: (min: number) => void;
  onExportWishlist: () => void;
  onImportWishlist: (json: string) => void;
  onExportLibrary: () => void;
  onImportLibrary: (json: string) => void;
  onConnectSteam: () => void;
  onDisconnectSteam: () => void;
  onResyncSteam: () => void;
}

function readListFilter(select: HTMLSelectElement | null): ListFilterMode {
  const value = select?.value;
  if (value === "only" || value === "exclude") return value;
  return "all";
}

function readImportFile(input: HTMLInputElement, onText: (text: string) => void): void {
  const file = input.files?.[0];
  if (!file) return;
  void file.text().then(onText);
}

export function bindCatalogToolbar(appRoot: HTMLElement, handlers: CatalogToolbarHandlers): void {
  appRoot.querySelector<HTMLInputElement>("#search")?.addEventListener("input", (e) => {
    handlers.onSearch((e.target as HTMLInputElement).value);
  });
  appRoot.querySelector<HTMLSelectElement>("#wishlist-filter")?.addEventListener("change", (e) => {
    handlers.onWishlistFilter(readListFilter(e.target as HTMLSelectElement));
  });
  appRoot.querySelector<HTMLSelectElement>("#library-filter")?.addEventListener("change", (e) => {
    handlers.onLibraryFilter(readListFilter(e.target as HTMLSelectElement));
  });
  appRoot.querySelector<HTMLInputElement>("#ultra-only")?.addEventListener("change", (e) => {
    handlers.onUltraOnly((e.target as HTMLInputElement).checked);
  });
  appRoot.querySelector<HTMLInputElement>("#vision-certified")?.addEventListener("change", (e) => {
    handlers.onVisionCertified((e.target as HTMLInputElement).checked);
  });
  appRoot.querySelector<HTMLInputElement>("#truegame-only")?.addEventListener("change", (e) => {
    handlers.onTrueGameOnly((e.target as HTMLInputElement).checked);
  });
  appRoot.querySelector<HTMLInputElement>("#uevr-only")?.addEventListener("change", (e) => {
    handlers.onUevrOnly((e.target as HTMLInputElement).checked);
  });
  appRoot.querySelector<HTMLSelectElement>("#min-rank-3d")?.addEventListener("change", (e) => {
    handlers.onMinRank3D(Number((e.target as HTMLSelectElement).value) || 0);
  });
  appRoot.querySelector<HTMLButtonElement>("#export-wishlist")?.addEventListener("click", () => {
    handlers.onExportWishlist();
  });
  appRoot.querySelector<HTMLInputElement>("#import-wishlist")?.addEventListener("change", (e) => {
    readImportFile(e.target as HTMLInputElement, handlers.onImportWishlist);
  });
  appRoot.querySelector<HTMLButtonElement>("#export-library")?.addEventListener("click", () => {
    handlers.onExportLibrary();
  });
  appRoot.querySelector<HTMLInputElement>("#import-library")?.addEventListener("change", (e) => {
    readImportFile(e.target as HTMLInputElement, handlers.onImportLibrary);
  });
  appRoot.querySelector<HTMLButtonElement>("#connect-steam")?.addEventListener("click", () => {
    handlers.onConnectSteam();
  });
  appRoot.querySelector<HTMLButtonElement>("#resync-steam")?.addEventListener("click", () => {
    handlers.onResyncSteam();
  });
  appRoot.querySelector<HTMLButtonElement>("#disconnect-steam")?.addEventListener("click", () => {
    handlers.onDisconnectSteam();
  });
  appRoot.querySelector<HTMLAnchorElement>(".footer-summary a")?.addEventListener("click", (e) => {
    e.stopPropagation();
  });
}

export function libraryMergeMode(appRoot: HTMLElement): "merge" | "replace" {
  return appRoot.querySelector<HTMLInputElement>("#replace-library")?.checked ? "replace" : "merge";
}

export function applyToolbarPrefsToDom(
  appRoot: HTMLElement,
  prefs: {
    wishlistFilter: ListFilterMode;
    libraryFilter: ListFilterMode;
    ultraOnly: boolean;
    visionCertifiedOnly: boolean;
    trueGameOnly: boolean;
    uevrOnly: boolean;
    minRank3D: number;
  },
): void {
  const wish = appRoot.querySelector<HTMLSelectElement>("#wishlist-filter");
  const lib = appRoot.querySelector<HTMLSelectElement>("#library-filter");
  const ultra = appRoot.querySelector<HTMLInputElement>("#ultra-only");
  const vision = appRoot.querySelector<HTMLInputElement>("#vision-certified");
  const tg = appRoot.querySelector<HTMLInputElement>("#truegame-only");
  const uevr = appRoot.querySelector<HTMLInputElement>("#uevr-only");
  const minRank = appRoot.querySelector<HTMLSelectElement>("#min-rank-3d");
  if (wish) wish.value = prefs.wishlistFilter;
  if (lib) lib.value = prefs.libraryFilter;
  if (ultra) ultra.checked = prefs.ultraOnly;
  if (vision) vision.checked = prefs.visionCertifiedOnly;
  if (tg) tg.checked = prefs.trueGameOnly;
  if (uevr) uevr.checked = prefs.uevrOnly;
  if (minRank) minRank.value = String(prefs.minRank3D);
}
