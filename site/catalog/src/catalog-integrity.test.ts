// @vitest-environment happy-dom
import { afterEach, describe, expect, it, vi } from "vitest";
import { checkCatalogSync, verifyCatalogIntegrity } from "./catalog-integrity";

describe("verifyCatalogIntegrity", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("passes when hash file is missing", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("", { status: 404 })));
    const result = await verifyCatalogIntegrity("/base/", '{"games":[]}');
    expect(result.ok).toBe(true);
    expect(result.hash).toBeNull();
  });

  it("fails when digest does not match", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response("deadbeef  catalog-v2.json\n", { status: 200 })),
    );
    const result = await verifyCatalogIntegrity("/base/", '{"games":[]}');
    expect(result.ok).toBe(false);
    expect(result.hash).toMatch(/^[a-f0-9]{64}$/);
  });

  it("passes when digest matches catalog text", async () => {
    const catalogText = '{"meta":{"mergedAt":"2026-01-01"},"games":[]}';
    const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(catalogText));
    const expected = [...new Uint8Array(digest)]
      .map((b) => b.toString(16).padStart(2, "0"))
      .join("");

    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response(`${expected}  catalog-v2.json`, { status: 200 })),
    );
    const verified = await verifyCatalogIntegrity("/base/", catalogText);
    expect(verified.ok).toBe(true);
    expect(verified.hash).toBe(expected);
  });
});

describe("checkCatalogSync", () => {
  afterEach(() => {
    localStorage.clear();
  });

  it("invokes callback when mergedAt changes", () => {
    localStorage.setItem("3d-catalog-last-sync", "old");
    const onUpdate = vi.fn();
    checkCatalogSync("new", "abc", onUpdate);
    expect(onUpdate).toHaveBeenCalledOnce();
    expect(localStorage.getItem("3d-catalog-last-sync")).toBe("new");
  });

  it("skips callback on first visit", () => {
    const onUpdate = vi.fn();
    checkCatalogSync("2026-01-01", "hash1", onUpdate);
    expect(onUpdate).not.toHaveBeenCalled();
  });
});
