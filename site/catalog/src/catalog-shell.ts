import { DATA_COVERAGE_GAPS, DONATE_URL } from "./data-coverage";
import type { ListFilterMode } from "./list-filter";
import { escapeHtml } from "./utils";

export interface CatalogShellOptions {
  steamEnabled: boolean;
  connectedHint: string;
}

export interface CatalogToolbarHandlers {
  onSearch: (query: string) => void;
  onWishlistFilter: (mode: ListFilterMode) => void;
  onLibraryFilter: (mode: ListFilterMode) => void;
  onUltraOnly: (enabled: boolean) => void;
  onVisionCertified: (enabled: boolean) => void;
  onExportWishlist: () => void;
  onImportWishlist: (json: string) => void;
  onExportLibrary: () => void;
  onImportLibrary: (json: string) => void;
  onConnectSteam: () => void;
  onDisconnectSteam: () => void;
}

export function renderCatalogShell(appRoot: HTMLElement, options: CatalogShellOptions): void {
  const { steamEnabled, connectedHint } = options;
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
