using System.Management;

namespace SpatialLabsOptimizer.Infrastructure.Performance;

internal static partial class WmiHardwareProbe
{
    internal sealed record VideoControllerInfo(
        string Name,
        long AdapterRamBytes,
        string DriverVersion,
        string PnpDeviceId);

    internal static string QueryCpuName()
    {
        foreach (var obj in Search("SELECT Name FROM Win32_Processor"))
        {
            using (obj)
            {
                var name = obj["Name"]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
        }

        return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown CPU";
    }

    internal static long QueryTotalRamMb()
    {
        long totalBytes = 0;
        foreach (var obj in Search("SELECT Capacity FROM Win32_PhysicalMemory"))
        {
            using (obj)
            {
                if (obj["Capacity"] is ulong capacity)
                {
                    totalBytes += (long)capacity;
                }
                else if (obj["Capacity"] is not null && long.TryParse(obj["Capacity"]!.ToString(), out var parsed))
                {
                    totalBytes += parsed;
                }
            }
        }

        if (totalBytes <= 0)
        {
            return (int)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024));
        }

        return totalBytes / (1024 * 1024);
    }

    private static IEnumerable<ManagementObject> Search(string query, int timeoutMs = 8000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
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
            foreach (var obj in results)
            {
                obj.Dispose();
            }

            return Array.Empty<ManagementObject>();
        }

        return results;
    }
}
