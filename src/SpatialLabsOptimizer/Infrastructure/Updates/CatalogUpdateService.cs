using System.Security.Cryptography;
using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class CatalogUpdateService
{
    private readonly ExternalDataGateway _gateway;
    private readonly UserPreferencesService _prefs;
    private readonly CompatibilityRepository _compatibility;
    private readonly OperationProgressHub _progressHub;

    public CatalogUpdateService(
        ExternalDataGateway gateway,
        UserPreferencesService prefs,
        CompatibilityRepository compatibility,
        OperationProgressHub progressHub)
    {
        _gateway = gateway;
        _prefs = prefs;
        _compatibility = compatibility;
        _progressHub = progressHub;
    }

    public async Task<bool> IsCheckDueAsync(CancellationToken cancellationToken = default)
    {
        var interval = await _prefs.GetCatalogCheckIntervalAsync(cancellationToken);
        if (interval == UpdateCheckInterval.Off)
        {
            return false;
        }

        var last = await _prefs.GetLastCatalogCheckUtcAsync(cancellationToken);
        if (last is null)
        {
            return true;
        }

        var elapsed = DateTimeOffset.UtcNow - last.Value;
        return interval switch
        {
            UpdateCheckInterval.Startup => true,
            UpdateCheckInterval.Daily => elapsed >= TimeSpan.FromDays(1),
            UpdateCheckInterval.Weekly => elapsed >= TimeSpan.FromDays(7),
            _ => false
        };
    }

    public async Task<CatalogUpdateResult> CheckForUpdateAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        var interval = await _prefs.GetCatalogCheckIntervalAsync(cancellationToken);
        if (!force && interval == UpdateCheckInterval.Off)
        {
            return new CatalogUpdateResult(false, "Catalog updates are disabled.");
        }

        if (!force && !await IsCheckDueAsync(cancellationToken))
        {
            return new CatalogUpdateResult(false, "Catalog check not due yet.");
        }

        _progressHub.Publish(new OperationProgressReport(
            "catalog-update",
            Application.Progress.OperationCategory.Update,
            "Checking catalog",
            "Downloading catalog-v2.json…"));

        try
        {
            var jsonBytes = await _gateway.GetBytesAsync(
                CatalogUpdateUrls.DefaultCatalogJson,
                "catalog-v2",
                cancellationToken: cancellationToken);
            if (jsonBytes is null || jsonBytes.Length == 0)
            {
                return Fail("Catalog download returned no data.");
            }

            var hashText = await _gateway.GetStringAsync(
                CatalogUpdateUrls.DefaultCatalogHash,
                "catalog-v2-hash",
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(hashText))
            {
                var expected = hashText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                var actual = Convert.ToHexString(SHA256.HashData(jsonBytes));
                if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                {
                    return Fail("Catalog hash mismatch — keeping bundled catalog.");
                }
            }

            var cacheDir = JsonDataLoader.UserCatalogDirectory;
            Directory.CreateDirectory(cacheDir);
            var target = Path.Combine(cacheDir, "catalog-v2.json");
            await File.WriteAllBytesAsync(target, jsonBytes, cancellationToken);

            _compatibility.InvalidateCache();
            await _prefs.SetLastCatalogCheckUtcAsync(DateTimeOffset.UtcNow, cancellationToken);

            var count = CountGames(jsonBytes);
            var message = $"Catalog updated — {count} 3D titles";
            _progressHub.Publish(new OperationProgressReport(
                "catalog-update",
                Application.Progress.OperationCategory.Update,
                "Catalog updated",
                message,
                IsComplete: true));

            return new CatalogUpdateResult(true, message, count, "ok");
        }
        catch (Exception ex)
        {
            return Fail($"Catalog update failed: {ex.Message}");
        }
    }

    private CatalogUpdateResult Fail(string message)
    {
        _progressHub.Publish(new OperationProgressReport(
            "catalog-update",
            Application.Progress.OperationCategory.Update,
            "Catalog update",
            message,
            IsFailed: true,
            IsComplete: true));
        return new CatalogUpdateResult(false, message);
    }

    private static int CountGames(byte[] jsonBytes)
    {
        using var document = JsonDocument.Parse(jsonBytes);
        if (!document.RootElement.TryGetProperty("games", out var games) || games.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return games.GetArrayLength();
    }
}
