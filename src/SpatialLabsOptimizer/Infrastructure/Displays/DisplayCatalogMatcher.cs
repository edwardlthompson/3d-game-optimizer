using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Displays;

internal static class DisplayCatalogMatcher
{
    public static DisplayProfile? MatchCatalog(
        IReadOnlyList<DisplayProfile> catalog,
        IReadOnlyList<DisplayEdidSnapshot> snapshots)
    {
        DisplayProfile? best = null;
        foreach (var snapshot in snapshots)
        {
            foreach (var profile in catalog)
            {
                if (profile.Id == "generic-manual" || !SignatureMatchesProfile(profile, snapshot))
                {
                    continue;
                }

                best = PickBetterMatch(best, profile);
            }
        }

        if (best is not null)
        {
            return best;
        }

        foreach (var snapshot in snapshots)
        {
            if (NameHintsAsv15(snapshot))
            {
                return catalog.FirstOrDefault(p => p.Id == "acer-asv15-1");
            }

            if (NameHintsSpatialLabsLaptop(snapshot))
            {
                return catalog.FirstOrDefault(p => p.Id == "acer-spatiallabs-15");
            }
        }

        return null;
    }

    private static DisplayProfile PickBetterMatch(DisplayProfile? current, DisplayProfile candidate)
    {
        if (current is null)
        {
            return candidate;
        }

        return TypePriority(candidate.Type) > TypePriority(current.Type) ? candidate : current;
    }

    private static int TypePriority(string type) => type switch
    {
        "glasses-free-3d-monitor" => 3,
        "active-shutter-3d" => 2,
        "integrated-display" => 1,
        _ => 0
    };

    internal static bool SignatureMatchesProfile(DisplayProfile profile, DisplayEdidSnapshot snapshot)
    {
        foreach (var signature in profile.EdidSignatures)
        {
            if (signature.StartsWith("EDID_PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (MatchesPattern(signature, snapshot.EdidSignature))
            {
                return true;
            }

            if (MatchesPattern(signature, snapshot.FriendlyName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool NameHintsAsv15(DisplayEdidSnapshot snapshot)
    {
        var text = $"{snapshot.FriendlyName} {snapshot.EdidSignature} {snapshot.DeviceId}";
        return text.Contains("ASV15", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("SpatialLabs View", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Spatial Labs View", StringComparison.OrdinalIgnoreCase);
    }

    private static bool NameHintsSpatialLabsLaptop(DisplayEdidSnapshot snapshot)
    {
        var text = $"{snapshot.FriendlyName} {snapshot.EdidSignature} {snapshot.DeviceId}";
        return (text.Contains("SpatialLabs", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Spatial Labs", StringComparison.OrdinalIgnoreCase)) &&
               (text.Contains("Laptop", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Panel", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("1022:", StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesPattern(string pattern, string value)
    {
        if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (pattern.Contains('*', StringComparison.Ordinal))
        {
            var parts = pattern.Split('*', StringSplitOptions.RemoveEmptyEntries);
            var index = 0;
            foreach (var part in parts)
            {
                var found = value.IndexOf(part, index, StringComparison.OrdinalIgnoreCase);
                if (found < 0)
                {
                    return false;
                }

                index = found + part.Length;
            }

            return true;
        }

        return string.Equals(pattern, value, StringComparison.OrdinalIgnoreCase);
    }
}
