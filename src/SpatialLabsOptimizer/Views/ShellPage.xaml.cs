using System.Diagnostics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
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
    private readonly Global3DSettingsViewModel _settingsViewModel;
    private readonly LibrarySettingsViewModel _librarySettingsViewModel;
    private readonly AboutViewModel _aboutViewModel;
    private readonly GlossaryViewModel _glossaryViewModel;
    private readonly TroubleshootingViewModel _troubleshootingViewModel;
    private readonly CommandPaletteViewModel _commandPaletteViewModel;
    private readonly PresetCacheService _presets;
    private readonly OperationProgressHub _progressHub;
    private readonly UserPreferencesService _prefs;
    private readonly StreamerHotkeyService? _streamerHotkey;

    public ShellViewModel ViewModel { get; }

    public ShellPage(
        ShellViewModel viewModel,
        ResponsiveStateService responsive,
        GameLibraryViewModel libraryViewModel,
        Global3DSettingsViewModel settingsViewModel,
        LibrarySettingsViewModel librarySettingsViewModel,
        AboutViewModel aboutViewModel,
        GlossaryViewModel glossaryViewModel,
        TroubleshootingViewModel troubleshootingViewModel,
        CommandPaletteViewModel commandPaletteViewModel,
        PresetCacheService presets,
        OperationProgressHub progressHub,
        UserPreferencesService prefs,
        StreamerHotkeyService? streamerHotkey = null)
    {
        ViewModel = viewModel;
        _responsive = responsive;
        _libraryViewModel = libraryViewModel;
        _settingsViewModel = settingsViewModel;
        _librarySettingsViewModel = librarySettingsViewModel;
        _aboutViewModel = aboutViewModel;
        _glossaryViewModel = glossaryViewModel;
        _troubleshootingViewModel = troubleshootingViewModel;
        _commandPaletteViewModel = commandPaletteViewModel;
        _presets = presets;
        _progressHub = progressHub;
        _prefs = prefs;
        _streamerHotkey = streamerHotkey;
        InitializeComponent();
        Loaded += ShellPage_Loaded;
        Unloaded += (_, _) => Current = null;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.PendingToolchainSettings) && ViewModel.PendingToolchainSettings)
        {
            _settingsViewModel.ExpandToolchain = true;
            NavigateToTag("settings");
            ViewModel.ClearToolchainSettingsRequest();
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

        var navTag = "library";
        if (!await ViewModel.IsSetupCompleteAsync())
        {
            _settingsViewModel.ExpandToolchain = true;
            navTag = "settings";
        }
        else
        {
            navTag = await _prefs.GetLastNavTagAsync() ?? "library";
        }

        NavigateToTag(navTag);

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

    private void DisplayChangeRerun_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => ViewModel.RequestToolchainSettings();

    private void DisplayChangeInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        => ViewModel.AcknowledgeDisplayChange();

    private void UpdateAvailable_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => NavigateToTag("about");

    private void NavView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (FeatureFlags.V11Enabled &&
            _streamerHotkey is { } hotkeys &&
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
            "settings" => _settingsViewModel,
            "library-settings" => _librarySettingsViewModel,
            "troubleshoot" => _troubleshootingViewModel,
            "glossary" => _glossaryViewModel,
            "about" => _aboutViewModel,
            "commands" => _commandPaletteViewModel,
            _ => null!
        };

        ContentFrame.Navigate(pageType, parameter);
        _ = _prefs.SetLastNavTagAsync(tag);
    }
}
