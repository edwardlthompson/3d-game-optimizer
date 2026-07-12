import { DATA_COVERAGE_GAPS, DONATE_URL } from "./data-coverage";
import { escapeHtml } from "./utils";

export type { CatalogToolbarHandlers } from "./catalog-shell-bind";
export {
  applyToolbarPrefsToDom,
  bindCatalogToolbar,
  libraryMergeMode,
} from "./catalog-shell-bind";

export interface CatalogShellOptions {
  steamEnabled: boolean;
  connectedHint: string;
}

function steamControlsHtml(steamEnabled: boolean, connectedHint: string): string {
  if (steamEnabled) {
    return `<button type="button" id="connect-steam">Connect Steam</button>
      <button type="button" id="resync-steam" class="muted-btn">Resync library</button>
      <button type="button" id="disconnect-steam" class="muted-btn">Disconnect</button>
      <label><input id="replace-library" type="checkbox" /> Replace library on sync</label>
      <span id="steam-connected-status" class="muted">${escapeHtml(connectedHint)}</span>`;
  }
  return `<span id="steam-unavailable" class="muted" title="Deploy the Steam sync worker to enable Connect Steam">Steam sync unavailable — worker not configured</span>`;
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
      ${steamControlsHtml(steamEnabled, connectedHint)}
      <label><input id="ultra-only" type="checkbox" /> 3D Ultra / native only</label>
      <label><input id="vision-certified" type="checkbox" /> 3D Vision certified</label>
      <label><input id="truegame-only" type="checkbox" /> TrueGame</label>
      <label><input id="uevr-only" type="checkbox" /> UEVR</label>
      <label class="filter-select">Min 3D Rank
        <select id="min-rank-3d" aria-label="Minimum 3D Rank">
          <option value="0">Any</option>
          <option value="26">Experimental (26+)</option>
          <option value="42">Playable (42+)</option>
          <option value="58">Optimized (58+)</option>
          <option value="72">Native (72+)</option>
          <option value="88">Ultra (88+)</option>
        </select>
      </label>
      <button type="button" id="export-wishlist">Export wishlist</button>
      <label class="import-label">Import wishlist <input id="import-wishlist" type="file" accept="application/json" hidden /></label>
      <button type="button" id="export-library">Export library</button>
      <label class="import-label">Import library <input id="import-library" type="file" accept="application/json" hidden /></label>
    </div>
    <div class="banner" id="sync-banner" hidden></div>
    <div class="banner steam-sync-banner" id="steam-sync-banner" hidden></div>
    <div class="status"></div>
    <div id="grid-root"></div>
    <div id="unmatched-modal" class="price-overlay" hidden></div>
    <footer class="site-footer">
      <details class="footer-details">
        <summary class="footer-summary">
          <span>Game Rank = Steam + 3D · local wishlist/library${steamEnabled ? " · Steam sync available" : " · Steam sync not configured"}</span>
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
