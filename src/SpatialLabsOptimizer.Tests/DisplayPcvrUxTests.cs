using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Settings;
using Windows.System;

namespace SpatialLabsOptimizer.Tests;

public class DisplayPcvrUxTests
{
    [Fact]
    public void DisplayChangeMonitor_DetectsEdidHotPlug()
    {
        var call = 0;
        var probe = new FakeDisplayEdidProbe(() =>
        {
            return call++ == 0
                ? [new DisplayEdidSnapshot("1", "sig-a", "Panel A")]
                :
                [
                    new DisplayEdidSnapshot("1", "sig-a", "Panel A"),
                    new DisplayEdidSnapshot("2", "sig-b", "Panel B")
                ];
        });
        var monitor = new DisplayChangeMonitor(probe);

        DisplayConfigurationChangedEventArgs? args = null;
        monitor.ConfigurationChanged += (_, e) => args = e;
        Assert.False(monitor.CheckForChanges());
        Assert.True(monitor.CheckForChanges());
        Assert.NotNull(args);
        Assert.Single(args!.Previous);
        Assert.Equal(2, args.Current.Count);
    }

    [Fact]
    public void ViewingDistanceCoach_EvaluatesInsideRange()
    {
        var coach = new ViewingDistanceCoach();
        var guide = coach.GetGuideForProfile("acer-psv27-2");

        Assert.Equal(70, guide.RecommendedDistanceCm);
        Assert.Contains("Perfect", coach.EvaluateDistance("acer-psv27-2", 70), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Move back", coach.EvaluateDistance("acer-psv27-2", 40), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MultiMonitorLaunchPicker_PersistsSelection()
    {
        var probe = new FakeDisplayEdidProbe(() =>
        [
            new DisplayEdidSnapshot("display-a", "sig-a", "3D Panel"),
            new DisplayEdidSnapshot("display-b", "sig-b", "Secondary")
        ]);

        var path = Path.Combine(Path.GetTempPath(), $"3dgo-display-{Guid.NewGuid()}.db");
        await using var store = new SqliteSettingsStore(path);
        await store.InitializeAsync();
        var prefs = new UserPreferencesService(store);
        var picker = new MultiMonitorLaunchPicker(probe, prefs);

        await picker.SetSelectedTargetAsync("display-b");
        var selected = await picker.GetSelectedTargetAsync();

        Assert.NotNull(selected);
        Assert.Equal("display-b", selected!.DeviceId);
        Assert.Equal("Secondary", selected.FriendlyName);
    }

    [Fact]
    public async Task OpenXrRuntimePicker_AppliesOverrideLabel()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-openxr-{Guid.NewGuid()}.db");
        await using var store = new SqliteSettingsStore(path);
        await store.InitializeAsync();
        var prefs = new UserPreferencesService(store);
        var picker = new OpenXrRuntimePicker(prefs);

        await picker.SetSelectedOverrideIdAsync("meta-link");
        var label = await picker.ResolveEffectiveRuntimeLabelAsync();

        Assert.Equal("OpenXR:Meta Quest Link", label);
    }

    [Fact]
    public void OpenXrRuntimeProbe_HonorsManualOverride()
    {
        var label = OpenXrRuntimeProbe.TryResolveActiveRuntimeLabel("virtual-desktop");
        Assert.Equal("OpenXR:Virtual Desktop", label);
    }

    [Fact]
    public void StreamFriendlyProfileService_ListsHotkeyBundles()
    {
        var service = new StreamFriendlyProfileService();
        var bundles = service.GetBundles();

        Assert.True(bundles.Count >= 2);
        Assert.Contains(bundles, b => b.Hotkeys.Any(h => h.Hotkey == "Ctrl+Shift+3"));
        Assert.Contains("Ctrl+Shift+3", service.FormatBundleForDisplay(bundles[0]), StringComparison.Ordinal);
    }

    [Fact]
    public void StreamerHotkeyService_RegistersBindingsAndHandlesKeys()
    {
        var service = new StreamerHotkeyService(new StreamFriendlyProfileService());
        Assert.Contains(service.Bindings, b => b.ActionId == "toggle-safe-launch" && b.DisplayLabel == "Ctrl+Shift+S");

        string? invoked = null;
        var handled = service.TryHandle(
            VirtualKey.S,
            ctrl: true,
            shift: true,
            alt: false,
            new StreamerHotkeyHandler
            {
                ExecuteCommandAsync = id =>
                {
                    invoked = id;
                    return Task.CompletedTask;
                },
                SetPreferredOutputAsync = _ => Task.CompletedTask,
                NavigateToSettings = () => { }
            });

        Assert.True(handled);
        Assert.Equal("toggle-safe-launch", invoked);
    }

    [Fact]
    public async Task GlossarySeed_LoadsEntries()
    {
        var loader = new JsonDataLoader(TestPaths.FindDataRoot());
        var doc = await loader.LoadAsync<GlossaryDocument>("glossary/glossary-v1.json");
        Assert.NotNull(doc);
        Assert.True(doc!.Entries.Count >= 9);
        Assert.Contains(doc.Entries, e => e.Term.Contains("SBS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CommandPaletteService_IncludesSprint37Actions()
    {
        var palette = new CommandPaletteService();
        var ids = palette.Search("").Select(c => c.Id).ToHashSet();

        Assert.Contains("cache-presets", ids);
        Assert.Contains("rescan-library", ids);
        Assert.Contains("open-logs", ids);
        Assert.Contains("toggle-safe-launch", ids);
        Assert.Contains(palette.Search("logs"), c => c.Id == "open-logs");
    }

    [Fact]
    public void WmiDisplayEdidProbe_BuildEdidSignature_ParsesPnpTokens()
    {
        var signature = WmiDisplayEdidProbe.BuildEdidSignature(
            @"DISPLAY\VEN_1234&PROD_ABCD&UID256",
            "Test panel");

        Assert.Equal("1234:abcd", signature);
    }

    [Fact]
    public void DisplayChangeMonitor_HasSnapshotChanged_IgnoresOrder()
    {
        var previous = new[]
        {
            new DisplayEdidSnapshot("a", "sig-b", "B"),
            new DisplayEdidSnapshot("b", "sig-a", "A")
        };
        var current = new[]
        {
            new DisplayEdidSnapshot("c", "sig-a", "A"),
            new DisplayEdidSnapshot("d", "sig-b", "B")
        };

        Assert.False(DisplayChangeMonitor.HasSnapshotChanged(previous, current));
    }
}
