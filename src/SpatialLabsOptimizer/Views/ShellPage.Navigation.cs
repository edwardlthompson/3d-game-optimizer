using Microsoft.UI.Xaml.Controls;

namespace SpatialLabsOptimizer.Views;

public sealed partial class ShellPage
{
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
            "setup-wizard" => typeof(SetupWizardView),
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
            "setup-wizard" => _setupWizardViewModel,
            _ => null!
        };

        ContentFrame.Navigate(pageType, parameter);
        if (tag != "setup-wizard")
        {
            _ = _prefs.SetLastNavTagAsync(tag);
        }
    }
}
