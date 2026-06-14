using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed record LaunchPreviewSummary(
    string Title,
    string PlatformLine,
    string DepthLine,
    string ToolchainLine,
    string TierLine);

public sealed class LaunchPreviewService
{
    public LaunchPreviewSummary Summarize(ResolvedGameLaunchPlan plan)
    {
        return new LaunchPreviewSummary(
            plan.Title,
            $"Platform: {FormatPlatform(plan.Platform)}",
            $"Depth {plan.Depth:P0}, convergence {plan.Convergence:P0}, separation {plan.Separation:P0}",
            $"Toolchain: {ToolchainFor(plan.Platform)}",
            $"Compatibility tier: {plan.Tier}");
    }

    public string ToProgressMessage(LaunchPreviewSummary summary) =>
        $"{summary.PlatformLine} · {summary.DepthLine} · {summary.ToolchainLine}";

    private static string FormatPlatform(LaunchPlatform platform) => platform switch
    {
        LaunchPlatform.TrueGame => "TrueGame",
        LaunchPlatform.Odyssey3DHub => "Odyssey 3D Hub",
        LaunchPlatform.Nvidia3DVision => "NVIDIA 3D Vision",
        LaunchPlatform.Uevr => "UEVR",
        LaunchPlatform.ReShade => "ReShade",
        LaunchPlatform.Tweak => "Tweak",
        LaunchPlatform.Blocked => "Blocked",
        _ => platform.ToString()
    };

    private static string ToolchainFor(LaunchPlatform platform) => platform switch
    {
        LaunchPlatform.Uevr => "UEVR injector + cached preset",
        LaunchPlatform.ReShade => "ReShade + depth shader",
        LaunchPlatform.TrueGame => "Native TrueGame bridge",
        LaunchPlatform.Odyssey3DHub => "Samsung Odyssey 3D Hub",
        LaunchPlatform.Nvidia3DVision => "NVIDIA 3D Vision driver stack",
        LaunchPlatform.Tweak => "Display tweak profile",
        LaunchPlatform.Blocked => "None (blocked)",
        _ => "Auto-selected adapter"
    };
}
