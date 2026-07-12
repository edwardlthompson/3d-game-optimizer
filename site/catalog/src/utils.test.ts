import { describe, expect, it } from "vitest";
import { displayTitle, escapeHtml, stripHtml } from "./utils";

describe("stripHtml / displayTitle", () => {
  it("strips tags and collapses whitespace", () => {
    expect(stripHtml("  <b>Half</b>  Life  ")).toBe("Half Life");
  });

  it("decodes basic named entities once", () => {
    expect(displayTitle("Tom &amp; Jerry")).toBe("Tom & Jerry");
    expect(displayTitle("A &lt; B &gt; C")).toBe("A < B > C");
  });

  it("escapeHtml after displayTitle is safe for HTML insertion", () => {
    const title = displayTitle("Tom &amp; Jerry");
    expect(escapeHtml(title)).toBe("Tom &amp; Jerry");
    expect(escapeHtml(displayTitle("A <b>bold</b> title"))).toBe("A bold title");
  });
});
