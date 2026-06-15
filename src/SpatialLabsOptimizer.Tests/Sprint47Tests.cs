using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Security;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public sealed class Sprint47Tests
{
    [Fact]
    public void PrivacyAllowlist_IncludesStoreImageCdns()
    {
        Assert.Contains("shared.fastly.steamstatic.com", PrivacyAllowlist.DefaultHosts);
        Assert.Contains("media.steampowered.com", PrivacyAllowlist.DefaultHosts);
    }

    [Fact]
    public void DpapiSecretStore_RoundTripsApiKey()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var store = new DpapiSecretStore();
        var protectedValue = store.Protect("test-api-key-12345");
        Assert.False(string.IsNullOrWhiteSpace(protectedValue));
        Assert.Equal("test-api-key-12345", store.Unprotect(protectedValue));
    }

    [Fact]
    public async Task UserPreferencesService_PersistsLibraryUiPrefs()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-prefs-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(dbPath);
        var prefs = new UserPreferencesService(settings);

        await prefs.SetLibraryUiPrefsAsync(new LibraryUiPrefs(
            SortMode: "Name",
            SmartCollection: "LocalOnly",
            ShowFavoritesOnly: true,
            ShowLocalOnly: true,
            ShowWhyNotReady: false,
            LastPlaylistName: "My session list"));

        var loaded = await prefs.GetLibraryUiPrefsAsync();
        Assert.Equal("Name", loaded.SortMode);
        Assert.Equal("LocalOnly", loaded.SmartCollection);
        Assert.True(loaded.ShowFavoritesOnly);
        Assert.True(loaded.ShowLocalOnly);
        Assert.Equal("My session list", loaded.LastPlaylistName);
    }

    [Fact]
    public async Task PlatformConnectionRepository_StoresSteamIdAndEncryptedKey()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-conn-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(dbPath);
        var repo = new PlatformConnectionRepository(settings, new DpapiSecretStore());

        await repo.SetSteamIdAsync("76561198000000000");
        await repo.SetSteamApiKeyAsync("secret-key");

        Assert.Equal("76561198000000000", await repo.GetSteamIdAsync());
        Assert.Equal("secret-key", await repo.GetSteamApiKeyAsync());
        Assert.True(await repo.HasSteamCredentialsAsync());
    }

    [Fact]
    public void UbisoftConnectScanner_ReturnsEmpty_WhenNotInstalled()
    {
        var scanner = new UbisoftConnectScanner(Path.Combine(Path.GetTempPath(), $"3dgo-ubi-{Guid.NewGuid()}"));
        Assert.Empty(scanner.ScanInstalledGames());
    }

    [Fact]
    public async Task SteamAppReviewsClient_ParsesFixtureJson()
    {
        const string json = """
            {
              "success": 1,
              "query_summary": {
                "total_reviews": 12000,
                "review_score": 8,
                "review_score_desc": "Very Positive"
              }
            }
            """;

        var handler = new ReviewsTestHandler(json);
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var gateway = new ExternalDataGateway(
            new Infrastructure.Privacy.PrivacyGuardHttpHandler(
                new Infrastructure.Privacy.PrivacyGuard(PrivacyAllowlist.DefaultHosts))
            {
                InnerHandler = handler
            },
            hub);
        var client = new SteamAppReviewsClient(gateway);

        var (percent, count, sortScore, descriptor) = await client.GetReviewSummaryAsync(570);

        Assert.Equal(80, percent);
        Assert.Equal(12000, count);
        Assert.True(sortScore > 0);
        Assert.Equal("Very Positive", descriptor);
    }

    [Fact]
    public void StoreCoverPlaceholder_ResolvesBundledAssetWhenPresent()
    {
        var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        if (!File.Exists(Path.Combine(assetsDir, "placeholder-cover.png")))
        {
            return;
        }

        var path = Infrastructure.Artwork.StoreCoverPlaceholder.ResolveBundledPath("Epic");
        Assert.NotNull(path);
        Assert.True(File.Exists(path));
    }

    private sealed class ReviewsTestHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
    }
}
