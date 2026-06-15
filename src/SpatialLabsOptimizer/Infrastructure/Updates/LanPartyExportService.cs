using System.Text.Json;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class LanPartyExportService
{
    public sealed record ExportEntry(int AppId, string Title);

    public async Task<string> ExportSessionAsync(
        IReadOnlyList<ExportEntry> games,
        CancellationToken cancellationToken = default)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "3d-game-optimizer", "exports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"lan-party-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");
        var payload = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            games = games.Select(g => new { appId = g.AppId, title = g.Title }).ToList()
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload), cancellationToken);
        return path;
    }

    public Task<string> ExportSessionAsync(IReadOnlyList<int> appIds, CancellationToken cancellationToken = default)
        => ExportSessionAsync(
            appIds.Select(id => new ExportEntry(id, $"App {id}")).ToList(),
            cancellationToken);
}
