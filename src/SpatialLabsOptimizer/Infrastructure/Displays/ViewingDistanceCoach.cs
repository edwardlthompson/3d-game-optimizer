namespace SpatialLabsOptimizer.Infrastructure.Displays;

public sealed record ViewingDistanceGuide(
    string ProfileId,
    int MinDistanceCm,
    int MaxDistanceCm,
    int RecommendedDistanceCm,
    string Tip);

public sealed class ViewingDistanceCoach
{
    private static readonly Dictionary<string, ViewingDistanceGuide> Guides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["acer-psv27-2"] = new(
            "acer-psv27-2", 60, 80, 70,
            "Sit 60–80 cm from the SpatialLabs View 27. Center your head in the sweet spot for stable eye-tracking."),
        ["acer-spatiallabs-15"] = new(
            "acer-spatiallabs-15", 50, 70, 60,
            "Laptop lenticular panels work best at 50–70 cm with the screen slightly below eye level."),
        ["samsung-g90xf"] = new(
            "samsung-g90xf", 70, 90, 80,
            "Position yourself 70–90 cm from the Odyssey 3D panel. Ensure the USB eye-tracking camera has a clear view."),
        ["nvidia-3d-vision-generic"] = new(
            "nvidia-3d-vision-generic", 60, 90, 75,
            "Active-shutter 3D requires glasses and a direct line of sight to the monitor."),
        ["generic-manual"] = new(
            "generic-manual", 55, 85, 65,
            "Start at 65 cm viewing distance and adjust depth if edges feel uncomfortable.")
    };

    public ViewingDistanceGuide GetGuideForProfile(string profileId) =>
        Guides.TryGetValue(profileId, out var guide) ? guide : Guides["generic-manual"];

    public string GetTipForProfile(string profileId) => GetGuideForProfile(profileId).Tip;

    public string EvaluateDistance(string profileId, int distanceCm)
    {
        var guide = GetGuideForProfile(profileId);
        if (distanceCm < guide.MinDistanceCm)
        {
            return $"Move back — {distanceCm} cm is closer than the {guide.MinDistanceCm} cm minimum.";
        }

        if (distanceCm > guide.MaxDistanceCm)
        {
            return $"Move closer — {distanceCm} cm is beyond the {guide.MaxDistanceCm} cm maximum.";
        }

        return distanceCm == guide.RecommendedDistanceCm
            ? "Perfect — you are at the recommended distance."
            : $"Good — {distanceCm} cm is within the {guide.MinDistanceCm}–{guide.MaxDistanceCm} cm sweet spot.";
    }
}
