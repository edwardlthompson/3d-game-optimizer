import { isAllowedOrigin } from "./security";

export interface Env {
  SYNC_KV: KVNamespace;
  STEAM_WEB_API_KEY: string;
  ALLOWED_ORIGIN: string;
  CATALOG_RETURN_URL: string;
  /** Set to "true" in local dev only — enables POST /sync/owned with user API keys */
  ENABLE_USER_KEY_SYNC?: string;
}

interface SyncPayload {
  appIds: number[];
  steamId: string;
  emptyLibrary: boolean;
}

const TOKEN_TTL = 300;
const LIMITS: Record<string, number> = {
  auth: 10,
  exchange: 20,
  owned: 5,
};

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const ip = request.headers.get("cf-connecting-ip") ?? "unknown";
    const origin = request.headers.get("Origin");

    if (request.method === "OPTIONS") {
      return cors(new Response(null, { status: 204 }), env, origin);
    }

    try {
      if (url.pathname === "/auth/steam" && request.method === "GET") {
        const limited = await rateLimited(env, ip, "auth", origin);
        if (limited) return limited;
        return startSteamOpenId(url.origin);
      }

      if (url.pathname === "/auth/steam/callback" && request.method === "GET") {
        const limited = await rateLimited(env, ip, "auth", origin);
        if (limited) return limited;
        return handleOpenIdCallback(request, env, url.origin);
      }

      if (url.pathname === "/sync/exchange" && request.method === "POST") {
        const limited = await rateLimited(env, ip, "exchange", origin);
        if (limited) return limited;
        return exchangeToken(request, env, origin);
      }

      if (url.pathname === "/sync/owned" && request.method === "POST") {
        if (env.ENABLE_USER_KEY_SYNC !== "true") {
          return cors(json({ error: "Disabled" }, 404), env, origin);
        }
        const limited = await rateLimited(env, ip, "owned", origin);
        if (limited) return limited;
        return syncOwnedWithUserKey(request, env, origin);
      }

      if (url.pathname === "/health" && request.method === "GET") {
        return json({ ok: true });
      }

      return new Response("Not found", { status: 404 });
    } catch {
      return json({ error: "Internal error" }, 500);
    }
  },
};

function startSteamOpenId(workerOrigin: string): Response {
  const params = new URLSearchParams({
    "openid.ns": "http://specs.openid.net/auth/2.0",
    "openid.mode": "checkid_setup",
    "openid.return_to": `${workerOrigin}/auth/steam/callback`,
    "openid.realm": workerOrigin,
    "openid.identity": "http://specs.openid.net/auth/2.0/identifier_select",
    "openid.claimed_id": "http://specs.openid.net/auth/2.0/identifier_select",
  });
  return Response.redirect(`https://steamcommunity.com/openid/login?${params}`, 302);
}

async function handleOpenIdCallback(
  request: Request,
  env: Env,
  workerOrigin: string,
): Promise<Response> {
  const params = new URL(request.url).searchParams;
  const steamId = await verifyOpenId(params);
  if (!steamId) {
    return redirectCatalog(env, { error: "openid_failed" });
  }

  const appIds = await fetchOwnedGames(env.STEAM_WEB_API_KEY, steamId);
  const token = crypto.randomUUID();
  const payload: SyncPayload = {
    appIds,
    steamId,
    emptyLibrary: appIds.length === 0,
  };
  await env.SYNC_KV.put(`sync:${token}`, JSON.stringify(payload), { expirationTtl: TOKEN_TTL });
  return redirectCatalog(env, { steam_sync_token: token });
}

async function verifyOpenId(params: URLSearchParams): Promise<string | null> {
  const verify = new URLSearchParams();
  for (const [key, value] of params.entries()) {
    if (key.startsWith("openid.")) verify.set(key, value);
  }
  verify.set("openid.mode", "check_authentication");

  const resp = await fetch("https://steamcommunity.com/openid/login", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: verify.toString(),
  });
  const text = await resp.text();
  if (!text.includes("is_valid:true")) return null;

  const claimed = params.get("openid.claimed_id") ?? "";
  const match = claimed.match(/\/id\/(\d+)$/);
  return match?.[1] ?? null;
}

async function fetchOwnedGames(apiKey: string, steamId: string): Promise<number[]> {
  if (!apiKey) return [];
  const url = new URL("https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/");
  url.searchParams.set("key", apiKey);
  url.searchParams.set("steamid", steamId);
  url.searchParams.set("include_appinfo", "0");
  url.searchParams.set("include_played_free_games", "1");

  const resp = await fetch(url.toString());
  if (!resp.ok) return [];
  const data = (await resp.json()) as {
    response?: { games?: Array<{ appid: number }> };
  };
  return (data.response?.games ?? []).map((g) => g.appid);
}

async function exchangeToken(
  request: Request,
  env: Env,
  origin: string | null,
): Promise<Response> {
  if (!isAllowedOrigin(origin, env)) {
    return cors(json({ error: "Forbidden origin" }, 403), env, origin);
  }

  const body = (await request.json()) as { token?: string };
  const token = body.token?.trim();
  if (!token) return cors(json({ error: "Missing token" }, 400), env, origin);

  const key = `sync:${token}`;
  const raw = await env.SYNC_KV.get(key);
  if (!raw) return cors(json({ error: "Token expired or invalid" }, 410), env, origin);

  await env.SYNC_KV.delete(key);
  const payload = JSON.parse(raw) as SyncPayload;

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

async function syncOwnedWithUserKey(
  request: Request,
  env: Env,
  origin: string | null,
): Promise<Response> {
  if (!isAllowedOrigin(origin, env)) {
    return cors(json({ error: "Forbidden origin" }, 403), env, origin);
  }

  const body = (await request.json()) as { steamId?: string; apiKey?: string };
  const steamId = body.steamId?.trim();
  const apiKey = body.apiKey?.trim();
  if (!steamId || !apiKey || steamId.length > 32 || apiKey.length > 128) {
    return cors(json({ error: "Invalid request" }, 400), env, origin);
  }

  const appIds = await fetchOwnedGames(apiKey, steamId);
  return cors(
    json({
      appIds,
      steamIdTruncated: truncateSteamId(steamId),
      emptyLibrary: appIds.length === 0,
      source: "user_key",
    }),
    env,
    origin,
  );
}

async function rateLimited(
  env: Env,
  ip: string,
  route: keyof typeof LIMITS,
  origin: string | null,
): Promise<Response | null> {
  const hour = Math.floor(Date.now() / 3_600_000);
  const key = `rate:${route}:${ip}:${hour}`;
  const raw = await env.SYNC_KV.get(key);
  const count = raw ? Number.parseInt(raw, 10) : 0;
  if (count >= LIMITS[route]) {
    return cors(json({ error: "Rate limit exceeded" }, 429), env, origin);
  }
  await env.SYNC_KV.put(key, String(count + 1), { expirationTtl: 3600 });
  return null;
}

function redirectCatalog(env: Env, params: Record<string, string>): Response {
  const url = new URL(env.CATALOG_RETURN_URL);
  for (const [key, value] of Object.entries(params)) url.searchParams.set(key, value);
  return Response.redirect(url.toString(), 302);
}

function truncateSteamId(steamId: string): string {
  if (steamId.length <= 6) return steamId;
  return `${steamId.slice(0, 4)}…${steamId.slice(-2)}`;
}

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function cors(response: Response, env: Env, origin: string | null): Response {
  const headers = new Headers(response.headers);
  headers.set("Access-Control-Allow-Origin", origin ?? env.ALLOWED_ORIGIN);
  headers.set("Access-Control-Allow-Methods", "POST, OPTIONS");
  headers.set("Access-Control-Allow-Headers", "Content-Type");
  headers.set("Vary", "Origin");
  return new Response(response.body, { status: response.status, headers });
}
