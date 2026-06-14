using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class UpdateSchedulerTests
{
    [Fact]
    public async Task RunIfDueAsync_Off_SkipsNetworkAndUsesCache()
    {
        var store = new SqliteSettingsStore(Path.Combine(Path.GetTempPath(), $"3dgo-sched-{Guid.NewGuid()}.db"));
        await store.InitializeAsync();
        var prefs = new UserPreferencesService(store);
        await prefs.SetUpdateCheckIntervalAsync(UpdateCheckInterval.Off);
        await prefs.SetCachedUpdateResultAsync(new UpdateCheckResult(
            "1.0.1", "1.0.1", false, null, null, null, null));

        var service = CreateService(store, prefs, shouldFailIfCalled: true);
        var scheduler = new UpdateScheduler(service, prefs);

        await scheduler.RunIfDueAsync();

        Assert.NotNull(scheduler.LastResult);
        Assert.False(scheduler.IsUpdateAvailable);
        await store.DisposeAsync();
    }

    [Fact]
    public async Task IsCheckDueAsync_Daily_WhenStale_ReturnsTrue()
    {
        var store = new SqliteSettingsStore(Path.Combine(Path.GetTempPath(), $"3dgo-sched-{Guid.NewGuid()}.db"));
        await store.InitializeAsync();
        var prefs = new UserPreferencesService(store);
        await prefs.SetUpdateCheckIntervalAsync(UpdateCheckInterval.Daily);
        await prefs.SetLastUpdateCheckUtcAsync(DateTimeOffset.UtcNow.AddDays(-2));

        var service = CreateService(store, prefs);
        var due = await service.IsCheckDueAsync();
        Assert.True(due);
        await store.DisposeAsync();
    }

    [Fact]
    public async Task IsCheckDueAsync_Weekly_WhenRecent_ReturnsFalse()
    {
        var store = new SqliteSettingsStore(Path.Combine(Path.GetTempPath(), $"3dgo-sched-{Guid.NewGuid()}.db"));
        await store.InitializeAsync();
        var prefs = new UserPreferencesService(store);
        await prefs.SetUpdateCheckIntervalAsync(UpdateCheckInterval.Weekly);
        await prefs.SetLastUpdateCheckUtcAsync(DateTimeOffset.UtcNow.AddDays(-1));

        var service = CreateService(store, prefs);
        var due = await service.IsCheckDueAsync();
        Assert.False(due);
        await store.DisposeAsync();
    }

    private static UpdateService CreateService(
        SqliteSettingsStore store,
        UserPreferencesService prefs,
        bool shouldFailIfCalled = false)
    {
        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = shouldFailIfCalled
                ? new FailingHandler()
                : new StubMessageHandler()
        };
        var hub = new OperationProgressHub();
        var gateway = new ExternalDataGateway(handler, hub);
        var detector = new InstallArtifactDetector(new FakePackageProbe(false), new FakeMsiProbe(false));
        return new UpdateService(gateway, hub, prefs, detector);
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("Network should not be called when interval is Off.");
    }
}
