import { describe, expect, it } from "vitest";
import { parseSyncPayload } from "./sync-payload";

describe("parseSyncPayload", () => {
  it("parses valid sync payloads", () => {
    const payload = parseSyncPayload(
      JSON.stringify({ appIds: [570, 730], steamId: "76561198000000001", emptyLibrary: false }),
    );
    expect(payload).toEqual({
      appIds: [570, 730],
      steamId: "76561198000000001",
      emptyLibrary: false,
    });
  });

  it("rejects malformed payloads", () => {
    expect(parseSyncPayload("{not-json")).toBeNull();
    expect(parseSyncPayload(JSON.stringify({ steamId: "1" }))).toBeNull();
    expect(parseSyncPayload(JSON.stringify({ appIds: "nope", steamId: "1" }))).toBeNull();
  });

  it("filters non-numeric app ids", () => {
    const payload = parseSyncPayload(
      JSON.stringify({ appIds: [1, "x", 2], steamId: "9", emptyLibrary: true }),
    );
    expect(payload?.appIds).toEqual([1, 2]);
    expect(payload?.emptyLibrary).toBe(true);
  });

  it("rejects oversized app id lists", () => {
    const appIds = Array.from({ length: 10_001 }, (_, i) => i);
    expect(parseSyncPayload(JSON.stringify({ appIds, steamId: "1" }))).toBeNull();
  });
});
