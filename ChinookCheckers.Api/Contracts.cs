using System.Text.Json.Serialization;

namespace ChinookCheckers.Api;

public sealed record SuggestRequest(string? GameId, SuggestState? State, string? Level, SuggestLimits? Limits);

public sealed record SuggestState(string? Notation, string? Position);

public sealed record SuggestLimits(int? MaxDepth, int? SoftTimeMs, int? HardTimeMs);

public sealed record SuggestResponse(
    string Engine,
    string BestMove,
    IReadOnlyList<string> Pv,
    [property: JsonPropertyName("scoreOrWDL")] int ScoreOrWdl,
    int Depth,
    long Nodes,
    string PositionKey,
    SuggestInfo Info);

public sealed record SuggestInfo(bool TablebaseHit, long TimeMs);

public sealed record ValidateRequest(string? Position, string? Move);

public sealed record ValidateResponse(bool Legal);

/// <summary>An error that maps straight to an HTTP status.</summary>
public sealed class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
