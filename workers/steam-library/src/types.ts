export interface Env {
  SYNC_KV: KVNamespace;
  STEAM_WEB_API_KEY: string;
  ALLOWED_ORIGIN: string;
  CATALOG_RETURN_URL: string;
  /** Set to "true" in local dev only — enables POST /sync/owned with user API keys */
  ENABLE_USER_KEY_SYNC?: string;
}

export interface SyncPayload {
  appIds: number[];
  steamId: string;
  emptyLibrary: boolean;
}

export const TOKEN_TTL = 300;

export const LIMITS: Record<string, number> = {
  auth: 10,
  exchange: 20,
  owned: 5,
};
