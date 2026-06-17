export type OwnedGamesResult =
  | { ok: true; appIds: number[] }
  | { ok: false; reason: "missing_key" | "http_error" | "invalid_response" };

export async function fetchOwnedGames(apiKey: string, steamId: string): Promise<OwnedGamesResult> {
  if (!apiKey) return { ok: false, reason: "missing_key" };
  const url = new URL("https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/");
  url.searchParams.set("key", apiKey);
  url.searchParams.set("steamid", steamId);
  url.searchParams.set("include_appinfo", "0");
  url.searchParams.set("include_played_free_games", "1");

  const resp = await fetch(url.toString());
  if (!resp.ok) return { ok: false, reason: "http_error" };

  try {
    const data = (await resp.json()) as {
      response?: { games?: Array<{ appid: number }> };
    };
    const appIds = (data.response?.games ?? []).map((g) => g.appid);
    return { ok: true, appIds };
  } catch {
    return { ok: false, reason: "invalid_response" };
  }
}
