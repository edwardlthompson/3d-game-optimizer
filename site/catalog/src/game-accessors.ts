import type { CatalogGame } from "./types";

export const PLATFORM_LABELS: Record<string, string> = {
  truegame: "TrueGame",
  uevr: "UEVR",
  "nvidia-3d-vision": "3D Vision",
  "odyssey-hub": "Odyssey Hub",
  "reshade-depth": "ReShade",
  manual: "Curated",
  "asus-spatial-vision": "ASUS Spatial Vision",
};

export function visionLabel(game: CatalogGame): string {
  const nvidia = game.sources.find((s) => s.sourceId === "nvidia-3d-vision");
  if (!nvidia) return "—";
  return nvidia.label;
}

export function playMethodKey(entry: { platformKey: string; label: string }): string {
  return `${entry.platformKey}|${entry.label}`;
}

export function playMethodDisplay(entry: { platformKey: string; label: string }): string {
  const platform = PLATFORM_LABELS[entry.platformKey] ?? entry.platformKey;
  return `${platform} · ${entry.label}`;
}

export function playMethodsForGame(game: CatalogGame): Array<{ key: string; label: string }> {
  const support = game.platformSupport ?? [];
  return support.map((entry) => ({
    key: playMethodKey(entry),
    label: playMethodDisplay(entry),
  }));
}

export function playMethodsText(game: CatalogGame): string {
  return playMethodsForGame(game).map((m) => m.label).join(" · ");
}
