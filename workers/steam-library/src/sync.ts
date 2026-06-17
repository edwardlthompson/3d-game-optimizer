import { isAllowedOrigin } from "./security";
import { fetchOwnedGames } from "./steam-api";
import { parseSyncPayload } from "./sync-payload";
import { cors, json, truncateSteamId } from "./http";
import type { Env } from "./types";

export async function exchangeToken(
  request: Request,
  env: Env,
  origin: string | null,
): Promise<Response> {
  if (!isAllowedOrigin(origin, env)) {
    return cors(json({ error: "Forbidden origin" }, 403), env, origin);
  }

  let body: { token?: string };
  try {
    body = (await request.json()) as { token?: string };
  } catch {
    return cors(json({ error: "Invalid JSON" }, 400), env, origin);
  }

  const token = body.token?.trim();
  if (!token) return cors(json({ error: "Missing token" }, 400), env, origin);

  const key = `sync:${token}`;
  const raw = await env.SYNC_KV.get(key);
  if (!raw) return cors(json({ error: "Token expired or invalid" }, 410), env, origin);

  const payload = parseSyncPayload(raw);
  if (!payload) return cors(json({ error: "Invalid sync payload" }, 410), env, origin);

  await env.SYNC_KV.delete(key);
  return cors(
    json({
      appIds: payload.appIds,
      steamIdTruncated: truncateSteamId(payload.steamId),
      emptyLibrary: payload.emptyLibrary,
      source: "openid",
    }),
    env,
    origin,
  );
}

export async function syncOwnedWithUserKey(
  request: Request,
  env: Env,
  origin: string | null,
): Promise<Response> {
  if (!isAllowedOrigin(origin, env)) {
    return cors(json({ error: "Forbidden origin" }, 403), env, origin);
  }

  let body: { steamId?: string; apiKey?: string };
  try {
    body = (await request.json()) as { steamId?: string; apiKey?: string };
  } catch {
    return cors(json({ error: "Invalid JSON" }, 400), env, origin);
  }

  const steamId = body.steamId?.trim();
  const apiKey = body.apiKey?.trim();
  if (!steamId || !apiKey || steamId.length > 32 || apiKey.length > 128) {
    return cors(json({ error: "Invalid request" }, 400), env, origin);
  }

  const owned = await fetchOwnedGames(apiKey, steamId);
  if (!owned.ok) {
    const status = owned.reason === "missing_key" ? 503 : 502;
    return cors(json({ error: "Steam API unavailable" }, status), env, origin);
  }

  return cors(
    json({
      appIds: owned.appIds,
      steamIdTruncated: truncateSteamId(steamId),
      emptyLibrary: owned.appIds.length === 0,
      source: "user_key",
    }),
    env,
    origin,
  );
}
