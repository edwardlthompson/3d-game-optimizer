export function readSteamReturnParams(): { token: string | null; error: string | null } {
  const hash = new URLSearchParams(window.location.hash.replace(/^#/, ""));
  const query = new URLSearchParams(window.location.search);
  return {
    token: hash.get("steam_sync_token") ?? query.get("steam_sync_token"),
    error: query.get("error") ?? hash.get("error"),
  };
}

export function clearSteamReturnParams(): void {
  const params = new URLSearchParams(window.location.search);
  params.delete("steam_sync_token");
  params.delete("error");
  const qs = params.toString();
  let next = qs ? `${window.location.pathname}?${qs}` : window.location.pathname;

  const hash = new URLSearchParams(window.location.hash.replace(/^#/, ""));
  hash.delete("steam_sync_token");
  hash.delete("error");
  const hashStr = hash.toString();
  if (hashStr) next += `#${hashStr}`;

  window.history.replaceState({}, "", next);
}

export function steamSyncErrorMessage(error: string): string {
  return error === "steam_api_failed"
    ? "Steam library API is unavailable. Try again later."
    : "Steam sign-in failed.";
}
