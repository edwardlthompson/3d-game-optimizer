using System.Management;

namespace SpatialLabsOptimizer.Infrastructure.Displays;

public sealed record DisplayEdidSnapshot(string DeviceId, string EdidSignature, string FriendlyName);

public interface IDisplayEdidProbe
{
    IReadOnlyList<DisplayEdidSnapshot> GetCurrentSnapshots();
}

public sealed class WmiDisplayEdidProbe : IDisplayEdidProbe
{
    public static Func<IReadOnlyList<DisplayEdidSnapshot>>? TestSnapshotProvider { get; set; }

    public IReadOnlyList<DisplayEdidSnapshot> GetCurrentSnapshots()
    {
        if (TestSnapshotProvider is not null)
        {
            return TestSnapshotProvider();
        }

        return QueryWmiMonitors();
    }

    private static IReadOnlyList<DisplayEdidSnapshot> QueryWmiMonitors()
    {
        var list = new List<DisplayEdidSnapshot>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, PNPDeviceID, Name FROM Win32_DesktopMonitor");
            foreach (var obj in searcher.Get())
            {
                if (obj is not ManagementObject monitor)
                {
                    continue;
                }

                using (monitor)
                {
                    var pnp = monitor["PNPDeviceID"]?.ToString()?.Trim() ?? "";
                    var name = monitor["Name"]?.ToString()?.Trim() ?? "Display";
                    if (string.IsNullOrWhiteSpace(pnp) ||
                        name.Contains("Default Monitor", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var deviceId = monitor["DeviceID"]?.ToString()?.Trim() ?? pnp;
                    var signature = BuildEdidSignature(pnp, name);
                    list.Add(new DisplayEdidSnapshot(deviceId, signature, name));
                }
            }
        }
        catch (ManagementException)
        {
            return Array.Empty<DisplayEdidSnapshot>();
        }

        if (list.Count == 0)
        {
            list.Add(new DisplayEdidSnapshot("primary", "generic-manual", "Primary display"));
        }

        return list;
    }

    public static string BuildEdidSignature(string pnpDeviceId, string friendlyName)
    {
        var vendor = ExtractPnpToken(pnpDeviceId, "VEN_");
        var product = ExtractPnpToken(pnpDeviceId, "PROD_");
        if (!string.IsNullOrWhiteSpace(vendor) && !string.IsNullOrWhiteSpace(product))
        {
            return $"{vendor}:{product}".ToLowerInvariant();
        }

        return friendlyName.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant();
    }

    private static string ExtractPnpToken(string pnpDeviceId, string prefix)
    {
        var index = pnpDeviceId.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return "";
        }

        var start = index + prefix.Length;
        var end = pnpDeviceId.IndexOf('&', start);
        if (end < 0)
        {
            end = pnpDeviceId.Length;
        }

        return pnpDeviceId[start..end];
    }
}
