import type { SupportLevel } from "./types";

export const LEVEL_RANK: Record<SupportLevel, number> = {
  ultra3d: 0,
  native3d: 1,
  optimized3d: 2,
  playable3d: 3,
  experimental3d: 4,
  unsupported2d: 5,
};

export const LEVEL_LABEL: Record<string, string> = {
  ultra3d: "3D Ultra",
  native3d: "3D",
  optimized3d: "Optimized",
  playable3d: "Playable",
  experimental3d: "Experimental",
  unsupported2d: "Unsupported",
};

export const DISPLAY_LABEL: Record<string, string> = {
  "acer-psv27-2": "Acer SpatialLabs",
  "acer-asv15-1": 'Acer SpatialLabs 15"',
  "acer-spatiallabs-15": "Acer Laptop 3D",
  "samsung-g90xf": "Samsung Odyssey 3D",
  "nvidia-3d-vision-generic": "NVIDIA 3D Vision",
  "generic-manual": "Generic stereo",
};

export const PAGE_SIZES = [25, 50, 100, 250] as const;

export function formatLevel(level: string): string {
  return LEVEL_LABEL[level] ?? level;
}
