using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class Global3DSettingsViewModel
{
    private readonly SessionProfileService? _sessionProfiles;
    private readonly StreamerHotkeyService? _streamerHotkey;
    private readonly StreamFriendlyProfileService? _streamFriendly;

    private bool _sessionToolsVisible;
    private string _streamerHotkeyText = string.Empty;
    private string _streamFriendlyText = string.Empty;
    private string _sessionProfileName = string.Empty;
    private IReadOnlyList<string> _sessionProfileNames = [];
    private string _sessionProfileStatus = string.Empty;

    public bool SessionToolsVisible
    {
        get => _sessionToolsVisible;
        private set => SetProperty(ref _sessionToolsVisible, value);
    }

    public string StreamerHotkeyText
    {
        get => _streamerHotkeyText;
        private set => SetProperty(ref _streamerHotkeyText, value);
    }

    public string StreamFriendlyText
    {
        get => _streamFriendlyText;
        private set => SetProperty(ref _streamFriendlyText, value);
    }

    public string SessionProfileName
    {
        get => _sessionProfileName;
        set => SetProperty(ref _sessionProfileName, value);
    }

    public IReadOnlyList<string> SessionProfileNames
    {
        get => _sessionProfileNames;
        private set => SetProperty(ref _sessionProfileNames, value);
    }

    public string SessionProfileStatus
    {
        get => _sessionProfileStatus;
        private set => SetProperty(ref _sessionProfileStatus, value);
    }

    private async Task LoadSessionProfilesSectionAsync()
    {
        if (!FeatureFlags.V11Enabled)
        {
            return;
        }

        SessionToolsVisible = true;
        StreamerHotkeyText = _streamerHotkey is null
            ? "Streamer hotkey service unavailable."
            : $"Suggested streamer hotkey: {_streamerHotkey.Toggle3DHotkey} (register in your broadcast overlay app).";

        if (_streamFriendly is not null)
        {
            StreamFriendlyText = string.Join(
                Environment.NewLine + Environment.NewLine,
                _streamFriendly.GetBundles().Select(_streamFriendly.FormatBundleForDisplay));
        }

        await RefreshSessionProfilesAsync();
    }

    private async Task RefreshSessionProfilesAsync()
    {
        if (_sessionProfiles is null)
        {
            SessionProfileStatus = "Session profiles require v1.1 feature flag.";
            return;
        }

        var names = await _sessionProfiles.ListProfileNamesAsync();
        SessionProfileNames = names;
        if (names.Count == 0)
        {
            SessionProfileStatus = "No saved session profiles yet.";
            return;
        }

        var lines = new List<string>();
        foreach (var name in names)
        {
            var savedAt = await _sessionProfiles.GetProfileSavedAtAsync(name);
            lines.Add(savedAt.HasValue
                ? $"{name} — saved {savedAt.Value:yyyy-MM-dd HH:mm}"
                : name);
        }

        SessionProfileStatus = string.Join(Environment.NewLine, lines);
    }

    private async Task SaveSessionProfileAsync()
    {
        if (_sessionProfiles is null)
        {
            SessionProfileStatus = "Session profiles require v1.1 feature flag.";
            return;
        }

        var name = SessionProfileName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SessionProfileStatus = "Enter a profile name.";
            return;
        }

        await _sessionProfiles.SaveProfileAsync(name, new SessionProfileData
        {
            Name = name,
            Depth = Depth,
            Convergence = Convergence,
            Theme = Theme
        });
        await RefreshSessionProfilesAsync();
    }

    private async Task LoadSessionProfileAsync()
    {
        if (_sessionProfiles is null || string.IsNullOrWhiteSpace(SessionProfileName))
        {
            SessionProfileStatus = "Select a profile to load.";
            return;
        }

        var profile = await _sessionProfiles.LoadProfileAsync(SessionProfileName);
        if (profile is null)
        {
            SessionProfileStatus = "Profile not found.";
            return;
        }

        Depth = profile.Depth;
        Convergence = profile.Convergence;
        await SetThemeAsync(profile.Theme);
        SessionProfileStatus = $"Loaded profile \"{SessionProfileName}\".";
    }
}
