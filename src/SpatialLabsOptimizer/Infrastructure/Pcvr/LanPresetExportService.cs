using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Privacy;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public sealed class LanPresetExportService
{
    private readonly JsonDataLoader _loader;
    private readonly PrivacyGuard _privacyGuard;

    public LanPresetExportService(JsonDataLoader loader, PrivacyGuard privacyGuard)
    {
        _loader = loader;
        _privacyGuard = privacyGuard;
    }

    public sealed record ExportEntry(int AppId, string Title);

    public async Task<string> ExportAllowlistedPresetsAsync(
        IReadOnlyList<ExportEntry> games,
        CancellationToken cancellationToken = default)
    {
        var manifest = await _loader.LoadAsync<PresetManifestDocument>(
            "presets/preset-manifest-v1.json",
            cancellationToken);
        var profiles = manifest?.UevrProfiles ?? [];

        var presets = new List<object>();
        foreach (var game in games)
        {
            var profile = profiles.FirstOrDefault(p => p.SteamAppIds.Contains(game.AppId));
            if (profile is null)
            {
                continue;
            }

            var allowlisted = string.IsNullOrWhiteSpace(profile.Url) ||
                (Uri.TryCreate(profile.Url, UriKind.Absolute, out var uri) &&
                 _privacyGuard.IsHostAllowed(uri.Host));

            presets.Add(new
            {
                appId = game.AppId,
                title = game.Title,
                presetId = profile.Id,
                sha256 = profile.Sha256,
                allowlisted,
                urlHost = string.IsNullOrWhiteSpace(profile.Url) || !Uri.TryCreate(profile.Url, UriKind.Absolute, out var parsed)
                    ? ""
                    : parsed.Host
            });
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "3d-game-optimizer", "exports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"lan-presets-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");
        var payload = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            allowlistOnly = true,
            presets
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload), cancellationToken);
        return path;
    }
}
