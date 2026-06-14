namespace SpatialLabsOptimizer.Infrastructure.Displays;

public sealed class ViewingDistanceCoach
{
    private static readonly Dictionary<string, string> Tips = new(StringComparer.OrdinalIgnoreCase)
    {
        ["acer-psv27-2"] = "Sit 60–80 cm from the SpatialLabs View 27. Center your head in the sweet spot for stable eye-tracking.",
        ["acer-spatiallabs-15"] = "Laptop lenticular panels work best at 50–70 cm with the screen slightly below eye level.",
        ["samsung-g90xf"] = "Position yourself 70–90 cm from the Odyssey 3D panel. Ensure the USB eye-tracking camera has a clear view.",
        ["nvidia-3d-vision-generic"] = "Active-shutter 3D requires glasses and a direct line of sight to the monitor.",
        ["generic-manual"] = "Start at 65 cm viewing distance and adjust depth if edges feel uncomfortable."
    };

    public string GetTipForProfile(string profileId) =>
        Tips.TryGetValue(profileId, out var tip) ? tip : Tips["generic-manual"];
}
