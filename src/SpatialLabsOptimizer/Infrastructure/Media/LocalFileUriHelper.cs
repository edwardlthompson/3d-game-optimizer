namespace SpatialLabsOptimizer.Infrastructure.Media;

public static class LocalFileUriHelper
{
    public static Uri ToFileUri(string path, bool cacheBust = true)
    {
        var fullPath = Path.GetFullPath(path);
        if (cacheBust && File.Exists(fullPath))
        {
            var ticks = File.GetLastWriteTimeUtc(fullPath).Ticks;
            return new Uri("file:///" + fullPath.Replace('\\', '/') + "?v=" + ticks);
        }

        var uri = new Uri(fullPath);
        if (uri.IsAbsoluteUri && uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        return new Uri("file:///" + fullPath.Replace('\\', '/'));
    }
}
