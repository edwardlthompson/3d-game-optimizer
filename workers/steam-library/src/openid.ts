import { fetchOwnedGames } from "./steam-api";
import { redirectCatalog } from "./http";
import { MAX_SYNC_APP_IDS } from "./sync-payload";
import type { Env } from "./types";
import { TOKEN_TTL } from "./types";

export function startSteamOpenId(workerOrigin: string): Response {
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

export async function handleOpenIdCallback(
  request: Request,
  env: Env,
  workerOrigin: string,
): Promise<Response> {
  const params = new URL(request.url).searchParams;
  const steamId = await verifyOpenId(params);
  if (!steamId) {
    return redirectCatalog(env, { error: "openid_failed" });
  }

  const owned = await fetchOwnedGames(env.STEAM_WEB_API_KEY, steamId);
  if (!owned.ok) {
    return redirectCatalog(env, { error: "steam_api_failed" });
  }

  const appIds = owned.appIds.slice(0, MAX_SYNC_APP_IDS);
  const token = crypto.randomUUID();
  const payload = {
    appIds,
    steamId,
    emptyLibrary: appIds.length === 0,
  };
  await env.SYNC_KV.put(`sync:${token}`, JSON.stringify(payload), { expirationTtl: TOKEN_TTL });
  return redirectCatalog(env, {}, { steam_sync_token: token });
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
