import { cors, json } from "./http";
import type { Env } from "./types";
import { LIMITS } from "./types";

export async function rateLimited(
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
