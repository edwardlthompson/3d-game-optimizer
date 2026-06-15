using System.Text.Json;

namespace SpatialLabsOptimizer.Infrastructure.Artwork;

internal static class CoverArtDebugLog
{
    private static bool Enabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("SLO_COVER_ART_DEBUG"),
            "1",
            StringComparison.OrdinalIgnoreCase);

    public static void LogConvert(string? resolvedPath, bool success, string? raw)
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3d-game-optimizer",
                "logs");
            Directory.CreateDirectory(logDir);
            var payload = JsonSerializer.Serialize(new
            {
                ts = DateTimeOffset.UtcNow,
                eventType = "cover_convert",
                success,
                resolvedPath,
                raw
            });
            File.AppendAllText(Path.Combine(logDir, "debug-2ca1ae.log"), payload + Environment.NewLine);
        }
        catch
        {
            // Debug instrumentation must not affect UI conversion.
        }
    }
}
