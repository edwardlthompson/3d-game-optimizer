namespace SpatialLabsOptimizer.Domain;

public enum DisplayVendor
{
    AcerSpatialLabs,
    SamsungOdyssey3D,
    Nvidia3DVision,
    Generic
}

public enum CompatibilityTier
{
    Unsupported = 5,
    Experimental = 4,
    Playable = 3,
    Optimized = 2,
    Native = 1
}

public enum LaunchPlatform
{
    TrueGame,
    Odyssey3DHub,
    Nvidia3DVision,
    Uevr,
    ReShade,
    Tweak,
    Blocked
}

public enum LaunchReadinessState
{
    Ready,
    NeedsPresetCache,
    NeedsInstall,
    NeedsToolchain,
    Blocked
}

public enum PerformanceTier
{
    Low,
    Medium,
    High,
    Enthusiast
}

public enum ResponsiveBreakpoint
{
    Narrow,
    Medium,
    Wide
}

public enum VrCapability
{
    None,
    NativeVr,
    UevrCompatible
}
