using System.Net;
using System.Text;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class UpdateServiceTests
{
    [Fact]
    public void PrivacyAllowlist_IncludesGitHubApi()
    {
        Assert.Contains("api.github.com", PrivacyAllowlist.DefaultHosts);
    }

    [Fact]
    public void SemverComparer_DetectsNewerProductVersion()
    {
        Assert.True(SemverComparer.IsNewer("1.1.0", "1.0.1"));
        Assert.False(SemverComparer.IsNewer("1.0.1", "1.1.0"));
    }

    [Fact]
    public void SemverComparer_ParseTagVersion_FiltersProductTags()
    {
        Assert.Equal("1.1.0", SemverComparer.ParseTagVersion("SpatialLabsOptimizer-v1.1.0"));
        Assert.Null(SemverComparer.ParseTagVersion("v0.7.1"));
    }

    [Fact]
    public async Task UpdateService_FiltersProductTagAndMatchesZipAsset()
    {
        const string releasesJson = """
            [
              {
                "tag_name": "v0.7.1",
                "html_url": "https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/v0.7.1",
                "assets": []
              },
              {
                "tag_name": "SpatialLabsOptimizer-v1.2.0",
                "html_url": "https://github.com/edwardlthompson/3d-game-optimizer/releases/tag/SpatialLabsOptimizer-v1.2.0",
                "assets": [
                  {
                    "name": "SpatialLabsOptimizer-1.2.0-win-x64.zip",
                    "browser_download_url": "https://objects.githubusercontent.com/example/SpatialLabsOptimizer-1.2.0-win-x64.zip"
                  }
                ]
              }
            ]
            """;

        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new FixedResponseHandler(releasesJson)
        };
        var hub = new OperationProgressHub();
        var gateway = new ExternalDataGateway(handler, hub);
        var store = new SqliteSettingsStore(Path.Combine(Path.GetTempPath(), $"3dgo-update-{Guid.NewGuid()}.db"));
        await store.InitializeAsync();
        var prefs = new UserPreferencesService(store);
        await prefs.SetInstallArtifactTypeAsync(InstallArtifactType.Zip);
        var detector = new InstallArtifactDetector(
            new FakePackageProbe(false),
            new FakeMsiProbe(false));
        var service = new UpdateService(gateway, hub, prefs, detector);

        var result = await service.CheckForUpdateAsync();

        Assert.Equal("1.2.0", result.LatestVersion);
        Assert.True(result.IsUpdateAvailable);
        Assert.Equal(InstallArtifactType.Zip, result.DownloadArtifactType);
        Assert.Contains(".zip", result.MatchedAssetName, StringComparison.OrdinalIgnoreCase);
        await store.DisposeAsync();
    }

    private sealed class FixedResponseHandler : HttpMessageHandler
    {
        private readonly string _body;

        public FixedResponseHandler(string body) => _body = body;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
