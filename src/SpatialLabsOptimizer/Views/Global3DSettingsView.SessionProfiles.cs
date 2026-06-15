using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class Global3DSettingsView
{
            : $"Effective runtime: {effectiveRuntime}";
    }

    private static bool IsSteamVrInstalled()
    {
        var steamVr = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam",
            "steamapps",
            "common",
            "SteamVR");
        return Directory.Exists(steamVr);
    }
}
}
