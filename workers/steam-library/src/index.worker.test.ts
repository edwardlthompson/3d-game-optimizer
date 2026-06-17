import { env, exports } from "cloudflare:workers";
import { describe, expect, it } from "vitest";

const ORIGIN = "https://edwardlthompson.github.io";

function workerFetch(
  path: string,
  init: RequestInit & { ip?: string } = {},
): Promise<Response> {
  const { ip = "203.0.113.10", ...requestInit } = init;
  const headers = new Headers(requestInit.headers);
  headers.set("cf-connecting-ip", ip);
  return exports.default.fetch(
    new Request(`http://worker.test${path}`, {
      ...requestInit,
      headers,
    }),
  );
}

function exchangeRequest(token: string, ip: string, origin = ORIGIN): Promise<Response> {
  return workerFetch("/sync/exchange", {
    method: "POST",
    ip,
    headers: {
      Origin: origin,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ token }),
  });
}

describe("POST /sync/exchange", () => {
  it("returns owned app IDs and consumes the KV token", async () => {
    const token = crypto.randomUUID();
    await env.SYNC_KV.put(
      `sync:${token}`,
      JSON.stringify({
        appIds: [570, 730],
        steamId: "76561198000000000",
        emptyLibrary: false,
      }),
      { expirationTtl: 300 },
    );

    const first = await exchangeRequest(token, "203.0.113.20");
    expect(first.status).toBe(200);
    const body = (await first.json()) as {
      appIds: number[];
      steamIdTruncated: string;
      emptyLibrary: boolean;
      source: string;
    };
    expect(body.appIds).toEqual([570, 730]);
    expect(body.emptyLibrary).toBe(false);
    expect(body.source).toBe("openid");
    expect(body.steamIdTruncated).toContain("…");

    const second = await exchangeRequest(token, "203.0.113.21");
    expect(second.status).toBe(410);
    expect(await env.SYNC_KV.get(`sync:${token}`)).toBeNull();
  });

  it("rejects missing token", async () => {
    const resp = await exchangeRequest("", "203.0.113.22");
    expect(resp.status).toBe(400);
  });

  it("rejects unknown token", async () => {
    const resp = await exchangeRequest(crypto.randomUUID(), "203.0.113.23");
    expect(resp.status).toBe(410);
  });

  it("rejects forbidden origin", async () => {
    const token = crypto.randomUUID();
    await env.SYNC_KV.put(
      `sync:${token}`,
      JSON.stringify({ appIds: [], steamId: "1", emptyLibrary: true }),
      { expirationTtl: 300 },
    );

    const resp = await exchangeRequest(token, "203.0.113.24", "https://evil.example");
    expect(resp.status).toBe(403);
    expect(resp.headers.get("Access-Control-Allow-Origin")).toBe(ORIGIN);
  });

  it("does not consume token when KV payload is invalid", async () => {
    const token = crypto.randomUUID();
    await env.SYNC_KV.put(`sync:${token}`, "{not-json", { expirationTtl: 300 });

    const resp = await exchangeRequest(token, "203.0.113.25");
    expect(resp.status).toBe(410);
    expect(await env.SYNC_KV.get(`sync:${token}`)).not.toBeNull();
  });

  it("returns 400 for malformed JSON body", async () => {
    const resp = await workerFetch("/sync/exchange", {
      method: "POST",
      ip: "203.0.113.26",
      headers: { Origin: ORIGIN, "Content-Type": "application/json" },
      body: "{",
    });
    expect(resp.status).toBe(400);
  });
});

describe("rate limits", () => {
  it("increments per-route KV counters for exchange", async () => {
    const ip = "203.0.113.55";
    const hour = Math.floor(Date.now() / 3_600_000);
    const key = `rate:exchange:${ip}:${hour}`;

    await exchangeRequest(crypto.randomUUID(), ip);
    const afterOne = await env.SYNC_KV.get(key);
    expect(afterOne).not.toBeNull();
    expect(Number.parseInt(afterOne!, 10)).toBeGreaterThanOrEqual(1);
  });

  it("returns 429 after the exchange route limit", async () => {
    const ip = "203.0.113.60";
    for (let i = 0; i < 20; i += 1) {
      const token = crypto.randomUUID();
      await env.SYNC_KV.put(
        `sync:${token}`,
        JSON.stringify({ appIds: [1], steamId: "1", emptyLibrary: false }),
        { expirationTtl: 300 },
      );
      const resp = await exchangeRequest(token, ip);
      expect(resp.status).toBe(200);
    }

    const token = crypto.randomUUID();
    await env.SYNC_KV.put(
      `sync:${token}`,
      JSON.stringify({ appIds: [2], steamId: "2", emptyLibrary: false }),
      { expirationTtl: 300 },
    );
    const limited = await exchangeRequest(token, ip);
    expect(limited.status).toBe(429);
    expect(limited.headers.get("Access-Control-Allow-Origin")).toBe(ORIGIN);
  });
});

describe("KV lifecycle", () => {
  it("stores sync tokens under sync: prefix with TTL metadata", async () => {
    const token = crypto.randomUUID();
    await env.SYNC_KV.put(
      `sync:${token}`,
      JSON.stringify({ appIds: [42], steamId: "99", emptyLibrary: false }),
      { expirationTtl: 300 },
    );

    const raw = await env.SYNC_KV.get(`sync:${token}`);
    expect(raw).not.toBeNull();
    expect(JSON.parse(raw!).appIds).toEqual([42]);

    await exchangeRequest(token, "203.0.113.70");
    expect(await env.SYNC_KV.get(`sync:${token}`)).toBeNull();
  });
});
