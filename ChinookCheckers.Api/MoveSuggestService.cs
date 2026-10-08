using System.Diagnostics;
using ChinookCheckers.Engine;
using ChinookCheckers.Engine.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ChinookCheckers.Api;

public sealed class MoveSuggestService(EngineHost host, IMemoryCache cache)
{
    private const int TablebasePieces = 8;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    // Endgame positions are answered from the database almost instantly, so ask with a tiny limit first.
    private static readonly SearchLimits TablebaseProbe = new(0, 30, 45);

    public async Task<SuggestResponse> SuggestAsync(SuggestRequest request, CancellationToken ct)
    {
        var started = Stopwatch.StartNew();

        var pool = host.Pool ?? throw new ApiException(503, "Engine is still starting.");
        var position = ParsePosition(request.State?.Position);
        var level = Levels.Normalize(request.Level);
        var limits = Levels.LimitsFor(level, request.Limits);

        if (MoveGenerator.LegalMoves(position).Count == 0)
            throw new ApiException(422, "The side to move has no legal moves.");

        var key = $"{position.CanonicalKey}|{level}";
        if (!cache.TryGetValue(key, out SearchResult? result) || result == null)
        {
            // hardTimeMs covers waiting for a free worker too; expiring while queued becomes a 504.
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(limits.HardTimeMs);

            result = await SearchAsync(pool, position, limits, deadline.Token);

            if (result.LegalMove == null)
                throw new ApiException(500, $"Engine returned an illegal move for {position.CanonicalKey}.");

            cache.Set(key, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl, Size = 1 });
        }

        return new SuggestResponse("chinook", result.BestMove, result.Pv, result.ScoreOrWdl, result.Depth, result.Nodes,
            position.CanonicalKey, new SuggestInfo(result.TablebaseHit, started.ElapsedMilliseconds));
    }

    public static Position ParsePosition(string? pdn)
    {
        if (string.IsNullOrWhiteSpace(pdn)) throw new ApiException(422, "Position is required.");

        try
        {
            return Position.Parse(pdn.Trim());
        }
        catch (FormatException e)
        {
            throw new ApiException(422, e.Message);
        }
    }

    private static async Task<SearchResult> SearchAsync(EnginePool pool, Position position, SearchLimits limits, CancellationToken ct)
    {
        if (position.PieceCount <= TablebasePieces)
        {
            var probe = await pool.RunAsync(position, TablebaseProbe, ct);
            if (probe.TablebaseHit && probe.LegalMove != null) return probe;
        }

        return await pool.RunAsync(position, limits, ct);
    }
}
