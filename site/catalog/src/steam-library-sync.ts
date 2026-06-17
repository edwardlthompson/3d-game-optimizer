import {
  clearSteamCredentials,
  loadSteamMeta,
  mergeLibraryFromCatalogIds,
  saveSteamMeta,
  type LibraryMergeMode,
} from "./library";
import { MIN_STEAM_MATCH_CONFIDENCE } from "./steam-constants";
import type { CatalogGame } from "./types";

export interface SteamSyncStats {
  catalogMatched: number;
  ownedTotal: number;
  ownedUnmatched: number;
  catalogNoSteamLink: number;
}

export interface SteamExchangeResult {
  appIds: number[];
  steamIdTruncated: string;
  emptyLibrary: boolean;
  source: "openid";
}

export function steamSyncWorkerUrl(): string {
  const url = import.meta.env.VITE_STEAM_SYNC_URL as string | undefined;
  return url?.replace(/\/$/, "") ?? "";
}

export function isSteamSyncEnabled(): boolean {
  return steamSyncWorkerUrl().length > 0;
}

export function startSteamConnect(): void {
  const base = steamSyncWorkerUrl();
  if (!base) return;
  window.location.href = `${base}/auth/steam`;
}

export function mapOwnedAppIdsToCatalogIds(
  games: CatalogGame[],
  appIds: number[],
): { matchedIds: string[]; stats: SteamSyncStats } {
  const owned = new Set(appIds);
  const linkableAppIds = new Set<number>();
  const matchedIds: string[] = [];

  for (const game of games) {
    const appId = game.steamAppId;
    const confidence = game.steamMatchConfidence ?? 0;
    if (!appId || confidence < MIN_STEAM_MATCH_CONFIDENCE) continue;
    linkableAppIds.add(appId);
    if (owned.has(appId)) matchedIds.push(game.id);
  }

  let ownedUnmatched = 0;
  for (const appId of owned) {
    if (!linkableAppIds.has(appId)) ownedUnmatched += 1;
  }

  const catalogNoSteamLink = games.filter((game) => {
    const confidence = game.steamMatchConfidence ?? 0;
    return !game.steamAppId || confidence < MIN_STEAM_MATCH_CONFIDENCE;
  }).length;

  return {
    matchedIds,
    stats: {
      catalogMatched: matchedIds.length,
      ownedTotal: owned.size,
      ownedUnmatched,
      catalogNoSteamLink,
    },
  };
}

export async function exchangeSyncToken(token: string): Promise<SteamExchangeResult> {
  const base = steamSyncWorkerUrl();
  const resp = await fetch(`${base}/sync/exchange`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token }),
  });
  if (!resp.ok) {
    throw new Error(`Sync exchange failed (${resp.status})`);
  }
  return (await resp.json()) as SteamExchangeResult;
}

export function applySteamSyncToLibrary(
  games: CatalogGame[],
  result: SteamExchangeResult,
  mode: LibraryMergeMode,
): SteamSyncStats {
  const { matchedIds, stats } = mapOwnedAppIdsToCatalogIds(games, result.appIds);
  mergeLibraryFromCatalogIds(matchedIds, mode);
  saveSteamMeta({
    steamId: result.steamIdTruncated,
    lastSyncAt: new Date().toISOString(),
  });
  return stats;
}

function readSteamReturnParams(): { token: string | null; error: string | null } {
  const hash = new URLSearchParams(window.location.hash.replace(/^#/, ""));
  const query = new URLSearchParams(window.location.search);
  return {
    token: hash.get("steam_sync_token") ?? query.get("steam_sync_token"),
    error: query.get("error") ?? hash.get("error"),
  };
}

function clearSteamReturnParams(): void {
  const params = new URLSearchParams(window.location.search);
  params.delete("steam_sync_token");
  params.delete("error");
  const qs = params.toString();
  let next = qs ? `${window.location.pathname}?${qs}` : window.location.pathname;

  const hash = new URLSearchParams(window.location.hash.replace(/^#/, ""));
  hash.delete("steam_sync_token");
  hash.delete("error");
  const hashStr = hash.toString();
  if (hashStr) next += `#${hashStr}`;

  window.history.replaceState({}, "", next);
}

export async function handleSteamSyncReturn(
  games: CatalogGame[],
  mode: LibraryMergeMode,
): Promise<{ stats: SteamSyncStats | null; error: string | null; emptyLibrary: boolean }> {
  const { token, error } = readSteamReturnParams();
  if (error) {
    clearSteamReturnParams();
    const message =
      error === "steam_api_failed"
        ? "Steam library API is unavailable. Try again later."
        : "Steam sign-in failed.";
    return { stats: null, error: message, emptyLibrary: false };
  }
  if (!token) return { stats: null, error: null, emptyLibrary: false };

  clearSteamReturnParams();

  try {
    const result = await exchangeSyncToken(token);
    const stats = applySteamSyncToLibrary(games, result, mode);
    return { stats, error: null, emptyLibrary: result.emptyLibrary };
  } catch {
    return { stats: null, error: "Could not complete Steam library sync.", emptyLibrary: false };
  }
}

export function disconnectSteam(): void {
  clearSteamCredentials();
}

export { loadSteamMeta };
