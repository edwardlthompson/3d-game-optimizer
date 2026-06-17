using System.Security.Cryptography;
using System.Text;

namespace SpatialLabsOptimizer.Infrastructure.Security;

public sealed class DpapiSecretStore
{
    private static readonly byte[] LegacyEntropy = Encoding.UTF8.GetBytes("3d-game-optimizer-v1");
    private readonly byte[] _entropy;

    public DpapiSecretStore()
        : this(DefaultEntropyPath())
    {
    }

    public DpapiSecretStore(string entropyFilePath)
    {
        _entropy = LoadOrCreateEntropy(entropyFilePath);
    }

    public string? Protect(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return null;
        }

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, _entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public string? Unprotect(string? protectedBase64)
    {
        if (string.IsNullOrWhiteSpace(protectedBase64))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(protectedBase64);
            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(bytes, _entropy, DataProtectionScope.CurrentUser));
        }
        catch (CryptographicException)
        {
            return TryLegacyUnprotect(protectedBase64);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? TryLegacyUnprotect(string protectedBase64)
    {
        try
        {
            var bytes = Convert.FromBase64String(protectedBase64);
            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(bytes, LegacyEntropy, DataProtectionScope.CurrentUser));
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string DefaultEntropyPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "3d-game-optimizer", ".dpapi-entropy");
    }

    private static byte[] LoadOrCreateEntropy(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(path))
        {
            var stored = Convert.FromBase64String(File.ReadAllText(path).Trim());
            if (stored.Length >= 16)
            {
                return stored;
            }
        }

        var entropy = RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(path, Convert.ToBase64String(entropy));
        return entropy;
    }
}
