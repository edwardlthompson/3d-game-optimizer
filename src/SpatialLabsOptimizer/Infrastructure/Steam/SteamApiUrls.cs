namespace SpatialLabsOptimizer.Infrastructure.Steam;

internal static class SteamApiUrls
{
    public static string PlayerCount(int appId, string apiKey)
        => Build("https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/", new Dictionary<string, string>
        {
            ["appid"] = appId.ToString(),
            ["key"] = apiKey,
        });

    public static string OwnedGames(string apiKey, string steamId)
        => Build("https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/", new Dictionary<string, string>
        {
            ["key"] = apiKey,
            ["steamid"] = steamId,
            ["include_appinfo"] = "0",
            ["include_played_free_games"] = "1",
        });

    private static string Build(string baseUrl, IReadOnlyDictionary<string, string> query)
    {
        var builder = new UriBuilder(baseUrl);
        var parts = query.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");
        builder.Query = string.Join("&", parts);
        return builder.Uri.AbsoluteUri;
    }
}
