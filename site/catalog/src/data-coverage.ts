export const DATA_COVERAGE_GAPS = [
  "686 titles merged from TrueGame, UEVR, VRto3D wiki, Odyssey Hub seed, ReShade curated, and NVIDIA 3D Vision.",
  "638 titles include Steam purchase links; reviews and concurrent players from official Steam APIs.",
  "3D Rank is the single best path score per title (not cumulative). Game Rank blends weighted Steam popularity with 3D Rank.",
  "Connect Steam (when enabled) marks Lib checkmarks from your owned Steam App IDs; data stays on this device. Titles link to the Steam store when App ID confidence ≥ 0.92.",
  "Play methods lists every compatibility path, not only the top-ranked one.",
  "Wishlist and library stay on this device — export/import JSON to back up.",
  "Price graphs use self-tracked history from weekly catalog sync (Steam Store API only; see ADR-0005).",
] as const;

export const DONATE_URL = "https://venmo.com/code?user_id=1857304970395648420";
