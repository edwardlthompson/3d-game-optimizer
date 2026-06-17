// @vitest-environment happy-dom
import { describe, expect, it } from "vitest";
import { showSteamBanner, type SteamUiContext } from "./steam-ui";

function ctx(html: string): SteamUiContext {
  const appRoot = document.createElement("div");
  appRoot.innerHTML = html;
  document.body.append(appRoot);
  return {
    appRoot,
    getGames: () => [],
    getMergeMode: () => "merge",
    refreshGrid: () => {},
  };
}

describe("showSteamBanner", () => {
  it("shows error state", () => {
    const ui = ctx('<div id="steam-sync-banner" hidden></div>');
    showSteamBanner(ui, null, "Steam sign-in failed.", false);
    const banner = ui.appRoot.querySelector("#steam-sync-banner")!;
    expect(banner.hidden).toBe(false);
    expect(banner.className).toContain("error");
    expect(banner.textContent).toContain("Steam sign-in failed.");
  });

  it("shows empty-library help after sync with zero owned", () => {
    const ui = ctx('<div id="steam-sync-banner" hidden></div>');
    showSteamBanner(
      ui,
      { catalogMatched: 0, ownedTotal: 0, ownedUnmatched: 0, catalogNoSteamLink: 0 },
      null,
      true,
    );
    const banner = ui.appRoot.querySelector("#steam-sync-banner")!;
    expect(banner.className).toContain("warn");
    expect(banner.innerHTML).toContain("Steam returned no owned games");
  });

  it("shows empty-library help", () => {
    const ui = ctx('<div id="steam-sync-banner" hidden></div>');
    showSteamBanner(ui, null, null, true);
    const banner = ui.appRoot.querySelector("#steam-sync-banner")!;
    expect(banner.className).toContain("warn");
    expect(banner.innerHTML).toContain("Steam returned no owned games");
  });

  it("shows success stats", () => {
    const ui = ctx('<div id="steam-sync-banner" hidden></div>');
    showSteamBanner(
      ui,
      {
        catalogMatched: 3,
        ownedTotal: 10,
        ownedUnmatched: 2,
        catalogNoSteamLink: 5,
      },
      null,
      false,
    );
    const banner = ui.appRoot.querySelector("#steam-sync-banner")!;
    expect(banner.className).toContain("success");
    expect(banner.innerHTML).toContain("3 catalog titles matched");
    expect(banner.innerHTML).toContain("10 owned on Steam");
  });

  it("hides banner when idle", () => {
    const ui = ctx('<div id="steam-sync-banner">old</div>');
    showSteamBanner(ui, null, null, false);
    const banner = ui.appRoot.querySelector<HTMLDivElement>("#steam-sync-banner")!;
    expect(banner.hidden).toBe(true);
    expect(banner.textContent).toBe("");
  });
});
