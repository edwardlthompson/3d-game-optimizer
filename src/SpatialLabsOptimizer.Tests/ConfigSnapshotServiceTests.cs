using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Tests;

public sealed class ConfigSnapshotServiceTests : IDisposable
{
    private readonly string _snapshotDir;
    private readonly string _dbPath;
    private readonly Infrastructure.Data.SqliteSettingsStore _store;
    private readonly GameOverrideRepository _overrides;

    public ConfigSnapshotServiceTests()
    {
        _snapshotDir = Path.Combine(Path.GetTempPath(), $"3dgo-snapdir-{Guid.NewGuid():N}");
        _dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-snapdb-{Guid.NewGuid():N}.db");
        _store = new SqliteSettingsStore(_dbPath);
        _store.InitializeAsync().GetAwaiter().GetResult();
        _overrides = new GameOverrideRepository(_store);
    }

    public void Dispose()
    {
        _store.DisposeAsync().AsTask().GetAwaiter().GetResult();
        if (Directory.Exists(_snapshotDir))
        {
            Directory.Delete(_snapshotDir, recursive: true);
        }
    }

    private ConfigSnapshotService CreateService() => new(_overrides, _snapshotDir);

    [Fact]
    public async Task SnapshotAsync_WritesJsonUnderInjectedDirectory()
    {
        var service = CreateService();
        var path = await service.SnapshotAsync(570);

        Assert.StartsWith(_snapshotDir, path, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task ListSnapshots_FiltersByAppId()
    {
        var service = CreateService();
        await service.SnapshotAsync(570);
        await service.SnapshotAsync(1091500);

        var filtered = service.ListSnapshots(570);
        Assert.Single(filtered);
        Assert.Equal(570, filtered[0].AppId);
    }

    [Fact]
    public async Task RollbackAsync_RestoresPriorOverride()
    {
        await _overrides.SaveAsync(new GameOverride(570, 0.7, 0.5, LaunchPlatform.Uevr, false, "Auto"));
        var service = CreateService();
        var path = await service.SnapshotAsync(570);

        await _overrides.SaveAsync(new GameOverride(570, 0.1, 0.1, LaunchPlatform.ReShade, true, "Headset"));
        await service.RollbackAsync(path);

        var restored = await _overrides.GetAsync(570);
        Assert.NotNull(restored);
        Assert.Equal(0.7, restored!.Depth);
    }

    [Fact]
    public async Task RollbackAsync_RemovesOverrideWhenSnapshotHasNullOverride()
    {
        await _overrides.SaveAsync(new GameOverride(570, 0.7, 0.5, LaunchPlatform.Uevr, false, "Auto"));
        var service = CreateService();
        var path = Path.Combine(_snapshotDir, "570-20260101120000123-test.json");
        await File.WriteAllTextAsync(
            path,
            """{"appId":570,"createdAt":"2026-01-01T00:00:00Z","override":null}""");

        await service.RollbackAsync(path);

        var restored = await _overrides.GetAsync(570);
        Assert.Null(restored);
    }

    [Fact]
    public async Task ListSnapshots_UsesFilenameTimestampOverCreationTime()
    {
        var service = CreateService();
        var path = await service.SnapshotAsync(570);
        var name = Path.GetFileNameWithoutExtension(path);
        var embedded = service.ListSnapshots(570)[0].CreatedAt;
        Assert.NotEqual(default, embedded);

        var listed = service.ListSnapshots(570);
        Assert.Single(listed);
        Assert.Equal(embedded, listed[0].CreatedAt);
    }
}
