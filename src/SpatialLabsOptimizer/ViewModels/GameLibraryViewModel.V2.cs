using System.Windows.Input;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class GameLibraryViewModel
{
    private readonly WorkshopPresetImporter? _workshopImporter;
    private readonly LanPartyExportService? _lanExport;
    private readonly HybridSessionService? _hybridSession;
    private readonly ThreeDGoCodeService? _codes;

    private string _v2StatusText = "";
    private bool _v2Enabled;

    public bool V2Enabled
    {
        get => _v2Enabled;
        private set => SetProperty(ref _v2Enabled, value);
    }

    public string V2StatusText
    {
        get => _v2StatusText;
        private set => SetProperty(ref _v2StatusText, value);
    }

    public ICommand V2WorkshopImportCommand { get; private set; } = null!;
    public ICommand V2LanExportCommand { get; private set; } = null!;
    public ICommand V2HybridSessionCommand { get; private set; } = null!;

    private IReadOnlyList<LanPartyExportService.ExportEntry> GetV2ExportEntries()
    {
        if (SelectedGame is not null)
        {
            return [new LanPartyExportService.ExportEntry(SelectedGame.SteamAppId, SelectedGame.Title)];
        }

        return Games.Take(10)
            .Select(g => new LanPartyExportService.ExportEntry(g.SteamAppId, g.Title))
            .ToList();
    }

    public async Task WorkshopImportAsync()
    {
        if (_workshopImporter is null)
        {
            V2StatusText = "Enable SPATIALLABS_ENABLE_V2=true to import workshop presets.";
            return;
        }

        var count = await _workshopImporter.ImportAllowlistedSourcesAsync();
        V2StatusText = $"Imported {count} preset profile(s) from allowlisted sources.";
    }

    public async Task LanExportAsync()
    {
        if (_lanExport is null || _codes is null)
        {
            V2StatusText = "Enable SPATIALLABS_ENABLE_V2=true for LAN export.";
            return;
        }

        var selected = GetV2ExportEntries();
        if (selected.Count == 0)
        {
            V2StatusText = "Select a library title or refresh the library for LAN export.";
            return;
        }

        var path = await _lanExport.ExportSessionAsync(selected);
        V2StatusText = $"LAN export: {path} · session code {_codes.GenerateCode()}";
    }

    public async Task HybridSessionAsync()
    {
        if (_hybridSession is null)
        {
            V2StatusText = "Enable SPATIALLABS_ENABLE_V2=true for hybrid co-op.";
            return;
        }

        var selected = GetV2ExportEntries();
        if (selected.Count == 0)
        {
            V2StatusText = "Select a host title for hybrid co-op session.";
            return;
        }

        var session = await _hybridSession.CreateSessionAsync(selected[0].AppId);
        V2StatusText = $"Hybrid session {session.SessionCode} for {selected[0].Title}.";
    }
}
