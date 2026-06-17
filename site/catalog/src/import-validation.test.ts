import { describe, expect, it } from "vitest";
import { parseStringIdList } from "./import-validation";

describe("parseStringIdList", () => {
  it("accepts valid string arrays", () => {
    expect(parseStringIdList('["a","b"]')).toEqual(["a", "b"]);
  });

  it("rejects oversized payloads", () => {
    expect(() => parseStringIdList("[]".padEnd(600_000, " "))).toThrow(/exceeds/);
  });

  it("rejects non-array json", () => {
    expect(() => parseStringIdList('{"a":1}')).toThrow(/array/);
  });
});
