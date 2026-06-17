import { describe, expect, it, vi, afterEach } from "vitest";
import { fetchOwnedGames } from "./steam-api";

describe("fetchOwnedGames", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns missing_key when api key is empty", async () => {
    const result = await fetchOwnedGames("", "76561198000000001");
    expect(result).toEqual({ ok: false, reason: "missing_key" });
  });

  it("returns app ids on success", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ response: { games: [{ appid: 570 }, { appid: 730 }] } }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      ),
    );

    const result = await fetchOwnedGames("key", "76561198000000001");
    expect(result).toEqual({ ok: true, appIds: [570, 730] });
  });

  it("returns http_error on non-OK response", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("Unauthorized", { status: 401 })));

    const result = await fetchOwnedGames("key", "76561198000000001");
    expect(result).toEqual({ ok: false, reason: "http_error" });
  });

  it("returns invalid_response on malformed JSON", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response("not-json", { status: 200, headers: { "Content-Type": "application/json" } })),
    );

    const result = await fetchOwnedGames("key", "76561198000000001");
    expect(result).toEqual({ ok: false, reason: "invalid_response" });
  });

  it("returns empty appIds when library is genuinely empty", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ response: { games: [] } }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      ),
    );

    const result = await fetchOwnedGames("key", "76561198000000001");
    expect(result).toEqual({ ok: true, appIds: [] });
  });
});
