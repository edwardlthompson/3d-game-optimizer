using System.Management;

namespace SpatialLabsOptimizer.Infrastructure.Displays;

public sealed record DisplayEdidSnapshot(string DeviceId, string EdidSignature, string FriendlyName);

public interface IDisplayEdidProbe
{
    IReadOnlyList<DisplayEdidSnapshot> GetCurrentSnapshots();
}

public sealed class WmiDisplayEdidProbe : IDisplayEdidProbe
{
    private const int WmiTimeoutMs = 8000;

    public static Func<IReadOnlyList<DisplayEdidSnapshot>>? TestSnapshotProvider { get; set; }

    public IReadOnlyList<DisplayEdidSnapshot> GetCurrentSnapshots()
    {
        if (TestSnapshotProvider is not null)
        {
            return TestSnapshotProvider();
        }

        var list = new List<DisplayEdidSnapshot>();
        list.AddRange(QueryDesktopMonitors());
        list.AddRange(QueryPnPMonitors());

        if (list.Count == 0)
        {
            list.Add(new DisplayEdidSnapshot("primary", "generic-manual", "Primary display"));
        }

        return Deduplicate(list);
    }

    private static IEnumerable<DisplayEdidSnapshot> QueryDesktopMonitors()
    {
        foreach (var obj in Search("SELECT DeviceID, PNPDeviceID, Name FROM Win32_DesktopMonitor"))
        {
            using (obj)
            {
                var pnp = obj["PNPDeviceID"]?.ToString()?.Trim() ?? "";
                var name = obj["Name"]?.ToString()?.Trim() ?? "Display";
                if (string.IsNullOrWhiteSpace(pnp) ||
                    name.Contains("Default Monitor", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var deviceId = obj["DeviceID"]?.ToString()?.Trim() ?? pnp;
                yield return new DisplayEdidSnapshot(deviceId, BuildEdidSignature(pnp, name), name);
            }
        }
    }

    private static IEnumerable<DisplayEdidSnapshot> QueryPnPMonitors()
    {
        foreach (var obj in Search(
                     "SELECT DeviceID, PNPDeviceID, Name, Caption FROM Win32_PnPEntity WHERE PNPClass='Monitor'"))
        {
            using (obj)
            {
                var pnp = obj["PNPDeviceID"]?.ToString()?.Trim() ?? "";
                var name = obj["Name"]?.ToString()?.Trim()
                    ?? obj["Caption"]?.ToString()?.Trim()
                    ?? "Monitor";
                if (string.IsNullOrWhiteSpace(pnp))
                {
                    continue;
                }

                var deviceId = obj["DeviceID"]?.ToString()?.Trim() ?? pnp;
                yield return new DisplayEdidSnapshot(deviceId, BuildEdidSignature(pnp, name), name);
            }
        }
    }

    private static List<DisplayEdidSnapshot> Deduplicate(List<DisplayEdidSnapshot> list)
    {
        return list
            .GroupBy(s => s.EdidSignature, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static IEnumerable<ManagementObject> Search(string query)
    {
        using var cts = new CancellationTokenSource(WmiTimeoutMs);
        using var searcher = new ManagementObjectSearcher(query);
        var results = new List<ManagementObject>();
        try
        {
            foreach (var obj in searcher.Get())
            {
                cts.Token.ThrowIfCancellationRequested();
                if (obj is ManagementObject managementObject)
                {
                    results.Add(managementObject);
                }
            }
        }
        catch (OperationCanceledException)
        {
            foreach (var item in results)
            {
                item.Dispose();
            }

            return Array.Empty<ManagementObject>();
        }

        return results;
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
