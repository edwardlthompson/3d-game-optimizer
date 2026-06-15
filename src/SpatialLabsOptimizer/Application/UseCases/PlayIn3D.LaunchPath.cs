using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

namespace SpatialLabsOptimizer.Application.UseCases;

public sealed partial class PlayIn3D
{
    private async Task<(bool Success, string? ErrorCode)> LaunchWithConfigsAsync(
        int appId,
        ResolvedGameLaunchPlan plan,
        DisplayProfile profile,
        Action<string, bool, bool, string?> publish,
        CancellationToken cancellationToken)
    {
        publish("Checking trainer and mod manager compatibility…", false, false, null);
        var trainerCoexistence = await _preferences.GetTrainerCoexistenceAsync(cancellationToken);
        var modManagerCoexistence = await _preferences.GetModManagerCoexistenceAsync(cancellationToken);
        var (shouldBlock, launchContextBase) = _coexistence.Evaluate(trainerCoexistence, modManagerCoexistence);
        if (shouldBlock)
        {
            publish(_errors.Get("3DGO-0004").Message, false, true, _errors.Get("3DGO-0004").Message);
            await _audit.LogAsync(
                appId,
                plan.Title,
                plan.Platform,
                false,
                "3DGO-0004",
                string.Join(", ", launchContextBase.DetectedTools),
                cancellationToken);
            return (false, "3DGO-0004");
        }

        var displayTarget = await _displayHandoff.PrepareAsync(cancellationToken);
        var launchContext = launchContextBase with { DisplayTarget = displayTarget };
        publish(LaunchDisplayHandoffService.FormatHandoffMessage(displayTarget), false, false, null);

        publish("Ensuring preset cached…", false, false, null);
        if (!await _presetCache.HasPresetAsync(appId, cancellationToken))
        {
            await _presetCache.CachePresetAsync(appId, cancellationToken);
        }

        publish("Applying 3D configs…", false, false, null);
        await ApplyPerGameConfigsAsync(plan, cancellationToken);

        publish("Applying display optimal defaults…", false, false, null);
        await _defaults.ApplyForProfileAsync(profile.RecommendedProfileId, cancellationToken);

        publish("Saving rollback snapshot…", false, false, null);
        var snapshotPath = await _snapshots.SnapshotAsync(appId, cancellationToken);

        publish("Starting game…", false, false, null);
        if (launchContext.IsGameFirst)
        {
            var gameFirstLaunched = await _gameFirst.LaunchAsync(plan, cancellationToken);
            if (!gameFirstLaunched)
            {
                await _snapshots.RollbackAsync(snapshotPath, cancellationToken);
                publish("Restoring previous settings…", false, true, _errors.Get("3DGO-0005").Message);
                await _audit.LogAsync(
                    appId,
                    plan.Title,
                    plan.Platform,
                    false,
                    "3DGO-0005",
                    "game-first",
                    cancellationToken);
                return (false, "3DGO-0005");
            }

            var displayNote = displayTarget?.FriendlyName ?? "primary";
            publish("Launched successfully (game-first)", true, false, null);
            await _audit.LogAsync(
                appId,
                plan.Title,
                plan.Platform,
                true,
                null,
                $"game-first: {string.Join(", ", launchContext.DetectedTools)}; display={displayNote}",
                cancellationToken);
            await _libraryIntel.RecordLaunchAsync(appId, plan.Title, true, null, cancellationToken);
            return (true, null);
        }

        var (launched, usedPlatform, fallbackNote) = await _fallback.LaunchWithFallbackAsync(plan, launchContext, cancellationToken);
        if (!launched)
        {
            await _snapshots.RollbackAsync(snapshotPath, cancellationToken);
            publish("Restoring previous settings…", false, true, _errors.Get("3DGO-0005").Message);
            var displayNote = displayTarget?.DeviceId ?? "primary";
            await _audit.LogAsync(
                appId,
                plan.Title,
                usedPlatform,
                false,
                "3DGO-0005",
                $"{fallbackNote}; display={displayNote}",
                cancellationToken);
            return (false, "3DGO-0005");
        }

        var successNote = displayTarget is null
            ? fallbackNote
            : $"{fallbackNote}; display={displayTarget.FriendlyName}";
        publish("Launched successfully", true, false, null);
        await _audit.LogAsync(appId, plan.Title, usedPlatform, true, null, successNote, cancellationToken);
        await _libraryIntel.RecordLaunchAsync(appId, plan.Title, true, null, cancellationToken);
        return (true, null);
    }

    private async Task ApplyPerGameConfigsAsync(ResolvedGameLaunchPlan plan, CancellationToken cancellationToken)
    {
        var install = _installPaths.Resolve(plan.SteamAppId);
        if (install is null)
        {
            return;
        }

        switch (plan.Platform)
        {
            case LaunchPlatform.ReShade:
                await _configWriter.ApplyReShadeConfigAsync(
                    install.InstallDir,
                    plan.Depth,
                    plan.Convergence,
                    cancellationToken);
                break;
            case LaunchPlatform.Uevr:
            {
                var presetPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "3d-game-optimizer",
                    "presets",
                    $"{plan.SteamAppId}.json");
                await _configWriter.ApplyUevrConfigAsync(presetPath, plan.Depth, cancellationToken);
                break;
            }
        }
    }
}
