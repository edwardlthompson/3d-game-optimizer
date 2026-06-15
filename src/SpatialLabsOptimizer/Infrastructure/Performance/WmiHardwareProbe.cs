using System.Management;

namespace SpatialLabsOptimizer.Infrastructure.Performance;

internal static class WmiHardwareProbe
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

    internal static IReadOnlyList<VideoControllerInfo> QueryVideoControllers()
    {
        var list = new List<VideoControllerInfo>();
        foreach (var obj in Search("SELECT Name, AdapterRAM, DriverVersion, PNPDeviceID FROM Win32_VideoController"))
        {
            using (obj)
            {
                var name = obj["Name"]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(name) ||
                    name.Contains("Microsoft Basic", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                list.Add(new VideoControllerInfo(
                    name,
                    ParseAdapterRam(obj["AdapterRAM"]),
                    obj["DriverVersion"]?.ToString()?.Trim() ?? "Unknown",
                    obj["PNPDeviceID"]?.ToString() ?? ""));
            }
        }

        return list;
    }

    internal static VideoControllerInfo? SelectPrimaryGpu(IReadOnlyList<VideoControllerInfo> controllers)
    {
        if (controllers.Count == 0)
        {
            return null;
        }

        return controllers
            .OrderByDescending(c => c.AdapterRamBytes)
            .ThenByDescending(c => IsDiscreteGpu(c) ? 1 : 0)
            .First();
    }

    internal static bool HasHybridGraphics(IReadOnlyList<VideoControllerInfo> controllers)
    {
        if (controllers.Count < 2)
        {
            return false;
        }

        var hasIntegrated = controllers.Any(IsIntegratedGpu);
        var hasDiscrete = controllers.Any(IsDiscreteGpu);
        return hasIntegrated && hasDiscrete;
    }

    internal static bool IsDiscreteGpu(VideoControllerInfo controller)
    {
        if (controller.Name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
            controller.Name.Contains("AMD Radeon", StringComparison.OrdinalIgnoreCase) ||
            controller.Name.Contains("GeForce", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (controller.Name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            return controller.Name.Contains("Arc", StringComparison.OrdinalIgnoreCase);
        }

        return controller.PnpDeviceId.Contains("VEN_10DE", StringComparison.OrdinalIgnoreCase) ||
               controller.PnpDeviceId.Contains("VEN_1002", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIntegratedGpu(VideoControllerInfo controller)
    {
        return controller.Name.Contains("Intel", StringComparison.OrdinalIgnoreCase) &&
               !controller.Name.Contains("Arc", StringComparison.OrdinalIgnoreCase);
    }

    private static long ParseAdapterRam(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        if (value is uint u32)
        {
            return u32;
        }

        if (value is ulong u64)
        {
            return (long)Math.Min(u64, long.MaxValue);
        }

        return long.TryParse(value.ToString(), out var parsed) ? parsed : 0;
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
