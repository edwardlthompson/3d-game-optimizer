namespace SpatialLabsOptimizer.Infrastructure.Compatibility;

/// <summary>
/// Mirrors site/catalog/src/rank-3d.ts — highest-scoring play path per title (0–100).
/// </summary>
public static class CatalogRank3DScorer
{
    private static readonly Dictionary<string, int> LevelScore = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ultra3d"] = 88,
        ["native3d"] = 72,
        ["optimized3d"] = 58,
        ["playable3d"] = 42,
        ["experimental3d"] = 26,
        ["unsupported2d"] = 8,
    };

    private static readonly Dictionary<string, int> MethodScore = new(StringComparer.Ordinal)
    {
        ["truegame|3D Ultra"] = 100,
        ["truegame|Acer TrueGame · 3D Ultra"] = 100,
        ["truegame|3D+"] = 82,
        ["truegame|Acer TrueGame · 3D"] = 84,
        ["uevr|Works Perfectly"] = 97,
        ["uevr|UEVR · 3D Ultra"] = 97,
        ["uevr|Works Well"] = 66,
        ["uevr|UEVR · Optimized"] = 66,
        ["uevr|Works OK"] = 49,
        ["uevr|UEVR · Playable"] = 49,
        ["uevr|Works Poorly"] = 27,
        ["uevr|UEVR · Experimental"] = 27,
        ["uevr|VRto3D wiki"] = 46,
        ["uevr|VRto3D · Playable"] = 46,
        ["odyssey-hub|Odyssey 3D Hub"] = 94,
        ["odyssey-hub|Samsung Odyssey 3D Hub · 3D"] = 78,
        ["nvidia-3d-vision|3D Vision Ready"] = 88,
        ["nvidia-3d-vision|NVIDIA 3D Vision · 3D"] = 86,
        ["reshade-depth|Strong depth"] = 38,
        ["reshade-depth|ReShade depth · Playable"] = 40,
        ["reshade-depth|ReShade depth · Experimental"] = 28,
        ["manual|Curated seed"] = 35,
        ["manual|Curated · 3D"] = 74,
        ["manual|Curated · Playable"] = 42,
        ["manual|Curated · Experimental"] = 26,
        ["manual|Curated · Unsupported"] = 8,
    };

    public static CatalogRank3DResult Rank(CatalogRank3DInput game)
    {
        var bestLevel = game.BestLevel ?? "";
        var best = new CatalogRank3DResult(
            LevelScore.GetValueOrDefault(bestLevel, 0),
            game.BestExperience?.Label ?? bestLevel,
            game.BestExperience is not null
                ? $"{game.BestExperience.PlatformKey}|{game.BestExperience.Label}"
                : bestLevel);

        if (game.BestExperience is not null)
        {
            best = RankEntry(
                game.BestExperience.PlatformKey,
                game.BestExperience.Label,
                game.BestExperience.Level);
        }

        foreach (var entry in game.PlatformSupport)
        {
            var candidate = RankEntry(entry.PlatformKey, entry.Label, entry.Level);
            if (candidate.Score > best.Score
                || (candidate.Score == best.Score
                    && string.Compare(candidate.Label, best.Label, StringComparison.Ordinal) < 0))
            {
                best = candidate;
            }
        }

        return best;
    }

    public static int Score(CatalogRank3DInput game) => Rank(game).Score;

    private static CatalogRank3DResult RankEntry(string platformKey, string label, string level)
    {
        var key = $"{platformKey}|{label}";
        var score = MethodScore.GetValueOrDefault(key, LevelScore.GetValueOrDefault(level, 0));
        return new CatalogRank3DResult(score, label, key);
    }
}

public sealed record CatalogRank3DInput(
    string? BestLevel,
    CatalogRank3DPath? BestExperience,
    IReadOnlyList<CatalogRank3DPath> PlatformSupport);

public sealed record CatalogRank3DPath(
    string PlatformKey,
    string Label,
    string Level);

public sealed record CatalogRank3DResult(int Score, string Label, string FilterKey);
