using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using SpatialLabsOptimizer.Infrastructure.Debug;
using SpatialLabsOptimizer.Infrastructure.Media;

namespace SpatialLabsOptimizer.Converters;

public sealed class CoverPathToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        var raw = value as string;
        var path = raw;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var pipe = raw.IndexOf('|');
            if (pipe > 0)
            {
                path = raw[..pipe];
            }
        }

        if (path is { Length: > 0 } coverPath && File.Exists(coverPath))
        {
            CoverArtDebugLog.LogConvert(coverPath, true, raw);
            return new BitmapImage(LocalFileUriHelper.ToFileUri(coverPath));
        }

        var bundled = Path.Combine(AppContext.BaseDirectory, "Assets", "placeholder-cover.png");
        if (File.Exists(bundled))
        {
            CoverArtDebugLog.LogConvert(bundled, true, raw);
            return new BitmapImage(LocalFileUriHelper.ToFileUri(bundled));
        }

        CoverArtDebugLog.LogConvert(path, false, raw);
        return null;
    }

    internal static Uri ToFileUri(string path) => LocalFileUriHelper.ToFileUri(path);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
