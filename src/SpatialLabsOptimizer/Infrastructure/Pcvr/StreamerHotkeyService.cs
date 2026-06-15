using Windows.System;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public sealed record StreamerHotkeyBinding(
    string ActionId,
    VirtualKey Key,
    bool Control,
    bool Shift,
    bool Alt,
    string DisplayLabel);

public sealed class StreamerHotkeyHandler
{
    public required Func<string, Task> ExecuteCommandAsync { get; init; }
    public required Func<string, Task> SetPreferredOutputAsync { get; init; }
    public required Action NavigateToSettings { get; init; }

    public async Task InvokeAsync(string actionId)
    {
        switch (actionId)
        {
            case "navigate-settings":
                NavigateToSettings();
                break;
            case "output-monitor":
                await SetPreferredOutputAsync("Monitor");
                break;
            case "output-headset":
                await SetPreferredOutputAsync("Headset");
                break;
            default:
                await ExecuteCommandAsync(actionId);
                break;
        }
    }
}

public sealed class StreamerHotkeyService
{
    private static readonly IReadOnlyList<(string ActionId, string Hotkey)> DefaultMappings =
    [
        ("navigate-settings", "Ctrl+Shift+3"),
        ("toggle-safe-launch", "Ctrl+Shift+S"),
        ("open-logs", "Ctrl+Shift+L"),
        ("output-monitor", "Ctrl+Shift+M"),
        ("output-headset", "Ctrl+Shift+H"),
        ("rescan-library", "Ctrl+Shift+R")
    ];

    private readonly IReadOnlyList<StreamerHotkeyBinding> _bindings;

    public StreamerHotkeyService(StreamFriendlyProfileService profiles)
    {
        _bindings = DefaultMappings
            .Select(m => ParseBinding(m.ActionId, m.Hotkey))
            .ToList();
        _ = profiles;
    }

    public IReadOnlyList<StreamerHotkeyBinding> Bindings => _bindings;

    public string Toggle3DHotkey =>
        _bindings.FirstOrDefault(b => b.ActionId == "navigate-settings")?.DisplayLabel ?? "Ctrl+Shift+3";

    public bool TryHandle(VirtualKey key, bool ctrl, bool shift, bool alt, StreamerHotkeyHandler handler)
    {
        foreach (var binding in _bindings)
        {
            if (binding.Key == key &&
                binding.Control == ctrl &&
                binding.Shift == shift &&
                binding.Alt == alt)
            {
                _ = handler.InvokeAsync(binding.ActionId);
                return true;
            }
        }

        return false;
    }

    internal static StreamerHotkeyBinding ParseBinding(string actionId, string hotkey)
    {
        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var ctrl = parts.Any(p => p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase));
        var shift = parts.Any(p => p.Equals("Shift", StringComparison.OrdinalIgnoreCase));
        var alt = parts.Any(p => p.Equals("Alt", StringComparison.OrdinalIgnoreCase));
        var keyToken = parts.LastOrDefault(p =>
            !p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) &&
            !p.Equals("Shift", StringComparison.OrdinalIgnoreCase) &&
            !p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) ?? "";

        return new StreamerHotkeyBinding(actionId, ParseVirtualKey(keyToken), ctrl, shift, alt, hotkey);
    }

    private static VirtualKey ParseVirtualKey(string token) => token switch
    {
        "3" => VirtualKey.Number3,
        "S" => VirtualKey.S,
        "L" => VirtualKey.L,
        "M" => VirtualKey.M,
        "H" => VirtualKey.H,
        "R" => VirtualKey.R,
        _ => Enum.TryParse<VirtualKey>(token, true, out var parsed) ? parsed : VirtualKey.None
    };
}
