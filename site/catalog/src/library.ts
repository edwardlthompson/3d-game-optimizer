const LIBRARY_KEY = "3d-catalog-library-v1";
export const STEAM_META_KEY = "3d-catalog-steam-meta-v1";

export type LibraryMergeMode = "merge" | "replace";

export interface SteamSyncMeta {
  steamId?: string;
  lastSyncAt?: string;
}

export function loadLibrary(): Set<string> {
  try {
    const raw = localStorage.getItem(LIBRARY_KEY);
    if (!raw) return new Set();
    const parsed = JSON.parse(raw) as string[];
    return new Set(Array.isArray(parsed) ? parsed : []);
  } catch {
    return new Set();
  }
}

export function saveLibrary(ids: Set<string>): void {
  localStorage.setItem(LIBRARY_KEY, JSON.stringify([...ids]));
}

export function toggleLibrary(ids: Set<string>, gameId: string): Set<string> {
  const next = new Set(ids);
  if (next.has(gameId)) next.delete(gameId);
  else next.add(gameId);
  saveLibrary(next);
  return next;
}

export function mergeLibraryFromCatalogIds(ids: string[], mode: LibraryMergeMode): Set<string> {
  const next = mode === "replace" ? new Set<string>() : loadLibrary();
  for (const id of ids) next.add(id);
  saveLibrary(next);
  return next;
}

export function exportLibrary(ids: Set<string>): string {
  return JSON.stringify([...ids], null, 2);
}

export function importLibrary(json: string): Set<string> {
  const parsed = JSON.parse(json) as string[];
  const next = new Set(Array.isArray(parsed) ? parsed : []);
  saveLibrary(next);
  return next;
}

export function loadSteamMeta(): SteamSyncMeta {
  try {
    const raw = localStorage.getItem(STEAM_META_KEY);
    if (!raw) return {};
    return JSON.parse(raw) as SteamSyncMeta;
  } catch {
    return {};
  }
}

export function saveSteamMeta(meta: SteamSyncMeta): void {
  localStorage.setItem(STEAM_META_KEY, JSON.stringify(meta));
}

export function clearSteamCredentials(): void {
  localStorage.removeItem(STEAM_META_KEY);
}
