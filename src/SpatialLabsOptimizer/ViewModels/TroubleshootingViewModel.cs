using System.Windows.Input;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class TroubleshootingViewModel : ViewModelBase
{
    private readonly DiagnosticBundleService _diagnostics;
    private readonly LaunchDryRunService _dryRun;
    private readonly SeedContributionExportService _seedExport;
    private readonly GameDatabase _database;
    private readonly WorkshopPresetImporter? _workshopImporter;
    private readonly LanPartyExportService? _lanExport;
    private readonly LanPresetExportService? _lanPresetExport;
    private readonly HybridSessionService? _hybridSession;
    private readonly ThreeDGoCodeService? _codes;

    private string _statusText = "";
    private string _v2StatusText = "";
    private string _dryRunAppId = "";
    private string _workshopUrl = "";
    private bool _v2Enabled;
    private IReadOnlyList<TroubleshootingExportItemViewModel> _exportItems = [];

    public TroubleshootingViewModel(
        DiagnosticBundleService diagnostics,
        LaunchDryRunService dryRun,
        SeedContributionExportService seedExport,
        GameDatabase database,
        WorkshopPresetImporter? workshopImporter = null,
        LanPartyExportService? lanExport = null,
        LanPresetExportService? lanPresetExport = null,
        HybridSessionService? hybridSession = null,
        ThreeDGoCodeService? codes = null)
    {
        _diagnostics = diagnostics;
        _dryRun = dryRun;
        _seedExport = seedExport;
        _database = database;
        _workshopImporter = workshopImporter;
        _lanExport = lanExport;
        _lanPresetExport = lanPresetExport;
        _hybridSession = hybridSession;
        _codes = codes;

        ExportDiagnosticsCommand = new RelayCommand(ExportDiagnosticsAsync);
        DryRunCommand = new RelayCommand(DryRunAsync);
        SeedExportCommand = new RelayCommand(SeedExportAsync);
        WorkshopImportCommand = new RelayCommand(WorkshopImportAsync);
        LanExportCommand = new RelayCommand(LanExportAsync);
        LanPresetExportCommand = new RelayCommand(LanPresetExportAsync);
        HybridSessionCommand = new RelayCommand(HybridSessionAsync);
    }

    public bool V2Enabled
    {
        get => _v2Enabled;
        private set => SetProperty(ref _v2Enabled, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string V2StatusText
    {
        get => _v2StatusText;
        private set => SetProperty(ref _v2StatusText, value);
    }

    public string DryRunAppId
    {
        get => _dryRunAppId;
        set => SetProperty(ref _dryRunAppId, value);
    }

    public string WorkshopUrl
    {
        get => _workshopUrl;
        set => SetProperty(ref _workshopUrl, value);
    }

    public IReadOnlyList<TroubleshootingExportItemViewModel> ExportItems
    {
        get => _exportItems;
        private set => SetProperty(ref _exportItems, value);
    }

    public ICommand ExportDiagnosticsCommand { get; }
    public ICommand DryRunCommand { get; }
    public ICommand SeedExportCommand { get; }
    public ICommand WorkshopImportCommand { get; }
    public ICommand LanExportCommand { get; }
    public ICommand LanPresetExportCommand { get; }
    public ICommand HybridSessionCommand { get; }

    public async Task LoadAsync()
    {
        V2Enabled = FeatureFlags.V2Enabled;
        if (!V2Enabled)
        {
            return;
        }

        await _database.InitializeAsync();
        var games = await _database.GetReadyToPlayAsync();
        if (games.Count == 0)
        {
            games = await _database.GetAllGamesAsync();
        }

        ExportItems = games.Take(25)
            .Select(g => new TroubleshootingExportItemViewModel(g.SteamAppId, g.Title))
            .ToList();

        if (ExportItems.Count > 0 && string.IsNullOrWhiteSpace(DryRunAppId))
        {
            DryRunAppId = ExportItems[0].AppId.ToString();
        }
    }

    private IReadOnlyList<LanPartyExportService.ExportEntry> GetSelectedExportEntries()
        => ExportItems
            .Where(item => item.IsSelected)
            .Select(item => new LanPartyExportService.ExportEntry(item.AppId, item.Title))
            .ToList();

    public async Task ExportDiagnosticsAsync()
    {
        var path = await _diagnostics.ExportAsync();
        StatusText = $"Exported to: {path}";
    }

    public async Task DryRunAsync()
    {
        if (!int.TryParse(DryRunAppId?.Trim(), out var appId) || appId <= 0)
        {
            StatusText = "Enter a valid Steam App ID for dry run.";
            return;
        }

        var result = await _dryRun.SimulateAsync(appId);
        var code = result.PredictedErrorCode ?? "none";
        StatusText = result.WouldSucceed
            ? $"Dry run OK for app {appId} · {result.Steps.Count} steps · preview: {result.Preview?.PlatformLine}"
            : $"Dry run would fail with {code} · last step: {result.Steps.LastOrDefault()}";
    }

    public async Task SeedExportAsync()
    {
        var path = await _seedExport.ExportAsync();
        StatusText = $"Seed contribution JSON: {path}";
    }

    public async Task WorkshopImportAsync()
    {
        if (_workshopImporter is null)
        {
            V2StatusText = "Enable SPATIALLABS_ENABLE_V2=true to import workshop presets.";
            return;
        }

        var customUrl = WorkshopUrl?.Trim();
        var count = string.IsNullOrWhiteSpace(customUrl)
            ? await _workshopImporter.ImportAllowlistedSourcesAsync()
            : await _workshopImporter.ImportFromUrlAsync(customUrl);
        V2StatusText = $"Imported {count} preset profile(s).";
    }

    public async Task LanExportAsync()
    {
        if (_lanExport is null || _codes is null)
        {
            V2StatusText = "Enable SPATIALLABS_ENABLE_V2=true for LAN export.";
            return;
        }

        var selected = GetSelectedExportEntries();
        if (selected.Count == 0)
        {
            V2StatusText = "Select at least one library title for LAN export.";
            return;
        }

        var path = await _lanExport.ExportSessionAsync(selected);
        V2StatusText = $"LAN export: {path} · session code {_codes.GenerateCode()}";
    }

    public async Task LanPresetExportAsync()
    {
        if (_lanPresetExport is null)
        {
            V2StatusText = "Enable SPATIALLABS_ENABLE_V2=true for LAN preset export.";
            return;
        }

        var selected = GetSelectedExportEntries();
        if (selected.Count == 0)
        {
            V2StatusText = "Select at least one library title for LAN preset export.";
            return;
        }

        var entries = selected
            .Select(entry => new LanPresetExportService.ExportEntry(entry.AppId, entry.Title))
            .ToList();
        var path = await _lanPresetExport.ExportAllowlistedPresetsAsync(entries);
        V2StatusText = $"LAN preset allowlist: {path}";
    }

    public async Task HybridSessionAsync()
    {
        if (_hybridSession is null)
        {
            V2StatusText = "Enable SPATIALLABS_ENABLE_V2=true for hybrid co-op.";
            return;
        }

        var selected = GetSelectedExportEntries();
        if (selected.Count == 0)
        {
            V2StatusText = "Select a host title for hybrid co-op session.";
            return;
        }

        var session = await _hybridSession.CreateSessionAsync(selected[0].AppId);
        V2StatusText = $"Hybrid session {session.SessionCode} for app {session.HostAppId}.";
    }
}
