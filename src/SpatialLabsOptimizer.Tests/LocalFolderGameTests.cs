using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class LocalFolderGameTests
{
    [Fact]
    public void LocalFolderGameScanner_ExcludesSetupAndUninstallExes()
    {
        Assert.True(LocalFolderGameScanner.IsExcludedExecutable("unins000.exe"));
        Assert.True(LocalFolderGameScanner.IsExcludedExecutable("Setup.exe"));
        Assert.True(LocalFolderGameScanner.IsExcludedExecutable("vcredist_x64.exe"));
        Assert.False(LocalFolderGameScanner.IsExcludedExecutable("Game.exe"));
    }

    [Fact]
    public void LocalFolderGameScanner_FindsGameExe_MaxDepthTwo()
    {
        var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"3dgo-local-{Guid.NewGuid()}"));
        var gameDir = Directory.CreateDirectory(Path.Combine(root.FullName, "MyGame"));
        var nested = Directory.CreateDirectory(Path.Combine(gameDir.FullName, "bin"));
        var gameExe = Path.Combine(nested.FullName, "MyGame.exe");
        File.WriteAllBytes(gameExe, new byte[4096]);
        File.WriteAllBytes(Path.Combine(nested.FullName, "setup.exe"), new byte[128]);

        var scanner = new LocalFolderGameScanner();
        var results = scanner.ScanFolders([root.FullName]);

        Assert.Single(results);
        Assert.Equal(gameExe, results[0].LaunchExe);
        Assert.Equal("bin", results[0].DisplayTitle);
    }

    [Fact]
    public void LocalFolderGameScanner_StableIdUsesLocalStoreMapper()
    {
        var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"3dgo-local-id-{Guid.NewGuid()}"));
        var gameDir = Directory.CreateDirectory(Path.Combine(root.FullName, "Demo"));
        var exePath = Path.Combine(gameDir.FullName, "Demo.exe");
        File.WriteAllBytes(exePath, new byte[2048]);

        var scanner = new LocalFolderGameScanner();
        var first = scanner.ScanFolders([root.FullName])[0];
        var second = scanner.ScanFolders([root.FullName])[0];

        var expectedKey = $"{LocalGameFolderRepository.NormalizePath(gameDir.FullName)}|Demo.exe";
        var expectedId = ExternalStoreIdMapper.StableAppId("Local", expectedKey);

        Assert.Equal(expectedId, first.StableAppId);
        Assert.Equal(first.StableAppId, second.StableAppId);
    }

    [Fact]
    public async Task LocalGameFolderRepository_PersistsJsonArray()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-local-repo-{Guid.NewGuid()}.db");
        await using var store = new SqliteSettingsStore(path);
        await store.InitializeAsync();
        var repo = new LocalGameFolderRepository(store);

        await repo.AddFolderAsync(@"D:\Games");
        await repo.AddFolderAsync(@"E:\ISOs\Installed");
        var folders = await repo.GetFoldersAsync();

        Assert.Equal(2, folders.Count);
        Assert.Contains(folders, f => f.EndsWith("Games", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LocalGameInstallResolver_UsesDirectExePath()
    {
        var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"3dgo-local-launch-{Guid.NewGuid()}"));
        var gameDir = Directory.CreateDirectory(Path.Combine(root.FullName, "LaunchMe"));
        var exePath = Path.Combine(gameDir.FullName, "LaunchMe.exe");
        await File.WriteAllBytesAsync(exePath, new byte[1024]);

        var scanner = new LocalFolderGameScanner();
        var scanned = scanner.ScanFolders([root.FullName])[0];

        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-local-db-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        await db.UpsertLocalInstallAsync(
            scanned.StableAppId,
            scanned.FolderPath,
            scanned.LaunchExe,
            scanned.DisplayTitle,
            isStale: false);

        var inner = new GameInstallPathResolver();
        var resolver = new LocalGameInstallResolver(db, inner);
        await resolver.WarmCacheAsync();

        var info = resolver.Resolve(scanned.StableAppId);
        Assert.NotNull(info);
        Assert.Equal(exePath, info!.LaunchExecutable);
    }

    [Fact]
    public async Task AddLocalFolder_GamesAppearAfterIndex()
    {
        var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"3dgo-local-index-{Guid.NewGuid()}"));
        var gameDir = Directory.CreateDirectory(Path.Combine(root.FullName, "IndexedGame"));
        var exePath = Path.Combine(gameDir.FullName, "IndexedGame.exe");
        File.WriteAllBytes(exePath, new byte[1024]);

        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-local-settings-{Guid.NewGuid()}.db");
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-local-games-{Guid.NewGuid()}.db");
        await using var store = new SqliteSettingsStore(settingsPath);
        await store.InitializeAsync();
        var folderRepo = new LocalGameFolderRepository(store);
        await folderRepo.AddFolderAsync(root.FullName);

        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        var scanner = new LocalFolderGameScanner();
        var resolver = new LocalGameInstallResolver(db, new GameInstallPathResolver());

        foreach (var localGame in scanner.ScanFolders(await folderRepo.GetFoldersAsync()))
        {
            await db.UpsertLocalInstallAsync(
                localGame.StableAppId,
                localGame.FolderPath,
                localGame.LaunchExe,
                localGame.DisplayTitle,
                isStale: false);
            await db.UpsertGameAsync(new Domain.GameCatalogItem(
                localGame.StableAppId,
                localGame.DisplayTitle,
                Domain.CompatibilityTier.Experimental,
                Domain.LaunchReadinessState.NeedsPresetCache,
                true,
                null, null, null, null, null, "Local", false));
        }

        var loaded = await db.GetGameAsync(scanner.ScanFolders([root.FullName])[0].StableAppId);
        Assert.NotNull(loaded);
        Assert.Equal("Local", loaded!.ReviewDescriptor);
    }

    [Fact]
    public async Task RemovedFolder_MarksInstallStale()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-local-stale-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        await db.UpsertLocalInstallAsync(12345, @"D:\Removed", @"D:\Removed\game.exe", "Removed", isStale: false);
        await db.MarkLocalInstallsStaleExceptAsync(Array.Empty<int>());

        var install = await db.GetLocalInstallAsync(12345);
        Assert.NotNull(install);
        Assert.True(install!.IsStale);
    }
}
