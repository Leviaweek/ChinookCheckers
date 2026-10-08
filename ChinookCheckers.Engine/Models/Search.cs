namespace ChinookCheckers.Engine.Models;

/// <summary>MaxDepth is carried for the API contract; the CheckerBoard getmove call has no depth parameter.</summary>
public sealed record SearchLimits(int MaxDepth, int SoftTimeMs, int HardTimeMs);

/// <summary>
/// BestMove is the legal move that leads to After (full path, e.g. "31x24x15"); LegalMove is null when the
/// engine answered with a position no legal move produces. ScoreOrWdl is 1/0/-1 (win/draw/loss) for a tablebase hit, otherwise the engine's search value.
/// </summary>
public sealed record SearchResult(
    string BestMove,
    IReadOnlyList<string> Pv,
    int ScoreOrWdl,
    long Nodes,
    int Depth,
    bool TablebaseHit,
    Position After,
    Move? LegalMove);
