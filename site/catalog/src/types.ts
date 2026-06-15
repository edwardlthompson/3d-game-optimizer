export type SupportLevel =
  | "ultra3d"
  | "native3d"
  | "optimized3d"
  | "playable3d"
  | "experimental3d"
  | "unsupported2d";

export interface CatalogSource {
  sourceId: string;
  level: SupportLevel;
  label: string;
  syncedAt: string;
  supportStatus?: string;
}

export interface HardwareRequirements {
  displays: string[];
  exclusiveTo: string[];
  notes?: string;
}

export interface SteamStats {
  releaseDate?: string;
  currentPlayers?: number;
  peakPlayers?: number;
  reviewPercent?: number;
  reviewCount?: number;
  priceUsd?: number;
  tags?: string[];
}

export interface CatalogGame {
  id: string;
  title: string;
  steamAppId?: number;
  steamMatchConfidence?: number;
  bestLevel: SupportLevel;
  bestExperience?: {
    level: SupportLevel;
    platformKey: string;
    sourceId: string;
    label: string;
  };
  platformSupport?: Array<{
    platformKey: string;
    sourceId: string;
    level: SupportLevel;
    label: string;
  }>;
  purchaseLinks?: { steam?: string };
  trueGameLabel?: string;
  platforms: string[];
  sources: CatalogSource[];
  hardwareRequirements: HardwareRequirements;
  steamStats?: SteamStats;
  steamTags?: string[];
  tiersByVendor: Record<string, string>;
  flags?: Record<string, boolean>;
}

export interface CatalogDocument {
  version: string;
  meta: {
    mergedAt: string;
    syncStatus: string;
    gameCount: number;
  };
  games: CatalogGame[];
}

export type SortKey =
  | "title"
  | "bestLevel"
  | "bestExperience"
  | "releaseDate"
  | "reviewPercent"
  | "weightedReview"
  | "currentPlayers"
  | "priceUsd";

export interface CatalogFilters {
  query: string;
  ultraOnly: boolean;
  platforms: Set<string>;
  hardware: Set<string>;
  visionCertifiedOnly: boolean;
}
