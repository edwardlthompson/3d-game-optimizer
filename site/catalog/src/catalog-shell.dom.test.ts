// @vitest-environment happy-dom
import { describe, expect, it } from "vitest";
import { libraryMergeMode, renderCatalogShell } from "./catalog-shell";

describe("renderCatalogShell", () => {
  it("omits Steam controls when sync is disabled", () => {
    const root = document.createElement("div");
    renderCatalogShell(root, { steamEnabled: false, connectedHint: "" });
    expect(root.querySelector("#connect-steam")).toBeNull();
    expect(root.querySelector("#disconnect-steam")).toBeNull();
    expect(root.querySelector("#replace-library")).toBeNull();
    expect(root.innerHTML).not.toContain("Steam sync available");
  });

  it("renders Steam controls when sync is enabled", () => {
    const root = document.createElement("div");
    renderCatalogShell(root, { steamEnabled: true, connectedHint: "Last sync yesterday" });
    expect(root.querySelector("#connect-steam")).not.toBeNull();
    expect(root.querySelector("#steam-connected-status")?.textContent).toContain("Last sync yesterday");
    expect(root.innerHTML).toContain("Steam sync available");
  });
});

describe("libraryMergeMode", () => {
  it("defaults to merge and respects replace checkbox", () => {
    const root = document.createElement("div");
    root.innerHTML = `<input id="replace-library" type="checkbox" />`;
    expect(libraryMergeMode(root)).toBe("merge");
    const box = root.querySelector<HTMLInputElement>("#replace-library")!;
    box.checked = true;
    expect(libraryMergeMode(root)).toBe("replace");
  });
});
