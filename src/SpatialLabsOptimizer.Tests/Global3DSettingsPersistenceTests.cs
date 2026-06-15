using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Tests;

public sealed class Global3DSettingsPersistenceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteSettingsStore _store;
    private readonly UserPreferencesService _prefs;

    public Global3DSettingsPersistenceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-g3d-prefs-{Guid.NewGuid():N}.db");
        _store = new SqliteSettingsStore(_dbPath);
        _store.InitializeAsync().GetAwaiter().GetResult();
        _prefs = new UserPreferencesService(_store);
    }

    public void Dispose()
    {
        _store.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task StereoscopyDefaults_RoundTripThroughPreferences()
    {
        await _prefs.SetDefaultDepthAsync(0.42);
        await _prefs.SetDefaultConvergenceAsync(0.33);

        Assert.Equal(0.42, await _prefs.GetDefaultDepthAsync(), 2);
        Assert.Equal(0.33, await _prefs.GetDefaultConvergenceAsync(), 2);
    }

    [Fact]
    public async Task StereoscopyDefaults_UseBuiltInFallbacksWhenUnset()
    {
        Assert.Equal(0.65, await _prefs.GetDefaultDepthAsync(), 2);
        Assert.Equal(0.5, await _prefs.GetDefaultConvergenceAsync(), 2);
    }

    [Fact]
    public async Task LaunchSafetyPreferences_RoundTrip()
    {
        await _prefs.SetSafeLaunchAsync(true);
        await _prefs.SetTrainerCoexistenceAsync(false);
        await _prefs.SetModManagerCoexistenceAsync(false);
        await _prefs.SetSimpleModeAsync(true);

        Assert.True(await _prefs.GetSafeLaunchAsync());
        Assert.False(await _prefs.GetTrainerCoexistenceAsync());
        Assert.False(await _prefs.GetModManagerCoexistenceAsync());
        Assert.True(await _prefs.GetSimpleModeAsync());
    }

    [Fact]
    public async Task LaunchTargetDisplay_PersistsInPreferences()
    {
        await _prefs.SetLaunchTargetDisplayAsync("display-test-1");

        Assert.Equal("display-test-1", await _prefs.GetLaunchTargetDisplayAsync());
    }

    [Fact]
    public async Task OpenXrRuntimeOverride_PersistsThroughPicker()
    {
        var picker = new OpenXrRuntimePicker(_prefs);

        await picker.SetSelectedOverrideIdAsync("steamvr");
        var saved = await _prefs.GetOpenXrRuntimeOverrideAsync();

        Assert.Equal("steamvr", saved);
        Assert.Equal("steamvr", await picker.GetSelectedOverrideIdAsync());
    }
}
