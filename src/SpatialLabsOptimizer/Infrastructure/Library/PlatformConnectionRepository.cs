using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Security;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed class PlatformConnectionRepository
{
    internal const string SteamIdKey = "steam_id";
    internal const string SteamApiKeyKey = "steam_api_key";
    internal const string EpicPathKey = "epic_manifests_path";
    internal const string GogPathKey = "gog_games_path";
    internal const string UbisoftPathKey = "ubisoft_config_path";
    internal const string SteamValidatedKey = "steam_last_validated_utc";

    private readonly SqliteSettingsStore _settings;
    private readonly DpapiSecretStore _secrets;

    public PlatformConnectionRepository(SqliteSettingsStore settings, DpapiSecretStore secrets)
    {
        _settings = settings;
        _secrets = secrets;
    }

    public async Task<string?> GetSteamIdAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _settings.GetAsync(SteamIdKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        var decrypted = _secrets.Unprotect(stored);
        return string.IsNullOrWhiteSpace(decrypted) ? stored : decrypted;
    }

    public async Task SetSteamIdAsync(string? steamId, CancellationToken cancellationToken = default)
    {
        var protectedValue = _secrets.Protect(steamId);
        await _settings.SetAsync(SteamIdKey, protectedValue ?? string.Empty, cancellationToken);
    }

    public async Task<string?> GetSteamApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _settings.GetAsync(SteamApiKeyKey, cancellationToken);
        return _secrets.Unprotect(stored);
    }

    public async Task SetSteamApiKeyAsync(string? apiKey, CancellationToken cancellationToken = default)
    {
        var protectedValue = _secrets.Protect(apiKey);
        await _settings.SetAsync(SteamApiKeyKey, protectedValue ?? string.Empty, cancellationToken);
    }

    public async Task<bool> HasSteamCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var id = await GetSteamIdAsync(cancellationToken);
        var key = await GetSteamApiKeyAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(key);
    }

    public async Task<DateTimeOffset?> GetSteamLastValidatedUtcAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync(SteamValidatedKey, cancellationToken);
        return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
    }

    public async Task SetSteamLastValidatedUtcAsync(DateTimeOffset timestamp, CancellationToken cancellationToken = default)
        => await _settings.SetAsync(SteamValidatedKey, timestamp.ToString("O"), cancellationToken);

    public async Task<string?> GetEpicManifestsPathAsync(CancellationToken cancellationToken = default)
        => await _settings.GetAsync(EpicPathKey, cancellationToken);

    public async Task SetEpicManifestsPathAsync(string? path, CancellationToken cancellationToken = default)
        => await _settings.SetAsync(EpicPathKey, path ?? string.Empty, cancellationToken);

    public async Task<string?> GetGogGamesPathAsync(CancellationToken cancellationToken = default)
        => await _settings.GetAsync(GogPathKey, cancellationToken);

    public async Task SetGogGamesPathAsync(string? path, CancellationToken cancellationToken = default)
        => await _settings.SetAsync(GogPathKey, path ?? string.Empty, cancellationToken);

    public async Task<string?> GetUbisoftConfigPathAsync(CancellationToken cancellationToken = default)
        => await _settings.GetAsync(UbisoftPathKey, cancellationToken);

    public async Task SetUbisoftConfigPathAsync(string? path, CancellationToken cancellationToken = default)
        => await _settings.SetAsync(UbisoftPathKey, path ?? string.Empty, cancellationToken);
}
