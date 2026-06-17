import type { SyncPayload } from "./types";

export const MAX_SYNC_APP_IDS = 10_000;

export function parseSyncPayload(raw: string): SyncPayload | null {
  try {
    const data = JSON.parse(raw) as Partial<SyncPayload>;
    if (!Array.isArray(data.appIds) || typeof data.steamId !== "string") return null;
    if (data.appIds.length > MAX_SYNC_APP_IDS) return null;
    const appIds = data.appIds.filter((id): id is number => typeof id === "number" && Number.isFinite(id));
    return {
      appIds,
      steamId: data.steamId,
      emptyLibrary: typeof data.emptyLibrary === "boolean" ? data.emptyLibrary : appIds.length === 0,
    };
  } catch {
    return null;
  }
}
