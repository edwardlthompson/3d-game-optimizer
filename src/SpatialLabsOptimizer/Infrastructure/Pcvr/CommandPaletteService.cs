namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public sealed class CommandPaletteService
{
    private readonly List<CommandPaletteEntry> _commands = new()
    {
        new("play-3d", "Play in 3D", "Launch selected game in 3D"),
        new("play-vr", "Play in VR", "Launch selected game in VR"),
        new("setup-wizard", "Run Setup Wizard", "Silent toolchain install"),
        new("refresh-metadata", "Refresh Metadata", "Update Steam store data"),
        new("rescan-library", "Re-scan Library", "Re-index installed games"),
        new("cache-presets", "Cache Top Presets", "Bulk download UEVR presets"),
        new("open-logs", "Open Logs Folder", "Reveal local log directory"),
        new("toggle-safe-launch", "Toggle Safe Launch", "Enable or disable injector-free launches"),
        new("safe-launch", "Safe Launch selected game", "Enable safe launch and Play in 3D"),
        new("diagnostic-bundle", "Export Diagnostics", "Create redacted support bundle"),
        new("command-palette", "Quick Actions", "Open searchable shortcut list (Ctrl+K)"),
        new("shortcut-ctrl-k", "Keyboard: Ctrl+K", "Open Quick Actions from anywhere"),
        new("shortcut-safe", "Keyboard: Ctrl+Shift+S", "Toggle Safe Launch (streamer binding)"),
        new("setup-wizard-guided", "Guided Setup Wizard", "Three-step first-run: disclaimer, display, tools")
    };

    public IReadOnlyList<CommandPaletteEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _commands;
        }

        return _commands
            .Where(c => c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        c.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

public sealed record CommandPaletteEntry(string Id, string Title, string Description);
