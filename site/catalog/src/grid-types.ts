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
}
