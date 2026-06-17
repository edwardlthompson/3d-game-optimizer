import { isLinkableSteamApp } from "./steam-constants";
import type { CatalogGame } from "./types";
import { escapeHtml } from "./utils";

export function titleCell(game: CatalogGame, title: string): string {
  if (isLinkableSteamApp(game.steamAppId, game.steamMatchConfidence)) {
    const appId = game.steamAppId;
    const storeUrl = `https://store.steampowered.com/app/${appId}/`;
    return `<a href="${storeUrl}" class="title-cell title-steam-link" target="_blank" rel="noopener noreferrer" data-steam-app="${appId}" aria-label="Open Steam store: ${escapeHtml(title)}">${escapeHtml(title)}</a>`;
  }
  return `<span class="title-cell" title="${escapeHtml(title)}">${escapeHtml(title)}</span>`;
}
