import type { ListFilterMode } from "./list-filter";

export interface GridStatus {
  filtered: number;
  total: number;
  page: number;
  pageCount: number;
}

export interface GridOptions {
  wishlistFilter: ListFilterMode;
  libraryFilter: ListFilterMode;
  ultraOnly: boolean;
  visionCertifiedOnly: boolean;
  trueGameOnly: boolean;
  uevrOnly: boolean;
  /** Minimum 3D Rank score 0–100; 0 = any. */
  minRank3D: number;
}
