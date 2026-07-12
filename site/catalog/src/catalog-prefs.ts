import type { ListFilterMode } from "./list-filter";
import type { GridOptions } from "./grid-types";

const PREFS_KEY = "3d-catalog-toolbar-prefs-v1";

export interface CatalogToolbarPrefs {
  wishlistFilter: ListFilterMode;
  libraryFilter: ListFilterMode;
  ultraOnly: boolean;
  visionCertifiedOnly: boolean;
  trueGameOnly: boolean;
  uevrOnly: boolean;
  minRank3D: number;
  pageSize?: number;
}

export function defaultToolbarPrefs(): CatalogToolbarPrefs {
  return {
    wishlistFilter: "all",
    libraryFilter: "all",
    ultraOnly: false,
    visionCertifiedOnly: false,
    trueGameOnly: false,
    uevrOnly: false,
    minRank3D: 0,
  };
}

export function loadToolbarPrefs(): CatalogToolbarPrefs {
  try {
    const raw = localStorage.getItem(PREFS_KEY);
    if (!raw) return defaultToolbarPrefs();
    const parsed = JSON.parse(raw) as Partial<CatalogToolbarPrefs>;
    return { ...defaultToolbarPrefs(), ...parsed };
  } catch {
    return defaultToolbarPrefs();
  }
}

export function saveToolbarPrefs(prefs: CatalogToolbarPrefs): void {
  localStorage.setItem(PREFS_KEY, JSON.stringify(prefs));
}

export function prefsToGridOptions(prefs: CatalogToolbarPrefs): GridOptions {
  return {
    wishlistFilter: prefs.wishlistFilter,
    libraryFilter: prefs.libraryFilter,
    ultraOnly: prefs.ultraOnly,
    visionCertifiedOnly: prefs.visionCertifiedOnly,
    trueGameOnly: prefs.trueGameOnly,
    uevrOnly: prefs.uevrOnly,
    minRank3D: prefs.minRank3D,
  };
}
