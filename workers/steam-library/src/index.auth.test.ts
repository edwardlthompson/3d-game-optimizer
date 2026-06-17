import { afterEach, describe, expect, it, vi } from "vitest";
import worker from "./index";

function createMockKv(): KVNamespace {
  const store = new Map<string, string>();
  return {
    get: vi.fn(async (key: string) => store.get(key) ?? null),
    put: vi.fn(async (key: string, value: string) => {
      store.set(key, value);
    }),
    delete: vi.fn(async (key: string) => {
      store.delete(key);
    }),
    list: vi.fn(),
    getWithMetadata: vi.fn(),
  } as unknown as KVNamespace;
}

const baseEnv = {
  ALLOWED_ORIGIN: "https://edwardlthompson.github.io",
  CATALOG_RETURN_URL: "https://edwardlthompson.github.io/3d-game-optimizer/catalog/",
  STEAM_WEB_API_KEY: "vitest-steam-key",
};

function workerFetch(
  path: string,
  init: RequestInit & { ip?: string } = {},
  env: typeof baseEnv & { SYNC_KV: KVNamespace },
): Promise<Response> {
  const { ip = "203.0.113.10", ...requestInit } = init;
  const headers = new Headers(requestInit.headers);
  headers.set("cf-connecting-ip", ip);
  return worker.fetch(
    new Request(`http://worker.test${path}`, {
      ...requestInit,
      headers,
    }),
    env,
  );
}

describe("GET /auth/steam", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("redirects to Steam OpenID login with callback URL", async () => {
    const env = { ...baseEnv, SYNC_KV: createMockKv() };
    const resp = await workerFetch("/auth/steam", { method: "GET", ip: "203.0.113.80" }, env);
    expect(resp.status).toBe(302);
    const location = resp.headers.get("Location");
    expect(location).toMatch(/^https:\/\/steamcommunity\.com\/openid\/login\?/);
    const url = new URL(location!);
    expect(url.searchParams.get("openid.mode")).toBe("checkid_setup");
    expect(url.searchParams.get("openid.return_to")).toBe("http://worker.test/auth/steam/callback");
    expect(url.searchParams.get("openid.realm")).toBe("http://worker.test");
  });
});

describe("GET /auth/steam/callback", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("stores owned games and redirects to catalog with sync token", async () => {
    const kv = createMockKv();
    const env = { ...baseEnv, SYNC_KV: kv };

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        if (url.includes("steamcommunity.com/openid/login")) {
          return new Response("is_valid:true\n", { status: 200 });
        }
        if (url.includes("GetOwnedGames")) {
          return new Response(JSON.stringify({ response: { games: [{ appid: 570 }, { appid: 730 }] } }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }
        throw new Error(`unexpected fetch: ${url}`);
      }),
    );

    const steamId = "76561198000000001";
    const query = new URLSearchParams({
      "openid.claimed_id": `https://steamcommunity.com/openid/id/${steamId}`,
      "openid.mode": "id_res",
    });
    const resp = await workerFetch(`/auth/steam/callback?${query}`, { method: "GET", ip: "203.0.113.81" }, env);
    expect(resp.status).toBe(302);

    const catalog = new URL(resp.headers.get("Location")!);
    const token = new URLSearchParams(catalog.hash.replace(/^#/, "")).get("steam_sync_token");
    expect(token).toBeTruthy();

    const raw = await kv.get(`sync:${token}`);
    expect(raw).not.toBeNull();
    const payload = JSON.parse(raw!) as { appIds: number[]; steamId: string; emptyLibrary: boolean };
    expect(payload.appIds).toEqual([570, 730]);
    expect(payload.steamId).toBe(steamId);
    expect(payload.emptyLibrary).toBe(false);
  });

  it("redirects to catalog with openid_failed when verification fails", async () => {
    const env = { ...baseEnv, SYNC_KV: createMockKv() };
    vi.stubGlobal("fetch", vi.fn(async () => new Response("is_valid:false\n", { status: 200 })));

    const query = new URLSearchParams({
      "openid.claimed_id": "https://steamcommunity.com/openid/id/76561198000000002",
      "openid.mode": "id_res",
    });
    const resp = await workerFetch(`/auth/steam/callback?${query}`, { method: "GET", ip: "203.0.113.82" }, env);
    expect(resp.status).toBe(302);
    const catalog = new URL(resp.headers.get("Location")!);
    expect(catalog.searchParams.get("error")).toBe("openid_failed");
    expect(catalog.searchParams.get("steam_sync_token")).toBeNull();
  });

  it("redirects to catalog with steam_api_failed when owned-games API fails", async () => {
    const env = { ...baseEnv, SYNC_KV: createMockKv() };
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        if (url.includes("steamcommunity.com/openid/login")) {
          return new Response("is_valid:true\n", { status: 200 });
        }
        if (url.includes("GetOwnedGames")) {
          return new Response("Unauthorized", { status: 401 });
        }
        throw new Error(`unexpected fetch: ${url}`);
      }),
    );

    const query = new URLSearchParams({
      "openid.claimed_id": "https://steamcommunity.com/openid/id/76561198000000003",
      "openid.mode": "id_res",
    });
    const resp = await workerFetch(`/auth/steam/callback?${query}`, { method: "GET", ip: "203.0.113.83" }, env);
    expect(resp.status).toBe(302);
    const catalog = new URL(resp.headers.get("Location")!);
    expect(catalog.searchParams.get("error")).toBe("steam_api_failed");
    expect(catalog.searchParams.get("steam_sync_token")).toBeNull();
  });

  it("caps owned app ids at MAX_SYNC_APP_IDS in KV payload", async () => {
    const kv = createMockKv();
    const env = { ...baseEnv, SYNC_KV: kv };
    const games = Array.from({ length: 10_001 }, (_, i) => ({ appid: i + 1 }));

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        if (url.includes("steamcommunity.com/openid/login")) {
          return new Response("is_valid:true\n", { status: 200 });
        }
        if (url.includes("GetOwnedGames")) {
          return new Response(JSON.stringify({ response: { games } }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }
        throw new Error(`unexpected fetch: ${url}`);
      }),
    );

    const query = new URLSearchParams({
      "openid.claimed_id": "https://steamcommunity.com/openid/id/76561198000000004",
      "openid.mode": "id_res",
    });
    const resp = await workerFetch(`/auth/steam/callback?${query}`, { method: "GET", ip: "203.0.113.84" }, env);
    expect(resp.status).toBe(302);

    const catalog = new URL(resp.headers.get("Location")!);
    const token = new URLSearchParams(catalog.hash.replace(/^#/, "")).get("steam_sync_token");
    expect(token).toBeTruthy();

    const raw = await kv.get(`sync:${token}`);
    const payload = JSON.parse(raw!) as { appIds: number[] };
    expect(payload.appIds).toHaveLength(10_000);
    expect(payload.appIds[0]).toBe(1);
    expect(payload.appIds.at(-1)).toBe(10_000);
  });
});

describe("POST /sync/owned", () => {
  const ORIGIN = "https://edwardlthompson.github.io";

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns 404 when dev sync is disabled", async () => {
    const env = { ...baseEnv, SYNC_KV: createMockKv() };
    const resp = await workerFetch(
      "/sync/owned",
      {
        method: "POST",
        ip: "203.0.113.90",
        headers: { Origin: ORIGIN, "Content-Type": "application/json" },
        body: JSON.stringify({ steamId: "1", apiKey: "key" }),
      },
      env,
    );
    expect(resp.status).toBe(404);
  });

  it("returns owned games when dev sync is enabled", async () => {
    const env = { ...baseEnv, SYNC_KV: createMockKv(), ENABLE_USER_KEY_SYNC: "true" };
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ response: { games: [{ appid: 42 }] } }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      ),
    );

    const resp = await workerFetch(
      "/sync/owned",
      {
        method: "POST",
        ip: "203.0.113.91",
        headers: { Origin: ORIGIN, "Content-Type": "application/json" },
        body: JSON.stringify({ steamId: "76561198000000009", apiKey: "user-key" }),
      },
      env,
    );
    expect(resp.status).toBe(200);
    const body = (await resp.json()) as { appIds: number[]; source: string };
    expect(body.appIds).toEqual([42]);
    expect(body.source).toBe("user_key");
  });

  it("returns 502 when Steam API fails", async () => {
    const env = { ...baseEnv, SYNC_KV: createMockKv(), ENABLE_USER_KEY_SYNC: "true" };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("error", { status: 500 })));

    const resp = await workerFetch(
      "/sync/owned",
      {
        method: "POST",
        ip: "203.0.113.92",
        headers: { Origin: ORIGIN, "Content-Type": "application/json" },
        body: JSON.stringify({ steamId: "76561198000000009", apiKey: "user-key" }),
      },
      env,
    );
    expect(resp.status).toBe(502);
    expect(await resp.json()).toMatchObject({ error: "Steam API unavailable" });
  });
});

describe("auth rate limits", () => {
  it("returns 429 after the auth route limit", async () => {
    const kv = createMockKv();
    const env = { ...baseEnv, SYNC_KV: kv };
    for (let i = 0; i < 10; i += 1) {
      const resp = await workerFetch("/auth/steam", { method: "GET", ip: "203.0.113.95" }, env);
      expect(resp.status).toBe(302);
    }
    const limited = await workerFetch("/auth/steam", { method: "GET", ip: "203.0.113.95" }, env);
    expect(limited.status).toBe(429);
    expect(limited.headers.get("Access-Control-Allow-Origin")).toBe(baseEnv.ALLOWED_ORIGIN);
  });
});

describe("exchange invalid payload", () => {
  const ORIGIN = "https://edwardlthompson.github.io";

  it("returns 410 for poisoned KV payloads", async () => {
    const kv = createMockKv();
    const env = { ...baseEnv, SYNC_KV: kv };
    const token = "bad-payload";
    await kv.put(`sync:${token}`, JSON.stringify({ appIds: "not-array", steamId: 1 }));

    const resp = await workerFetch(
      "/sync/exchange",
      {
        method: "POST",
        ip: "203.0.113.96",
        headers: { Origin: ORIGIN, "Content-Type": "application/json" },
        body: JSON.stringify({ token }),
      },
      env,
    );
    expect(resp.status).toBe(410);
    expect(await resp.json()).toMatchObject({ error: "Invalid sync payload" });
  });
});
