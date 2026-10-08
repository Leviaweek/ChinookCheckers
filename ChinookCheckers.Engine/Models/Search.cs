namespace ChinookCheckers.Engine.Models;

/// <summary>MaxDepth is carried for the API contract; the CheckerBoard getmove call has no depth parameter.</summary>
public sealed record SearchLimits(int MaxDepth, int SoftTimeMs, int HardTimeMs);

/// <summary>
/// ScoreOrWdl is 1/0/-1 (win/draw/loss) for a tablebase hit, otherwise the engine's search value.
/// </summary>
public sealed record SearchResult(
    string BestMove,
    IReadOnlyList<string> Pv,
    int ScoreOrWdl,
    long Nodes,
    int Depth,
    bool TablebaseHit,
    Position After);
