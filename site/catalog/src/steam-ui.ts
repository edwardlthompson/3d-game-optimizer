import type { CatalogGame } from "./types";
import type { SteamSyncStats } from "./steam-library-sync";
import type { LibraryMergeMode } from "./library";
import { escapeHtml } from "./utils";

export interface SteamUiContext {
  appRoot: HTMLElement;
  getGames: () => CatalogGame[];
  getMergeMode: () => LibraryMergeMode;
  refreshGrid: () => void;
}

export function showSteamBanner(
  ctx: SteamUiContext,
  stats: SteamSyncStats | null,
  error: string | null,
  emptyLibrary: boolean,
): void {
  const banner = ctx.appRoot.querySelector<HTMLDivElement>("#steam-sync-banner");
  if (!banner) return;

  if (error) {
    banner.hidden = false;
    banner.className = "banner steam-sync-banner error";
    banner.innerHTML = escapeHtml(error);
    return;
  }

  if ((emptyLibrary && !stats) || (stats && emptyLibrary && stats.ownedTotal === 0)) {
    banner.hidden = false;
    banner.className = "banner steam-sync-banner warn";
    banner.innerHTML = `
      <strong>Steam returned no owned games.</strong>
      Set <a href="https://steamcommunity.com/my/edit/settings" target="_blank" rel="noopener noreferrer">Game details</a>
      to Public and retry Connect Steam.
      <p class="muted">For private libraries, make Game details public and connect again. API keys are not stored on this device.</p>`;
    return;
  }

  if (stats) {
    banner.hidden = false;
    banner.className = "banner steam-sync-banner success";
    banner.innerHTML = `
      <strong>Steam library synced.</strong>
      ${stats.catalogMatched} catalog titles matched ·
      ${stats.ownedTotal} owned on Steam ·
      ${stats.ownedUnmatched} owned games not in this 3D catalog.
      <span class="muted">(${stats.catalogNoSteamLink} catalog titles have no Steam link.)</span>`;
    return;
  }

  banner.hidden = true;
  banner.textContent = "";
}
