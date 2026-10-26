using System.Text.RegularExpressions;

namespace FiveThreeOneTracker.Services;

/// <summary>
/// Fuzzy exercise-name matching used to reuse logged accessory weights for PPL exercises.
/// Handles plurals, hyphenation, and common abbreviations, and refuses matches whose equipment
/// or movement-defining words conflict (e.g. "Leg Curl" vs "Dumbbell Curl").
/// </summary>
public static partial class ExerciseNameMatcher
{
    public const double AutoMatchThreshold = 0.7;
    public const double SuggestThreshold = 0.5;

    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["db"] = ["dumbbell"],
        ["dbs"] = ["dumbbell"],
        ["bb"] = ["barbell"],
        ["kb"] = ["kettlebell"],
        ["ez"] = ["ezbar"],
        ["ext"] = ["extension"],
        ["tri"] = ["tricep"],
        ["triceps"] = ["tricep"],
        ["bi"] = ["bicep"],
        ["biceps"] = ["bicep"],
        ["ohp"] = ["overhead", "press"],
        ["rdl"] = ["romanian", "deadlift"],
        ["sldl"] = ["stiff", "deadlift"],
        ["pulldown"] = ["pulldown"],
        ["pullup"] = ["pullup"],
        ["chinup"] = ["chinup"],
        ["pushup"] = ["pushup"],
        ["flye"] = ["fly"],
        ["flies"] = ["fly"],
        ["flyes"] = ["fly"],
        ["raises"] = ["raise"],
        ["presses"] = ["press"],
        ["calves"] = ["calf"],
    };

    private static readonly HashSet<string> EquipmentTokens = new(StringComparer.Ordinal)
    {
        "dumbbell", "barbell", "cable", "machine", "kettlebell", "smith", "ezbar", "band", "trapbar"
    };

    // Words that change the movement; if only one side has them, the exercises differ.
    private static readonly HashSet<string> DistinguishingTokens = new(StringComparer.Ordinal)
    {
        "leg", "hammer", "incline", "decline", "lateral", "front", "rear", "reverse", "romanian", "stiff",
        "sumo", "overhead", "split", "calf", "face", "upright", "preacher", "concentration", "hip", "goblet",
        "hack", "bulgarian", "close", "wide", "spider", "skull", "nordic", "good", "shrug", "glute"
    };

    // Descriptors that never decide whether two exercises are the same movement.
    private static readonly HashSet<string> NoiseTokens = new(StringComparer.Ordinal)
    {
        "the", "a", "with", "and", "of", "on", "weighted", "standing", "seated", "single", "arm", "one",
        "alternating", "grip", "bent", "over", "tricep", "bicep", "exercise"
    };

    public static IReadOnlyList<string> Tokenize(string name)
    {
        var text = NonAlphaNumeric().Replace(name.ToLowerInvariant().Replace("-", " "), " ");
        text = JoinedUpPattern().Replace(text, "$1up");
        text = text.Replace("trap bar", "trapbar").Replace("ez bar", "ezbar").Replace("pull down", "pulldown");

        var tokens = new List<string>();
        foreach (var raw in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Synonyms.TryGetValue(raw, out var mapped))
            {
                tokens.AddRange(mapped);
                continue;
            }
            tokens.Add(Singularize(raw));
        }
        return tokens;
    }

    /// <summary>Returns 0 (no match) to 1 (same exercise).</summary>
    public static double Score(string a, string b)
    {
        var left = Tokenize(a).ToHashSet(StringComparer.Ordinal);
        var right = Tokenize(b).ToHashSet(StringComparer.Ordinal);
        if (left.Count == 0 || right.Count == 0) return 0;
        if (left.SetEquals(right)) return 1;

        var leftEquipment = left.Where(EquipmentTokens.Contains).ToHashSet();
        var rightEquipment = right.Where(EquipmentTokens.Contains).ToHashSet();
        if (leftEquipment.Count > 0 && rightEquipment.Count > 0 && !leftEquipment.Overlaps(rightEquipment))
            return 0;

        var leftDistinct = left.Where(DistinguishingTokens.Contains).ToHashSet();
        var rightDistinct = right.Where(DistinguishingTokens.Contains).ToHashSet();
        if (!leftDistinct.SetEquals(rightDistinct))
            return 0;

        var leftCore = left.Where(IsCoreToken).ToHashSet();
        var rightCore = right.Where(IsCoreToken).ToHashSet();
        if (leftCore.Count == 0 && rightCore.Count == 0)
            return leftDistinct.Count > 0 ? AdjustForEquipment(0.95, leftEquipment, rightEquipment) : 0;
        if (leftCore.Count == 0 || rightCore.Count == 0) return 0;

        var shared = leftCore.Intersect(rightCore).Count();
        if (shared == 0) return 0;

        var jaccard = (double)shared / leftCore.Union(rightCore).Count();
        var containment = (double)shared / Math.Min(leftCore.Count, rightCore.Count);
        return AdjustForEquipment(0.5 * jaccard + 0.5 * containment, leftEquipment, rightEquipment);
    }

    // Same explicit equipment is a strong signal; one-sided equipment is a mild uncertainty.
    private static double AdjustForEquipment(double score, HashSet<string> left, HashSet<string> right)
    {
        if (left.Count > 0 && right.Count > 0)
            score += 0.05;
        else if (left.Count != right.Count)
            score -= 0.05;
        return Math.Clamp(score, 0, 0.99);
    }

    private static bool IsCoreToken(string token) =>
        !EquipmentTokens.Contains(token) && !NoiseTokens.Contains(token) && !DistinguishingTokens.Contains(token);

    private static string Singularize(string token)
    {
        if (token.Length <= 3 || token.EndsWith("ss", StringComparison.Ordinal) || token.EndsWith("us", StringComparison.Ordinal))
            return token;
        if (token.EndsWith("ies", StringComparison.Ordinal))
            return token[..^3] + "y";
        if (token.EndsWith("es", StringComparison.Ordinal) && (token.EndsWith("ches", StringComparison.Ordinal) || token.EndsWith("shes", StringComparison.Ordinal)))
            return token[..^2];
        return token.EndsWith('s') ? token[..^1] : token;
    }

    [GeneratedRegex(@"[^a-z0-9 ]")]
    private static partial Regex NonAlphaNumeric();

    [GeneratedRegex(@"\b(pull|chin|push)\s+ups?\b")]
    private static partial Regex JoinedUpPattern();
}
