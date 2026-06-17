using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed record PlatformConnectionResult(
    bool Success,
    string Message,
    int LocalInstalledCount = 0,
    int OnlineCatalogCount = 0);

public sealed class PlatformConnectionService
{
    private readonly PlatformConnectionRepository _connections;
    private readonly SteamWebApiClient _steamWebApi;
    private readonly SteamVdfScanner _vdfScanner;
    private readonly EpicGogLibraryScanner _epicGogScanner;
    private readonly UbisoftConnectScanner _ubisoftScanner;

    public PlatformConnectionService(
        PlatformConnectionRepository connections,
        SteamWebApiClient steamWebApi,
        SteamVdfScanner vdfScanner,
        EpicGogLibraryScanner epicGogScanner,
        UbisoftConnectScanner ubisoftScanner)
    {
        _connections = connections;
        _steamWebApi = steamWebApi;
        _vdfScanner = vdfScanner;
        _epicGogScanner = epicGogScanner;
        _ubisoftScanner = ubisoftScanner;
    }

    public async Task<PlatformConnectionResult> ValidateSteamAsync(
        string? steamId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steamId) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new PlatformConnectionResult(false, "Enter your Steam ID64 and Web API key.");
        }

        try
        {
            var owned = await _steamWebApi.GetOwnedAppIdsAsync(apiKey, steamId, cancellationToken);
            var installed = _vdfScanner.ScanInstalledAppIds().Count;
            await _connections.SetSteamIdAsync(steamId, cancellationToken);
            await _connections.SetSteamApiKeyAsync(apiKey, cancellationToken);
            await _connections.SetSteamLastValidatedUtcAsync(DateTimeOffset.UtcNow, cancellationToken);
            return new PlatformConnectionResult(
                true,
                $"Connected — {owned.Count} owned, {installed} installed locally.",
                installed,
                owned.Count);
        }
        catch (Exception ex)
        {
            return new PlatformConnectionResult(false, $"Steam connection failed: {ex.Message}");
        }
    }

    public async Task<PlatformConnectionResult> ValidateEpicAsync(CancellationToken cancellationToken = default)
    {
        var path = await _connections.GetEpicManifestsPathAsync(cancellationToken);
        var scanner = string.IsNullOrWhiteSpace(path)
            ? _epicGogScanner
            : new EpicGogLibraryScanner(path, null);
        var games = scanner.ScanEpicInstalledGames();
        if (games.Count == 0 && string.IsNullOrWhiteSpace(path))
        {
            return new PlatformConnectionResult(
                false,
                "Epic launcher manifests not found. Install Epic Games Launcher or browse to the Manifests folder.",
                0,
                0);
        }

        return new PlatformConnectionResult(
            true,
            games.Count > 0
                ? $"Epic validated — {games.Count} installed locally."
                : "Epic path set but no games found. Install a game or check the folder.",
            games.Count,
            0);
    }

    public async Task<PlatformConnectionResult> ValidateGogAsync(CancellationToken cancellationToken = default)
    {
        var path = await _connections.GetGogGamesPathAsync(cancellationToken);
        var gogPath = string.IsNullOrWhiteSpace(path)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GOG Galaxy", "Games")
            : path;
        var scanner = new EpicGogLibraryScanner(null, gogPath);
        var games = scanner.ScanGogInstalledGames();
        if (games.Count == 0 && !Directory.Exists(gogPath))
        {
            return new PlatformConnectionResult(
                false,
                "GOG Galaxy games folder not found. Install GOG Galaxy or browse to your games folder.",
                0,
                0);
        }

        return new PlatformConnectionResult(
            true,
            games.Count > 0
                ? $"GOG validated — {games.Count} installed locally."
                : "GOG path set but no games found.",
            games.Count,
            0);
    }

    public async Task<PlatformConnectionResult> ValidateUbisoftAsync(CancellationToken cancellationToken = default)
    {
        var path = await _connections.GetUbisoftConfigPathAsync(cancellationToken);
        var scanner = string.IsNullOrWhiteSpace(path)
            ? _ubisoftScanner
            : new UbisoftConnectScanner(path);
        var games = scanner.ScanInstalledGames();
        if (games.Count == 0)
        {
            return new PlatformConnectionResult(
                false,
                "Ubisoft Connect not detected or no installed games found.",
                0,
                0);
        }

        return new PlatformConnectionResult(
            true,
            $"Ubisoft validated — {games.Count} installed locally.",
            games.Count,
            0);
    }

    public string GetDefaultEpicManifestsPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "EpicGamesLauncher", "Data", "Manifests");

    public string GetDefaultGogGamesPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "GOG Galaxy", "Games");

    public string GetDefaultUbisoftConfigPath()
        => UbisoftConnectScanner.DefaultConfigPath;
}
