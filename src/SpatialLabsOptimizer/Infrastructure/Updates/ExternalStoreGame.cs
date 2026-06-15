namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed record ExternalStoreGame(
    string Store,
    string ExternalId,
    int StableAppId,
    string Title,
    string? InstallDir = null,
    string? LaunchExe = null);

public static class ExternalStoreIdMapper
{
    public static int StableAppId(string store, string externalId)
    {
        var payload = $"{store}:{externalId}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload));
        return Math.Abs(BitConverter.ToInt32(hash, 0));
    }
}
