using SpatialLabsOptimizer.Infrastructure.Library;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class LibrarySettingsViewModel
{
    public async Task LoadAsync()
    {
        SteamId = await _repository.GetSteamIdAsync() ?? "";
        EpicPath = await _repository.GetEpicManifestsPathAsync() ?? _connections.GetDefaultEpicManifestsPath();
        GogPath = await _repository.GetGogGamesPathAsync() ?? _connections.GetDefaultGogGamesPath();
        UbisoftPath = await _repository.GetUbisoftConfigPathAsync() ?? _connections.GetDefaultUbisoftConfigPath();
        var validated = await _repository.GetSteamLastValidatedUtcAsync();
        SteamStatus = validated.HasValue ? $"Last validated {validated.Value.ToLocalTime():g}" : "Not connected";
        await RefreshFoldersAsync();
        await RefreshStatsAsync();
    }

    public async Task RefreshFoldersAsync()
    {
        var folders = await _folders.GetFoldersAsync();
        FoldersList = folders;
        FolderStatus = folders.Count == 0
            ? "No custom folders configured."
            : $"{folders.Count} custom folder(s) watched.";
    }

    public async Task AddFolderAsync(string path)
    {
        await _folders.AddFolderAsync(path);
        await RunIndexAsync();
        await RefreshFoldersAsync();
        await RefreshStatsAsync();
    }

    public async Task RemoveFolderAsync(string path)
    {
        await _folders.RemoveFolderAsync(path);
        SelectedFolder = null;
        await RunIndexAsync();
        await RefreshFoldersAsync();
        await RefreshStatsAsync();
    }

    private async Task RemoveSelectedFolderAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolder))
        {
            FolderStatus = "Select a folder to remove.";
            return;
        }

        await RemoveFolderAsync(SelectedFolder);
    }

    public async Task RescanFoldersAsync()
    {
        await RunIndexAsync();
        FolderStatus = "Local folder scan complete.";
        await RefreshStatsAsync();
    }

    public async Task SaveEpicPathAsync(string path)
    {
        await _repository.SetEpicManifestsPathAsync(path);
        EpicPath = path;
    }

    public async Task SaveGogPathAsync(string path)
    {
        await _repository.SetGogGamesPathAsync(path);
        GogPath = path;
    }

    public async Task SaveUbisoftPathAsync(string path)
    {
        await _repository.SetUbisoftConfigPathAsync(path);
        UbisoftPath = path;
    }

    public async Task TestSteamConnectionAsync()
    {
        var result = await _connections.ValidateSteamAsync(SteamId, SteamApiKey);
        SteamStatus = result.Message;
        if (result.Success)
        {
            SteamApiKey = "";
            await RunIndexAsync();
            await RefreshStatsAsync();
        }
    }

    public async Task ValidateEpicConnectionAsync()
    {
        await _repository.SetEpicManifestsPathAsync(EpicPath);
        var result = await _connections.ValidateEpicAsync();
        EpicStatus = result.Message;
        if (result.Success)
        {
            await RunIndexAsync();
            await RefreshStatsAsync();
        }
    }

    public async Task ValidateGogConnectionAsync()
    {
        await _repository.SetGogGamesPathAsync(GogPath);
        var result = await _connections.ValidateGogAsync();
        GogStatus = result.Message;
        if (result.Success)
        {
            await RunIndexAsync();
            await RefreshStatsAsync();
        }
    }

    public async Task ValidateUbisoftConnectionAsync()
    {
        await _repository.SetUbisoftConfigPathAsync(UbisoftPath);
        var result = await _connections.ValidateUbisoftAsync();
        UbisoftStatus = result.Message;
        if (result.Success)
        {
            await RunIndexAsync();
            await RefreshStatsAsync();
        }
    }

    public async Task RefreshStatsAsync()
    {
        var stats = await _stats.GetStatsAsync();
        StatsSummary =
            $"Steam: {stats.SteamInstalledLocal} installed locally, {stats.SteamOwnedOnline} owned online, {stats.SteamCompatibilitySeed} in compatibility seed\n" +
            $"Epic: {stats.EpicInstalledLocal} installed (online catalog — coming soon)\n" +
            $"GOG: {stats.GogInstalledLocal} installed (online catalog — coming soon)\n" +
            $"Ubisoft: {stats.UbisoftInstalledLocal} installed (online catalog — coming soon)\n" +
            $"Custom folders: {stats.CustomLocalFolders} games\n" +
            $"Total in library: {stats.TotalInLibrary}";
    }

    private async Task RunIndexAsync()
    {
        await _indexer.IndexAsync();
    }

    public void OpenSteamKeyHelp()
    {
        _ = global::System.Diagnostics.Process.Start(new global::System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://steamcommunity.com/dev/apikey",
            UseShellExecute = true
        });
    }
}
