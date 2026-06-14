using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Views;

public sealed partial class TroubleshootingView : Page
{
    public TroubleshootingView()
    {
        InitializeComponent();
        Loaded += TroubleshootingView_Loaded;
    }

    private async void TroubleshootingView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        V2Panel.Visibility = FeatureFlags.V2Enabled
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;

        if (FeatureFlags.V2Enabled)
        {
            await LoadLibraryExportListAsync();
        }
    }

    private async Task LoadLibraryExportListAsync()
    {
        LibraryExportPanel.Children.Clear();
        var db = App.Services.GetRequiredService<GameDatabase>();
        await db.InitializeAsync();
        var games = await db.GetReadyToPlayAsync();
        if (games.Count == 0)
        {
            games = await db.GetAllGamesAsync();
        }

        foreach (var game in games.Take(25))
        {
            LibraryExportPanel.Children.Add(new CheckBox
            {
                Content = game.Title,
                Tag = game.SteamAppId,
                IsChecked = true
            });
        }

        if (games.Count > 0 && string.IsNullOrWhiteSpace(DryRunAppIdBox.Text))
        {
            DryRunAppIdBox.Text = games[0].SteamAppId.ToString();
        }
    }

    private IReadOnlyList<LanPartyExportService.ExportEntry> GetSelectedExportEntries()
    {
        var entries = new List<LanPartyExportService.ExportEntry>();
        foreach (var child in LibraryExportPanel.Children)
        {
            if (child is CheckBox { IsChecked: true, Tag: int appId } checkBox)
            {
                var title = checkBox.Content?.ToString() ?? $"App {appId}";
                entries.Add(new LanPartyExportService.ExportEntry(appId, title));
            }
        }

        return entries;
    }

    private async void Export_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var diagnostics = App.Services.GetRequiredService<DiagnosticBundleService>();
        var path = await diagnostics.ExportAsync();
        ExportPathBlock.Text = $"Exported to: {path}";
    }

    private async void DryRun_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!int.TryParse(DryRunAppIdBox.Text?.Trim(), out var appId) || appId <= 0)
        {
            ExportPathBlock.Text = "Enter a valid Steam App ID for dry run.";
            return;
        }

        var dryRun = App.Services.GetRequiredService<LaunchDryRunService>();
        var result = await dryRun.SimulateAsync(appId);
        var code = result.PredictedErrorCode ?? "none";
        ExportPathBlock.Text = result.WouldSucceed
            ? $"Dry run OK for app {appId} · {result.Steps.Count} steps · preview: {result.Preview?.PlatformLine}"
            : $"Dry run would fail with {code} · last step: {result.Steps.LastOrDefault()}";
    }

    private async void SeedExport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var export = App.Services.GetRequiredService<SeedContributionExportService>();
        var path = await export.ExportAsync();
        ExportPathBlock.Text = $"Seed contribution JSON: {path}";
    }

    private async void WorkshopImport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var importer = App.Services.GetService<WorkshopPresetImporter>();
        if (importer is null)
        {
            V2StatusBlock.Text = "Enable SPATIALLABS_ENABLE_V2=true to import workshop presets.";
            return;
        }

        var customUrl = WorkshopUrlBox.Text?.Trim();
        var count = string.IsNullOrWhiteSpace(customUrl)
            ? await importer.ImportAllowlistedSourcesAsync()
            : await importer.ImportFromUrlAsync(customUrl);
        V2StatusBlock.Text = $"Imported {count} preset profile(s).";
    }

    private async void LanExport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var export = App.Services.GetService<LanPartyExportService>();
        var codes = App.Services.GetService<ThreeDGoCodeService>();
        if (export is null || codes is null)
        {
            V2StatusBlock.Text = "Enable SPATIALLABS_ENABLE_V2=true for LAN export.";
            return;
        }

        var selected = GetSelectedExportEntries();
        if (selected.Count == 0)
        {
            V2StatusBlock.Text = "Select at least one library title for LAN export.";
            return;
        }

        var path = await export.ExportSessionAsync(selected);
        V2StatusBlock.Text = $"LAN export: {path} · session code {codes.GenerateCode()}";
    }

    private async void LanPresetExport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var export = App.Services.GetService<LanPresetExportService>();
        if (export is null)
        {
            V2StatusBlock.Text = "Enable SPATIALLABS_ENABLE_V2=true for LAN preset export.";
            return;
        }

        var selected = GetSelectedExportEntries();
        if (selected.Count == 0)
        {
            V2StatusBlock.Text = "Select at least one library title for LAN preset export.";
            return;
        }

        var entries = selected
            .Select(entry => new LanPresetExportService.ExportEntry(entry.AppId, entry.Title))
            .ToList();
        var path = await export.ExportAllowlistedPresetsAsync(entries);
        V2StatusBlock.Text = $"LAN preset allowlist: {path}";
    }

    private async void HybridSession_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var hybrid = App.Services.GetService<HybridSessionService>();
        if (hybrid is null)
        {
            V2StatusBlock.Text = "Enable SPATIALLABS_ENABLE_V2=true for hybrid co-op.";
            return;
        }

        var selected = GetSelectedExportEntries();
        if (selected.Count == 0)
        {
            V2StatusBlock.Text = "Select a host title for hybrid co-op session.";
            return;
        }

        var session = await hybrid.CreateSessionAsync(selected[0].AppId);
        V2StatusBlock.Text = $"Hybrid session {session.SessionCode} for app {session.HostAppId}.";
    }
}
