using Microsoft.Win32;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public interface IPackageInstallProbe
{
    bool IsPackaged();
}

public interface IMsiInstallProbe
{
    bool IsMsiInstalled();
}

public sealed class DefaultPackageInstallProbe : IPackageInstallProbe
{
    public bool IsPackaged()
    {
        try
        {
            return Windows.ApplicationModel.Package.Current is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

public sealed class DefaultMsiInstallProbe : IMsiInstallProbe
{
    public const string ProductUpgradeCode = "A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D";

    public bool IsMsiInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (key is null)
            {
                return false;
            }

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using var subKey = key.OpenSubKey(subKeyName);
                var upgradeCode = subKey?.GetValue("UpgradeCode") as string;
                var displayName = subKey?.GetValue("DisplayName") as string;
                if (string.Equals(upgradeCode, ProductUpgradeCode, StringComparison.OrdinalIgnoreCase) ||
                    (displayName?.Contains("3D Game Optimizer", StringComparison.OrdinalIgnoreCase) == true &&
                     displayName.Contains("SpatialLabs", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            // Registry unavailable.
        }

        return false;
    }
}

public sealed class InstallArtifactDetector
{
    private readonly IPackageInstallProbe _packageProbe;
    private readonly IMsiInstallProbe _msiProbe;

    public InstallArtifactDetector(
        IPackageInstallProbe? packageProbe = null,
        IMsiInstallProbe? msiProbe = null)
    {
        _packageProbe = packageProbe ?? new DefaultPackageInstallProbe();
        _msiProbe = msiProbe ?? new DefaultMsiInstallProbe();
    }

    public InstallArtifactType Detect()
    {
        if (_packageProbe.IsPackaged())
        {
            return InstallArtifactType.Msix;
        }

        if (_msiProbe.IsMsiInstalled())
        {
            return InstallArtifactType.Msi;
        }

        return InstallArtifactType.Zip;
    }

    public static string GetExtension(InstallArtifactType type) => type switch
    {
        InstallArtifactType.Msix => ".msix",
        InstallArtifactType.Msi => ".msi",
        _ => ".zip"
    };

    public static bool MatchesExtension(string assetName, InstallArtifactType type)
    {
        var ext = GetExtension(type);
        return assetName.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ||
               (type == InstallArtifactType.Msix &&
                assetName.EndsWith(".msixbundle", StringComparison.OrdinalIgnoreCase));
    }
}
