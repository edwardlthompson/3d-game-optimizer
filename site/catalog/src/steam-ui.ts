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

export function showSteamLoading(ctx: SteamUiContext, message = "Syncing your Steam library…"): void {
  const banner = ctx.appRoot.querySelector<HTMLDivElement>("#steam-sync-banner");
  if (!banner) return;
  banner.hidden = false;
  banner.className = "banner steam-sync-banner";
  banner.innerHTML = `<strong>${escapeHtml(message)}</strong>`;
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
    banner.innerHTML = `${escapeHtml(error)} <button type="button" class="muted-btn" id="steam-banner-dismiss">Dismiss</button>
      <button type="button" id="steam-banner-retry">Retry Connect Steam</button>`;
    bindBannerActions(ctx, banner, null);
    return;
  }

  if ((emptyLibrary && !stats) || (stats && emptyLibrary && stats.ownedTotal === 0)) {
    banner.hidden = false;
    banner.className = "banner steam-sync-banner warn";
    banner.innerHTML = `
      <strong>Steam returned no owned games.</strong>
      Set <a href="https://steamcommunity.com/my/edit/settings" target="_blank" rel="noopener noreferrer">Game details</a>
      to Public and retry Connect Steam.
      <p class="muted">For private libraries, make Game details public and connect again. API keys are not stored on this device.</p>
      <button type="button" class="muted-btn" id="steam-banner-dismiss">Dismiss</button>`;
    bindBannerActions(ctx, banner, null);
    return;
  }

  if (stats) {
    banner.hidden = false;
    banner.className = "banner steam-sync-banner success";
    const unmatchedBtn =
      stats.ownedUnmatched > 0
        ? `<button type="button" id="steam-unmatched-btn">${stats.ownedUnmatched} owned games not in this 3D catalog</button>`
        : `${stats.ownedUnmatched} owned games not in this 3D catalog`;
    banner.innerHTML = `
      <strong>Steam library synced.</strong>
      ${stats.catalogMatched} catalog titles matched ·
      ${stats.ownedTotal} owned on Steam ·
      ${unmatchedBtn}.
      <span class="muted">(${stats.catalogNoSteamLink} catalog titles have no Steam link.)</span>
      <button type="button" class="muted-btn" id="steam-banner-dismiss">Dismiss</button>`;
    bindBannerActions(ctx, banner, stats);
    return;
  }

  banner.hidden = true;
  banner.textContent = "";
}

function bindBannerActions(ctx: SteamUiContext, banner: HTMLElement, stats: SteamSyncStats | null): void {
  banner.querySelector("#steam-banner-dismiss")?.addEventListener("click", () => {
    banner.hidden = true;
  });
  banner.querySelector("#steam-banner-retry")?.addEventListener("click", () => {
    const connect = ctx.appRoot.querySelector<HTMLButtonElement>("#connect-steam");
    connect?.click();
  });
  banner.querySelector("#steam-unmatched-btn")?.addEventListener("click", () => {
    if (stats) showUnmatchedModal(ctx, stats.unmatchedAppIds);
  });
}

export function showUnmatchedModal(ctx: SteamUiContext, appIds: number[]): void {
  const modal = ctx.appRoot.querySelector<HTMLDivElement>("#unmatched-modal");
  if (!modal) return;
  const list = appIds
    .slice(0, 200)
    .map(
      (id) =>
        `<li><a href="https://store.steampowered.com/app/${id}/" target="_blank" rel="noopener noreferrer">${id}</a></li>`,
    )
    .join("");
  modal.hidden = false;
  modal.innerHTML = `
    <div class="price-dialog" role="dialog" aria-label="Unmatched Steam games">
      <header><h2>Owned on Steam, not in this catalog</h2>
      <button type="button" class="close-btn" aria-label="Close">×</button></header>
      <p class="muted">${appIds.length} App ID(s). Showing up to 200.</p>
      <ul class="unmatched-list">${list}</ul>
    </div>`;
  modal.querySelector(".close-btn")?.addEventListener("click", () => {
    modal.hidden = true;
    modal.innerHTML = "";
  });
  modal.addEventListener("click", (e) => {
    if (e.target === modal) {
      modal.hidden = true;
      modal.innerHTML = "";
    }
  });
}

export function confirmReplaceLibrary(): boolean {
  return window.confirm(
    "Replace library on sync will remove manual Lib marks that are not in your Steam library. Continue?",
  );
}

export function confirmDisconnect(): "keep" | "clear" | "cancel" {
  if (!window.confirm("Disconnect Steam connection?")) return "cancel";
  if (window.confirm("Also remove Lib marks from the last Steam sync?")) return "clear";
  return "keep";
}
