/** Same threshold as scripts/sync-catalog/resolve-steam-appids.py */
export const MIN_STEAM_MATCH_CONFIDENCE = 0.92;

export function isLinkableSteamApp(
  steamAppId: number | undefined,
  confidence: number | undefined,
): steamAppId is number {
  return !!steamAppId && (confidence ?? 0) >= MIN_STEAM_MATCH_CONFIDENCE;
}
