using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Install;

public sealed partial class SilentInstallOrchestrator
{
    private string BuildHelperArgs(ToolManifestEntry tool)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append("install --tool-id ").Append(tool.Id).Append(" --silent \"").Append(tool.SilentArgs).Append('"');
        if (!string.IsNullOrWhiteSpace(tool.DownloadUrl))
        {
            builder.Append(" --url \"").Append(tool.DownloadUrl).Append('"');
        }

        if (!string.IsNullOrWhiteSpace(tool.BundledPackage))
        {
            var bundledPath = Path.GetFullPath(Path.Combine(_loader.DataRoot, tool.BundledPackage));
            builder.Append(" --local-package \"").Append(bundledPath).Append('"');
        }

        if (!string.IsNullOrWhiteSpace(tool.Sha256))
        {
            builder.Append(" --sha256 ").Append(tool.Sha256);
        }

        return builder.ToString();
    }
}
