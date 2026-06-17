using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Security;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public sealed class PlatformConnectionServiceTests
{
    [Fact]
    public async Task ValidateSteamAsync_RejectsEmptyCredentials()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"slo-conn-{Guid.NewGuid():N}.db");
        await using var settings = new SqliteSettingsStore(dbPath);
        await settings.InitializeAsync();

        var hub = new OperationProgressHub();
        var guard = new PrivacyGuard(PrivacyAllowlist.DefaultHosts);
        var gateway = new ExternalDataGateway(new PrivacyGuardHttpHandler(guard), hub);
        var service = new PlatformConnectionService(
            new PlatformConnectionRepository(settings, new DpapiSecretStore()),
            new SteamWebApiClient(gateway),
            new SteamVdfScanner(),
            new EpicGogLibraryScanner(null, null),
            new UbisoftConnectScanner());

        var missingKey = await service.ValidateSteamAsync("76561198000000000", "");
        var missingId = await service.ValidateSteamAsync("", "abc123");

        Assert.False(missingKey.Success);
        Assert.False(missingId.Success);
        Assert.Contains("Enter your Steam ID64", missingKey.Message);
    }
}
