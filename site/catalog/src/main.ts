import "./style.css";
import { DATA_COVERAGE_GAPS } from "./data-coverage";
import { CatalogGrid, type QuickFilters } from "./grid";
import type { CatalogDocument } from "./types";
import { escapeHtml } from "./utils";

const maybeRoot = document.querySelector<HTMLDivElement>("#app");
if (!maybeRoot) {
  throw new Error("Missing #app root");
}
const appRoot: HTMLDivElement = maybeRoot;

let catalog: CatalogDocument | null = null;
let grid: CatalogGrid | null = null;

const quickFilters: QuickFilters = {
  ultraOnly: false,
  visionCertifiedOnly: false,
  platforms: new Set<string>(),
  hardware: new Set<string>(),
};

function syncQuickFilters(): void {
  grid?.setQuickFilters({ ...quickFilters, platforms: new Set(quickFilters.platforms), hardware: new Set(quickFilters.hardware) });
}

function bindFilters(): void {
  appRoot.querySelector<HTMLInputElement>("#search")?.addEventListener("input", (e) => {
    grid?.setGlobalFilter((e.target as HTMLInputElement).value);
  });

  appRoot.querySelector<HTMLInputElement>("#ultra-only")?.addEventListener("change", (e) => {
    quickFilters.ultraOnly = (e.target as HTMLInputElement).checked;
    syncQuickFilters();
  });

  appRoot.querySelector<HTMLInputElement>("#vision-certified")?.addEventListener("change", (e) => {
    quickFilters.visionCertifiedOnly = (e.target as HTMLInputElement).checked;
    syncQuickFilters();
  });

  appRoot.querySelectorAll<HTMLInputElement>("input[data-platform]").forEach((input) => {
    input.addEventListener("change", () => {
      if (input.checked) quickFilters.platforms.add(input.dataset.platform!);
      else quickFilters.platforms.delete(input.dataset.platform!);
      syncQuickFilters();
    });
  });

  appRoot.querySelectorAll<HTMLInputElement>("input[data-hardware]").forEach((input) => {
    input.addEventListener("change", () => {
      if (input.checked) quickFilters.hardware.add(input.dataset.hardware!);
      else quickFilters.hardware.delete(input.dataset.hardware!);
      syncQuickFilters();
    });
  });
}

function shell(): void {
  appRoot.innerHTML = `
    <header>
      <h1>3D Game Catalog</h1>
      <p>686 lenticular 3D titles — TrueGame, UEVR, VRto3D, Odyssey Hub, ReShade, NVIDIA 3D Vision.</p>
    </header>
    <div class="toolbar">
      <input id="search" type="search" placeholder="Search title, platform, tags…" aria-label="Search games" />
      <label><input id="ultra-only" type="checkbox" /> 3D Ultra / native only</label>
      <label><input id="vision-certified" type="checkbox" /> 3D Vision certified</label>
      <label><input data-platform="truegame" type="checkbox" /> TrueGame</label>
      <label><input data-platform="uevr" type="checkbox" /> UEVR</label>
      <label><input data-platform="nvidia-3d-vision" type="checkbox" /> 3D Vision</label>
      <label><input data-platform="odyssey-hub" type="checkbox" /> Odyssey 3D</label>
      <label><input data-platform="reshade-depth" type="checkbox" /> ReShade depth</label>
      <label><input data-hardware="nvidia-3d-vision-generic" type="checkbox" /> NVIDIA hardware</label>
      <label><input data-hardware="samsung-g90xf" type="checkbox" /> Samsung 3D</label>
    </div>
    <div class="status"></div>
    <div id="grid-root"></div>
    <footer>
      <div>No cookies or analytics. Per-column filters and pagination powered by TanStack Table.</div>
      <ul>${DATA_COVERAGE_GAPS.map((g) => `<li>${escapeHtml(g)}</li>`).join("")}</ul>
    </footer>
  `;
  bindFilters();
}

async function loadCatalog(): Promise<void> {
  const base = import.meta.env.BASE_URL;
  const response = await fetch(`${base}data/catalog-v2.json`);
  if (!response.ok) throw new Error(`Failed to load catalog: ${response.status}`);
  catalog = (await response.json()) as CatalogDocument;

  shell();
  const gridRoot = appRoot.querySelector<HTMLDivElement>("#grid-root");
  const status = appRoot.querySelector<HTMLDivElement>(".status");
  if (!gridRoot) throw new Error("Missing grid root");

  grid = new CatalogGrid(gridRoot, catalog.games, (s) => {
    if (status) {
      status.textContent = `${s.filtered} of ${s.total} titles · page ${s.page}/${Math.max(s.pageCount, 1)} · sync ${catalog!.meta.syncStatus} · merged ${catalog!.meta.mergedAt}`;
    }
  });

  const params = new URLSearchParams(window.location.search);
  const appId = params.get("appId");
  if (appId) {
    const search = appRoot.querySelector<HTMLInputElement>("#search");
    if (search) search.value = appId;
    grid.setGlobalFilter(appId);
  }
}

loadCatalog().catch((error: unknown) => {
  appRoot.innerHTML = `<div class="empty">Could not load catalog: ${escapeHtml(String(error))}</div>`;
});
