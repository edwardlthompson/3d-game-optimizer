using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class UpdateApplyService
{
    private readonly UpdateDownloadService _download;
    private readonly IEnumerable<IUpdateApplier> _appliers;
    private readonly OperationProgressHub _progressHub;
    private readonly UserPreferencesService _prefs;
    private int _applyInFlight;

    public UpdateApplyService(
        UpdateDownloadService download,
        IEnumerable<IUpdateApplier> appliers,
        OperationProgressHub progressHub,
        UserPreferencesService prefs)
    {
        _download = download;
        _appliers = appliers;
        _progressHub = progressHub;
        _prefs = prefs;
    }

    public async Task ApplyUpdateAsync(UpdateCheckResult update, CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _applyInFlight, 1, 0) != 0)
        {
            throw new InvalidOperationException("Update already in progress.");
        }

        try
        {
            _progressHub.Publish(new OperationProgressReport(
                "update-apply",
                Application.Progress.OperationCategory.Update,
                "Applying update",
                "Downloading…"));

            var stagedPath = await _download.ResolveStagedPathAsync(update, cancellationToken);
            await UpdateStagedArtifactVerifier.VerifyAsync(stagedPath, cancellationToken);

            if (update.DownloadArtifactType is null)
            {
                throw new InvalidOperationException("Unknown install artifact type.");
            }

            var applier = _appliers.FirstOrDefault(a => a.ArtifactType == update.DownloadArtifactType.Value)
                ?? throw new InvalidOperationException($"No applier for {update.DownloadArtifactType}.");

            await _prefs.SetUpdateRestartPendingAsync(true, cancellationToken);
            if (!string.IsNullOrWhiteSpace(update.LatestVersion))
            {
                await _prefs.SetUpdateAppliedVersionAsync(update.LatestVersion, cancellationToken);
            }

            _progressHub.Publish(new OperationProgressReport(
                "update-apply",
                Application.Progress.OperationCategory.Update,
                "Applying update",
                "Restarting…"));

            await applier.ApplyAsync(stagedPath, cancellationToken);
        }
        finally
        {
            Interlocked.Exchange(ref _applyInFlight, 0);
        }
    }
}
