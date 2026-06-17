import { corsOrigin, type AllowedOriginEnv } from "./security";
import type { Env } from "./types";
export function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

export function cors(response: Response, env: Env, origin: string | null): Response {
  const headers = new Headers(response.headers);
  headers.set("Access-Control-Allow-Origin", corsOrigin(origin, env));
  headers.set("Access-Control-Allow-Methods", "POST, OPTIONS");
  headers.set("Access-Control-Allow-Headers", "Content-Type");
  headers.set("Vary", "Origin");
  return new Response(response.body, { status: response.status, headers });
}

export function truncateSteamId(steamId: string): string {
  if (steamId.length <= 6) return steamId;
  return `${steamId.slice(0, 4)}…${steamId.slice(-2)}`;
}

export function redirectCatalog(
  env: AllowedOriginEnv,  query: Record<string, string> = {},
  hash: Record<string, string> = {},
): Response {
  const url = new URL(env.CATALOG_RETURN_URL);
  for (const [key, value] of Object.entries(query)) url.searchParams.set(key, value);
  const hashParams = new URLSearchParams(hash);
  const hashStr = hashParams.toString();
  if (hashStr) url.hash = hashStr;
  return Response.redirect(url.toString(), 302);
}
