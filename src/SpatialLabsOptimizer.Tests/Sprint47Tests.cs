using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Security;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;
using System.Security.Cryptography;
using System.Text;

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

        var entropyPath = Path.Combine(Path.GetTempPath(), $"3dgo-entropy-{Guid.NewGuid()}.bin");
        var store = new DpapiSecretStore(entropyPath);
        var protectedValue = store.Protect("test-api-key-12345");
        Assert.False(string.IsNullOrWhiteSpace(protectedValue));
        Assert.Equal("test-api-key-12345", store.Unprotect(protectedValue));
    }

    [Fact]
    public void DpapiSecretStore_UsesPerInstallEntropy()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var pathA = Path.Combine(Path.GetTempPath(), $"3dgo-entropy-a-{Guid.NewGuid()}.bin");
        var pathB = Path.Combine(Path.GetTempPath(), $"3dgo-entropy-b-{Guid.NewGuid()}.bin");
        var storeA = new DpapiSecretStore(pathA);
        var storeB = new DpapiSecretStore(pathB);

        var protectedValue = storeA.Protect("install-specific-secret");
        Assert.Null(storeB.Unprotect(protectedValue));
        Assert.Equal("install-specific-secret", storeA.Unprotect(protectedValue));
    }

    [Fact]
    public void DpapiSecretStore_UnprotectsLegacyEntropy()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var legacyBytes = Encoding.UTF8.GetBytes("legacy-secret");
        var legacyProtected = Convert.ToBase64String(
            ProtectedData.Protect(
                legacyBytes,
                Encoding.UTF8.GetBytes("3d-game-optimizer-v1"),
                DataProtectionScope.CurrentUser));

        var store = new DpapiSecretStore(Path.Combine(Path.GetTempPath(), $"3dgo-entropy-{Guid.NewGuid()}.bin"));
        Assert.Equal("legacy-secret", store.Unprotect(legacyProtected));
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

    [Fact]
    public async Task SteamWebApiClient_ParsesOwnedGamesJson()
    {
        const string json = """
            {
              "response": {
                "games": [
                  { "appid": 570 },
                  { "appid": 730 }
                ]
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
        var client = new SteamWebApiClient(gateway);

        var appIds = await client.GetOwnedAppIdsAsync("test-key", "76561198000000001");

        Assert.Equal(new[] { 570, 730 }, appIds);
    }

    [Fact]
    public async Task SteamWebApiClient_ReturnsEmpty_OnMalformedJson()
    {
        var handler = new ReviewsTestHandler("{not-json");
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var gateway = new ExternalDataGateway(
            new Infrastructure.Privacy.PrivacyGuardHttpHandler(
                new Infrastructure.Privacy.PrivacyGuard(PrivacyAllowlist.DefaultHosts))
            {
                InnerHandler = handler
            },
            hub);
        var client = new SteamWebApiClient(gateway);

        var appIds = await client.GetOwnedAppIdsAsync("test-key", "76561198000000001");

        Assert.Empty(appIds);
    }

    [Fact]
    public async Task PlayerCountService_ReturnsNull_OnMalformedJson()
    {
        var handler = new ReviewsTestHandler("{not-json");
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var gateway = new ExternalDataGateway(
            new Infrastructure.Privacy.PrivacyGuardHttpHandler(
                new Infrastructure.Privacy.PrivacyGuard(PrivacyAllowlist.DefaultHosts))
            {
                InnerHandler = handler
            },
            hub);
        var service = new PlayerCountService(gateway);

        var count = await service.GetCurrentPlayersAsync(570, "test-key");

        Assert.Null(count);
    }

    [Fact]
    public async Task GameDatabase_ReadGamesAsync_RoundTripsIsCatalogTitle()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-catalog-title-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        await db.UpsertGameAsync(new GameCatalogItem(
            570,
            "Dota 2",
            CompatibilityTier.Playable,
            LaunchReadinessState.Ready,
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            true));

        var item = await db.GetGameAsync(570);
        Assert.NotNull(item);
        Assert.True(item!.IsCatalogTitle);
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
