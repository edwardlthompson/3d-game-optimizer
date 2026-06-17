import { describe, expect, it } from "vitest";
import {
  NO_DATA,
  gameRankBucket,
  playMethodKeysForGame,
  priceBucket,
  releaseYearBucket,
  reviewBucket,
} from "./buckets";
import type { CatalogGame } from "../types";

describe("priceBucket", () => {
  it("bins prices into five-dollar ranges", () => {
    expect(priceBucket(9.99)).toBe("$5–$9.99");
    expect(priceBucket(null)).toBe(NO_DATA);
  });
});

describe("reviewBucket", () => {
  it("bins review percentages", () => {
    expect(reviewBucket(87)).toBe("80–89%");
    expect(reviewBucket(100)).toBe("100%");
  });
});

describe("releaseYearBucket", () => {
  it("extracts year from release date strings", () => {
    expect(releaseYearBucket("15 Mar, 2020")).toBe("2020");
    expect(releaseYearBucket(undefined)).toBe(NO_DATA);
  });
});

describe("gameRankBucket", () => {
  it("bins scores into tens", () => {
    expect(gameRankBucket(95)).toBe("90–99");
    expect(gameRankBucket(100)).toBe("100");
  });
});

describe("playMethodKeysForGame", () => {
  it("returns platform keys for play methods", () => {
    const g = {
      platformSupport: [{ platformKey: "steam", label: "Steam", level: "experimental3d" }],
      bestLevel: "experimental3d",
      bestExperience: { platformKey: "steam", label: "Steam", level: "experimental3d" },
      sources: [],
    } as CatalogGame;
    expect(playMethodKeysForGame(g).length).toBeGreaterThan(0);
  });
});
