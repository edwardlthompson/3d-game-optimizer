import { handleOpenIdCallback, startSteamOpenId } from "./openid";
import { rateLimited } from "./rate-limit";
import { exchangeToken, syncOwnedWithUserKey } from "./sync";
import { cors, json } from "./http";
import type { Env } from "./types";

export type { Env } from "./types";

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
    } catch (err) {
      console.error("steam-library worker error", err);
      return cors(json({ error: "Internal error" }, 500), env, origin);
    }
  },
};
