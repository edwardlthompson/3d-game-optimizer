import "./style.css";
import { DATA_COVERAGE_GAPS } from "./data-coverage";
import type { CatalogDocument, CatalogFilters, CatalogGame, SortKey } from "./types";

const LEVEL_RANK: Record<string, number> = {
  ultra3d: 0,
  native3d: 1,
  optimized3d: 2,
  playable3d: 3,
  experimental3d: 4,
  unsupported2d: 5,
};

const LEVEL_LABEL: Record<string, string> = {
  ultra3d: "3D Ultra",
  native3d: "3D",
  optimized3d: "Optimized",
  playable3d: "Playable",
  experimental3d: "Experimental",
  unsupported2d: "Unsupported",
};

const DISPLAY_LABEL: Record<string, string> = {
  "acer-psv27-2": "Acer SpatialLabs",
  "acer-asv15-1": "Acer SpatialLabs 15\"",
  "acer-spatiallabs-15": "Acer Laptop 3D",
  "samsung-g90xf": "Samsung Odyssey 3D",
  "nvidia-3d-vision-generic": "NVIDIA 3D Vision",
  "generic-manual": "Generic stereo",
};

const appRoot = document.querySelector<HTMLDivElement>("#app");
if (!appRoot) {
  throw new Error("Missing #app root");
}

const state = {
  catalog: null as CatalogDocument | null,
  sortKey: "title" as SortKey,
  sortAsc: true,
  filters: {
    query: "",
    ultraOnly: false,
    platforms: new Set<string>(),
    hardware: new Set<string>(),
    visionCertifiedOnly: false,
  } as CatalogFilters,
};

function formatLevel(level: string): string {
  return LEVEL_LABEL[level] ?? level;
}

function visionLabel(game: CatalogGame): string {
  const nvidia = game.sources.find((s) => s.sourceId === "nvidia-3d-vision");
  if (!nvidia) return "—";
  return nvidia.label;
}

function hardwareSummary(game: CatalogGame): string {
  return game.hardwareRequirements.displays
    .map((id) => DISPLAY_LABEL[id] ?? id)
    .join(", ");
}

function compareGames(a: CatalogGame, b: CatalogGame): number {
  const dir = state.sortAsc ? 1 : -1;
  switch (state.sortKey) {
    case "bestLevel":
      return (LEVEL_RANK[a.bestLevel] - LEVEL_RANK[b.bestLevel]) * dir;
    case "releaseDate":
      return (
        (a.steamStats?.releaseDate ?? "").localeCompare(b.steamStats?.releaseDate ?? "") * dir
      );
    case "reviewPercent":
      return ((a.steamStats?.reviewPercent ?? -1) - (b.steamStats?.reviewPercent ?? -1)) * dir;
    case "currentPlayers":
      return ((a.steamStats?.currentPlayers ?? -1) - (b.steamStats?.currentPlayers ?? -1)) * dir;
    case "priceUsd":
      return ((a.steamStats?.priceUsd ?? -1) - (b.steamStats?.priceUsd ?? -1)) * dir;
    default:
      return a.title.localeCompare(b.title) * dir;
  }
}

function matchesFilters(game: CatalogGame): boolean {
  const q = state.filters.query.trim().toLowerCase();
  if (q) {
    const hay = [
      game.title,
      game.platforms.join(" "),
      game.steamTags?.join(" ") ?? "",
      game.steamStats?.tags?.join(" ") ?? "",
    ]
      .join(" ")
      .toLowerCase();
    if (!hay.includes(q)) return false;
  }

  if (state.filters.ultraOnly && game.bestLevel !== "ultra3d" && game.bestLevel !== "native3d") {
    return false;
  }

  if (state.filters.platforms.size > 0) {
    const hit = [...state.filters.platforms].some((p) => game.platforms.includes(p));
    if (!hit) return false;
  }

  if (state.filters.hardware.size > 0) {
    const hit = [...state.filters.hardware].some((h) => game.hardwareRequirements.displays.includes(h));
    if (!hit) return false;
  }

  if (state.filters.visionCertifiedOnly) {
    const nvidia = game.sources.find((s) => s.sourceId === "nvidia-3d-vision");
    if (!nvidia || nvidia.label !== "3D Vision Ready") return false;
  }

  return true;
}

function renderRows(games: CatalogGame[]): string {
  if (games.length === 0) {
    return `<tr><td colspan="11" class="empty">No games match the current filters.</td></tr>`;
  }

  return games
    .map((game) => {
      const exclusive =
        game.hardwareRequirements.exclusiveTo.length > 0
          ? `<span class="badge exclusive">Exclusive hardware</span>`
          : "";
      const legacy = game.sources.some((s) => s.supportStatus === "legacy")
        ? `<span class="badge legacy">Legacy 3D Vision</span>`
        : "";
      const levelClass = game.bestLevel === "ultra3d" ? "badge ultra" : "badge";
      const steamLink = game.steamAppId
        ? `<a href="https://store.steampowered.com/app/${game.steamAppId}/" rel="noopener noreferrer">Steam</a>`
        : "—";
      const review = game.steamStats?.reviewPercent;
      const players = game.steamStats?.currentPlayers;

      return `<tr>
        <td>${escapeHtml(game.title)}</td>
        <td><span class="${levelClass}">${escapeHtml(formatLevel(game.bestLevel))}</span>${legacy}</td>
        <td>${escapeHtml(game.trueGameLabel ?? "—")}</td>
        <td>${escapeHtml(visionLabel(game))}</td>
        <td>${game.platforms.map((p) => `<span class="badge">${escapeHtml(p)}</span>`).join("")}</td>
        <td>${escapeHtml(hardwareSummary(game))}${exclusive}</td>
        <td data-source="steam-store">${review != null ? `${review}%` : "—"}</td>
        <td data-source="steam-store">${players != null ? players.toLocaleString() : "—"}</td>
        <td data-source="steam-store">${game.steamStats?.releaseDate ?? "—"}</td>
        <td data-source="steam-store">${game.steamStats?.priceUsd != null ? `$${game.steamStats.priceUsd.toFixed(2)}` : "—"}</td>
        <td>${steamLink}</td>
      </tr>`;
    })
    .join("");
}

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function render(): void {
  if (!state.catalog) return;

  const filtered = state.catalog.games.filter(matchesFilters).sort(compareGames);
  const tbody = appRoot.querySelector("tbody");
  const status = appRoot.querySelector(".status");
  if (tbody) tbody.innerHTML = renderRows(filtered);
  if (status) {
    status.textContent = `${filtered.length} of ${state.catalog.meta.gameCount} titles · sync ${state.catalog.meta.syncStatus} · merged ${state.catalog.meta.mergedAt}`;
  }
}

function bindSortHeaders(): void {
  appRoot.querySelectorAll<HTMLTableCellElement>("th[data-sort]").forEach((th) => {
    th.addEventListener("click", (event) => {
      const key = th.dataset.sort as SortKey;
      if (state.sortKey === key) {
        state.sortAsc = event.shiftKey ? !state.sortAsc : !state.sortAsc;
      } else {
        state.sortKey = key;
        state.sortAsc = true;
      }
      render();
    });
  });
}

function bindFilters(): void {
  const search = appRoot.querySelector<HTMLInputElement>("#search");
  search?.addEventListener("input", () => {
    state.filters.query = search.value;
    render();
  });

  appRoot.querySelector<HTMLInputElement>("#ultra-only")?.addEventListener("change", (e) => {
    state.filters.ultraOnly = (e.target as HTMLInputElement).checked;
    render();
  });

  appRoot.querySelector<HTMLInputElement>("#vision-certified")?.addEventListener("change", (e) => {
    state.filters.visionCertifiedOnly = (e.target as HTMLInputElement).checked;
    render();
  });

  appRoot.querySelectorAll<HTMLInputElement>("input[data-platform]").forEach((input) => {
    input.addEventListener("change", () => {
      if (input.checked) state.filters.platforms.add(input.dataset.platform!);
      else state.filters.platforms.delete(input.dataset.platform!);
      render();
    });
  });

  appRoot.querySelectorAll<HTMLInputElement>("input[data-hardware]").forEach((input) => {
    input.addEventListener("change", () => {
      if (input.checked) state.filters.hardware.add(input.dataset.hardware!);
      else state.filters.hardware.delete(input.dataset.hardware!);
      render();
    });
  });
}

function shell(): void {
  appRoot.innerHTML = `
    <header>
      <h1>3D Game Catalog</h1>
      <p>Multi-source 3D compatibility — TrueGame, UEVR, NVIDIA 3D Vision, and curated seed data.</p>
    </header>
    <div class="toolbar">
      <input id="search" type="search" placeholder="Search title, platform, tags…" aria-label="Search games" />
      <label><input id="ultra-only" type="checkbox" /> 3D Ultra / native only</label>
      <label><input id="vision-certified" type="checkbox" /> 3D Vision certified</label>
      <label><input data-platform="truegame" type="checkbox" /> TrueGame</label>
      <label><input data-platform="uevr" type="checkbox" /> UEVR</label>
      <label><input data-platform="nvidia-3d-vision" type="checkbox" /> 3D Vision</label>
      <label><input data-hardware="nvidia-3d-vision-generic" type="checkbox" /> NVIDIA hardware</label>
      <label><input data-hardware="samsung-g90xf" type="checkbox" /> Samsung 3D</label>
    </div>
    <div class="status"></div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th data-sort="title">Title</th>
            <th data-sort="bestLevel">3D level</th>
            <th>TrueGame</th>
            <th>3D Vision</th>
            <th>Platforms</th>
            <th>Hardware</th>
            <th data-sort="reviewPercent" data-source="steam-store">Reviews</th>
            <th data-sort="currentPlayers" data-source="steam-store">Players</th>
            <th data-sort="releaseDate" data-source="steam-store">Release</th>
            <th data-sort="priceUsd" data-source="steam-store">Price</th>
            <th>Link</th>
          </tr>
        </thead>
        <tbody></tbody>
      </table>
    </div>
    <footer>
      <div>No cookies or analytics. Data sources: merged catalog JSON from this repository.</div>
      <ul>${DATA_COVERAGE_GAPS.map((g) => `<li>${escapeHtml(g)}</li>`).join("")}</ul>
    </footer>
  `;
  bindSortHeaders();
  bindFilters();
}

async function loadCatalog(): Promise<void> {
  const base = import.meta.env.BASE_URL;
  const response = await fetch(`${base}data/catalog-v2.json`);
  if (!response.ok) throw new Error(`Failed to load catalog: ${response.status}`);
  state.catalog = (await response.json()) as CatalogDocument;
  shell();
  render();

  const params = new URLSearchParams(window.location.search);
  const appId = params.get("appId");
  if (appId) {
    state.filters.query = appId;
    const search = appRoot.querySelector<HTMLInputElement>("#search");
    if (search) search.value = appId;
    render();
  }
}

loadCatalog().catch((error: unknown) => {
  appRoot.innerHTML = `<div class="empty">Could not load catalog: ${escapeHtml(String(error))}</div>`;
});
