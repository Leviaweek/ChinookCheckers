using ChinookCheckers.Engine.Models;

namespace ChinookCheckers.Api;

/// <summary>
/// Strength levels. The hard time is how long the engine may run before it is told to answer immediately;
/// KingsRow overshoots its soft time by up to 2x, so the strong level is capped to keep answers under 600 ms.
/// A request may only tighten these limits, never loosen them.
/// </summary>
public static class Levels
{
    private static readonly Dictionary<string, SearchLimits> Table = new(StringComparer.OrdinalIgnoreCase)
    {
        ["weak"] = new(8, 100, 200),
        ["medium"] = new(12, 250, 400),
        ["strong"] = new(18, 450, 550)
    };

    public const string Default = "medium";

    public static string Normalize(string? level)
    {
        level ??= Default;
        if (!Table.ContainsKey(level)) throw new ApiException(422, $"Unknown level '{level}'. Use weak, medium or strong.");
        return level.ToLowerInvariant();
    }

    public static SearchLimits LimitsFor(string level, SuggestLimits? requested)
    {
        var limits = Table[level];
        if (requested == null) return limits;

        var soft = Math.Min(limits.SoftTimeMs, requested.SoftTimeMs ?? int.MaxValue);
        var hard = Math.Min(limits.HardTimeMs, requested.HardTimeMs ?? int.MaxValue);
        var depth = Math.Min(limits.MaxDepth, requested.MaxDepth ?? int.MaxValue);

        if (soft < 1 || hard < 1 || depth < 1)
            throw new ApiException(422, "Limits must be positive.");

        return new SearchLimits(depth, soft, Math.Max(hard, soft));
    }
}
