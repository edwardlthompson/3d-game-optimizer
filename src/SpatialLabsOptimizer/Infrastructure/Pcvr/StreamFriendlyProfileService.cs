namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public sealed record StreamHotkeyEntry(string Action, string Hotkey, string Description);

public sealed record StreamHotkeyBundle(string Name, string Description, IReadOnlyList<StreamHotkeyEntry> Hotkeys);

public sealed class StreamFriendlyProfileService
{
    private static readonly IReadOnlyList<StreamHotkeyBundle> Bundles =
    [
        new StreamHotkeyBundle(
            "Stream overlay bundle",
            "Suggested keys for OBS / broadcast overlays — register in your streaming app.",
            [
                new StreamHotkeyEntry("Toggle 3D depth", "Ctrl+Shift+3", "Quick depth on/off for viewers"),
                new StreamHotkeyEntry("Safe launch", "Ctrl+Shift+S", "Launch without injectors"),
                new StreamHotkeyEntry("Open logs folder", "Ctrl+Shift+L", "Support bundle path")
            ]),
        new StreamHotkeyBundle(
            "Couch co-op bundle",
            "Hand off between monitor and headset without leaving the game.",
            [
                new StreamHotkeyEntry("Preferred output: monitor", "Ctrl+Shift+M", "Force lenticular panel"),
                new StreamHotkeyEntry("Preferred output: headset", "Ctrl+Shift+H", "Force PCVR path"),
                new StreamHotkeyEntry("Re-scan library", "Ctrl+Shift+R", "Refresh installs after hot-plug")
            ])
    ];

    public IReadOnlyList<StreamHotkeyBundle> GetBundles() => Bundles;

    public string FormatBundleForDisplay(StreamHotkeyBundle bundle)
    {
        var lines = bundle.Hotkeys.Select(h => $"{h.Action}: {h.Hotkey} — {h.Description}");
        return $"{bundle.Name} — {bundle.Description}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }
}
