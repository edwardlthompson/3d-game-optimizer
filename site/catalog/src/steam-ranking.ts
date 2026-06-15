import type { SteamStats } from "./types";

/** Prior mean Steam review % across the catalog (Bayesian shrinkage). */
const PRIOR_PERCENT = 75;
/** Equivalent review count before we trust raw percentages. */
const PRIOR_REVIEW_WEIGHT = 200;

/**
 * Catalog rank 0–100 blending review quality, review volume, and live players.
 * Low-sample 100% titles sink below well-reviewed popular games.
 */
export function weightedReviewScore(stats: SteamStats | undefined): number | null {
  if (!stats) return null;
  const pct = stats.reviewPercent;
  if (pct == null) return null;

  const reviews = stats.reviewCount ?? 0;
  const players = stats.currentPlayers ?? 0;

  const quality =
    (reviews * pct + PRIOR_REVIEW_WEIGHT * PRIOR_PERCENT) / (reviews + PRIOR_REVIEW_WEIGHT);

  const reviewSignal = Math.log10(1 + reviews) / 5;
  const playerSignal = Math.log10(1 + players) / 4;
  const credibility = Math.min(1, reviewSignal * 0.7 + playerSignal * 0.3);

  const score = quality * (0.4 + 0.6 * credibility);
  return Math.round(score * 10) / 10;
}
