namespace SpatialLabsOptimizer.Infrastructure.Updates;

public static class UpdateStagedArtifactVerifier
{
    public static async Task VerifyAsync(string stagedPath, CancellationToken cancellationToken = default)
    {
        var hashPath = stagedPath + ".sha256";
        if (!File.Exists(hashPath))
        {
            return;
        }

        var expected = (await File.ReadAllTextAsync(hashPath, cancellationToken)).Trim();
        var bytes = await File.ReadAllBytesAsync(stagedPath, cancellationToken);
        var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("3DGO-0103: Installer hash mismatch.");
        }
    }
}
