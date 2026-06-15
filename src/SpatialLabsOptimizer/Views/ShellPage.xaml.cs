using System.Diagnostics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Responsive;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.ViewModels;
using Windows.System;
using Windows.UI.Core;

namespace SpatialLabsOptimizer.Views;

public sealed partial class ShellPage : Page
{
    public static ShellPage? Current { get; private set; }

    private readonly ResponsiveStateService _responsive;
    private readonly GameLibraryViewModel _libraryViewModel;
    private readonly SetupWizardViewModel _wizardViewModel;

    public ShellViewModel ViewModel { get; }

    public ShellPage(
        ShellViewModel viewModel,
        ResponsiveStateService responsive,
        GameLibraryViewModel libraryViewModel,
        SetupWizardViewModel wizardViewModel)
    {
        ViewModel = viewModel;
        _responsive = responsive;
        _libraryViewModel = libraryViewModel;
        _wizardViewModel = wizardViewModel;
        InitializeComponent();
        Loaded += ShellPage_Loaded;
        Unloaded += (_, _) => Current = null;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.PendingSetupWizardRerun) && ViewModel.PendingSetupWizardRerun)
        {
            NavigateToTag("wizard");
            ViewModel.ClearSetupWizardRerunRequest();
        }
    }

    private async void ShellPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Current = this;
        if (App.Current is App app && app.PrimaryWindow is not null)
        {
            _responsive.AttachToWindow(app.PrimaryWindow);
        }

        ViewModel.StartDisplayMonitoring();
        await ViewModel.InitializeAsync();
        NavView.SelectedItem = NavView.MenuItems[0];
        ContentFrame.Navigate(typeof(GameLibraryView), _libraryViewModel);

        if (App.PendingProtocolAppId is int protocolAppId)
        {
            App.PendingProtocolAppId = null;
            NavigateToTag("library");
            await _libraryViewModel.PlayByAppIdAsync(protocolAppId);
        }
    }

    public void NavigateToTag(string tag)
    {
        foreach (NavigationViewItem item in NavView.MenuItems)
        {
            if (item.Tag is string itemTag && itemTag == tag)
            {
                NavView.SelectedItem = item;
                break;
            }
        }

        NavigateContent(tag);
    }

    public async Task ExecuteCommandAsync(string commandId)
    {
        switch (commandId)
        {
            case "setup-wizard":
                NavigateToTag("wizard");
                break;
            case "play-3d":
                NavigateToTag("library");
                _libraryViewModel.PlayCommand.Execute(null);
                break;
            case "play-vr":
                NavigateToTag("library");
                _libraryViewModel.PlayVrCommand.Execute(null);
                break;
            case "refresh-metadata":
            case "rescan-library":
                NavigateToTag("library");
                await _libraryViewModel.LoadAsync();
                ViewModel.Status = "Library re-indexed.";
                break;
            case "cache-presets":
                await CacheTopPresetsAsync();
                break;
            case "open-logs":
                OpenLogsFolder();
                break;
            case "toggle-safe-launch":
                await ToggleSafeLaunchAsync();
                break;
            case "safe-launch":
                NavigateToTag("settings");
                break;
            case "diagnostic-bundle":
                NavigateToTag("troubleshoot");
                break;
            case "command-palette":
                NavigateToTag("commands");
                break;
        }
    }

    private async Task CacheTopPresetsAsync()
    {
        var presets = App.Services.GetRequiredService<PresetCacheService>();
        var hub = App.Services.GetRequiredService<OperationProgressHub>();
        ViewModel.Status = "Caching top presets…";
        await presets.BulkCacheTopPresetsAsync(50, hub);
        ViewModel.Status = "Top presets cached.";
    }

    private static void OpenLogsFolder()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer", "logs");
        Directory.CreateDirectory(logDir);
        Process.Start(new ProcessStartInfo
        {
            FileName = logDir,
            UseShellExecute = true
        });
    }

    private async Task ToggleSafeLaunchAsync()
    {
        var prefs = App.Services.GetRequiredService<UserPreferencesService>();
        var enabled = await prefs.GetSafeLaunchAsync();
        await prefs.SetSafeLaunchAsync(!enabled);
        ViewModel.Status = !enabled ? "Safe launch enabled." : "Safe launch disabled.";
    }

    private void DisplayChangeRerun_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => ViewModel.RequestSetupWizardRerun();

    private void DisplayChangeInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        => ViewModel.AcknowledgeDisplayChange();

    private void UpdateAvailable_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => NavigateToTag("about");

    private void NavView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (FeatureFlags.V11Enabled &&
            App.Services.GetService<StreamerHotkeyService>() is { } hotkeys &&
            TryHandleStreamerHotkey(e, hotkeys))
        {
            e.Handled = true;
            return;
        }

        if (e.Key is VirtualKey.K &&
            InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
                .HasFlag(CoreVirtualKeyStates.Down))
        {
            NavigateToTag("commands");
            e.Handled = true;
        }
    }

    private bool TryHandleStreamerHotkey(KeyRoutedEventArgs e, StreamerHotkeyService hotkeys)
    {
        var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(CoreVirtualKeyStates.Down);
        var shift = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
            .HasFlag(CoreVirtualKeyStates.Down);
        var alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu)
            .HasFlag(CoreVirtualKeyStates.Down);

        return hotkeys.TryHandle(e.Key, ctrl, shift, alt, new StreamerHotkeyHandler
        {
            ExecuteCommandAsync = ExecuteCommandAsync,
            NavigateToSettings = () => NavigateToTag("settings"),
            SetPreferredOutputAsync = SetPreferredOutputFromHotkeyAsync
        });
    }

    private async Task SetPreferredOutputFromHotkeyAsync(string output)
    {
        if (_libraryViewModel.SelectedGame is null)
        {
            ViewModel.Status = "Select a game in the library first.";
            return;
        }

        _libraryViewModel.PreferredOutput = output;
        _libraryViewModel.SaveOutputCommand.Execute(null);
        ViewModel.Status = $"Preferred output set to {output} for selected game.";
        await Task.CompletedTask;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        NavigateContent(tag);
    }

    private void NavigateContent(string tag)
    {
        var pageType = tag switch
        {
            "library" => typeof(GameLibraryView),
            "wizard" => typeof(SetupWizardView),
            "settings" => typeof(Global3DSettingsView),
            "library-settings" => typeof(LibrarySettingsView),
            "troubleshoot" => typeof(TroubleshootingView),
            "glossary" => typeof(GlossaryView),
            "about" => typeof(AboutView),
            "commands" => typeof(CommandPaletteView),
            _ => typeof(GameLibraryView)
        };

        var parameter = tag switch
        {
            "library" => (object)_libraryViewModel,
            "wizard" => _wizardViewModel,
            _ => null!
        };

        ContentFrame.Navigate(pageType, parameter);
    }
}
