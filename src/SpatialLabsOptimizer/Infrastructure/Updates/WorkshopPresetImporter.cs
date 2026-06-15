using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class WorkshopPresetImporter
{
    private readonly JsonDataLoader _loader;
    private readonly PresetCacheService _presets;
    private readonly ExternalDataGateway _gateway;
    private readonly Privacy.PrivacyGuard _privacyGuard;

    public WorkshopPresetImporter(
        JsonDataLoader loader,
        PresetCacheService presets,
        ExternalDataGateway gateway,
        Privacy.PrivacyGuard privacyGuard)
    {
        _loader = loader;
        _presets = presets;
        _gateway = gateway;
        _privacyGuard = privacyGuard;
    }

    public async Task<int> ImportAllowlistedSourcesAsync(CancellationToken cancellationToken = default)
    {
        var sources = await _loader.LoadAsync<WorkshopSourcesDocument>("presets/workshop-sources-v1.json", cancellationToken);
        var imported = 0;
        foreach (var url in sources?.AllowlistedUrls ?? [])
        {
            if (!IsAllowlistedUrl(url))
            {
                continue;
            }

            imported += await ImportPresetBundleFromUrlAsync(url, cancellationToken);
        }

        imported += await ImportFromManifestAsync(cancellationToken);
        return imported;
    }

    public async Task<int> ImportFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!IsAllowlistedUrl(url))
        {
            return 0;
        }

        return await ImportPresetBundleFromUrlAsync(url, cancellationToken);
    }

    public async Task<int> ImportFromManifestAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await _loader.LoadAsync<PresetManifestDoc>("presets/preset-manifest-v1.json", cancellationToken);
        var count = manifest?.UevrProfiles?.Count ?? 0;
        if (count > 0 && manifest!.UevrProfiles![0].SteamAppIds.Count > 0)
        {
            await _presets.CachePresetAsync(manifest.UevrProfiles[0].SteamAppIds[0], cancellationToken);
        }

        return count;
    }

    private async Task<int> ImportPresetBundleFromUrlAsync(string url, CancellationToken cancellationToken)
    {
        var json = await _gateway.GetStringAsync(url, "workshop-import", cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return 0;
        }

        var manifest = JsonSerializer.Deserialize<PresetManifestDoc>(json);
        var profiles = manifest?.UevrProfiles ?? [];
        foreach (var profile in profiles)
        {
            var appId = profile.SteamAppIds.FirstOrDefault();
            if (appId > 0)
            {
                await _presets.CachePresetAsync(appId, cancellationToken);
            }
        }

        return profiles.Count;
    }

    private bool IsAllowlistedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return _privacyGuard.IsHostAllowed(uri.Host);
    }

    private sealed class WorkshopSourcesDocument
    {
        public List<string>? AllowlistedUrls { get; set; }
    }

    private sealed class PresetManifestDoc
    {
        public List<PresetProfileDoc>? UevrProfiles { get; set; }
    }

    private sealed class PresetProfileDoc
    {
        public List<int> SteamAppIds { get; set; } = [];
    }
}
