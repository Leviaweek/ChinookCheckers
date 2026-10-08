using ChinookCheckers.Engine.Models;

namespace ChinookCheckers.Engine;

public sealed class KingsRowEngine: IDisposable
{
    // Chinook/KingsRow endgame databases cover up to 8 pieces.
    private const int TablebasePieces = 8;

    private static readonly string[] WarmUpPositions =
    [
        "W:W29:B4",
        "B:W29:B4,K12",
        "W:W22,23,24:BK1,K5,K14",
        "W:W22,23,24,25,26:B1,2,3,K12,K13"
    ];

    private readonly KingsRowNative _native;

    public KingsRowEngine(string dllPath)
    {
        _native = KingsRowNative.Create(dllPath);
    }
    public (bool Handled, string Reply) Command(string command)
    {
        var (handled, reply) = _native.CallEngineCommand(command);
        return (handled, reply);
    }
    
    public (NativeMoveResult Result, string Status) GetMoveRaw(Span<int> board, int color, double maxTime)
    {
        _native.ResetStop();
        return _native.CallGetMove(board, color, maxTime, 0, 0);
    }
    
    public string RequireCommand(string command)
    {
        var (handled, reply) = _native.CallEngineCommand(command);
        if (!handled) throw new InvalidOperationException($"Engine did not handle '{command}' command.");
        return reply;
    }
    
    public void Configure(string dbPath, int dbMbytes)
    {
        RequireCommand($"set dbpath {dbPath}");
        RequireCommand($"set dbmbytes {dbMbytes}");
    }

    // The endgame databases are initialized by the first search (several seconds) and their blocks are read
    // lazily, so run a forced move plus a few endgame lookups before serving requests.
    public void WarmUp()
    {
        Span<int> board = stackalloc int[64];

        foreach (var pdn in WarmUpPositions)
        {
            board.Clear();
            Position.Parse(pdn).WriteToBoard(board);
            GetMoveRaw(board, (int)Position.Parse(pdn).SideToMove, 0.05);
        }
    }

    public string Name() => RequireCommand("name");

    public string ProtocolVersion() => RequireCommand("get protocolversion");

    public string GameType() => RequireCommand("get gametype");
    
    public async Task<MoveResult> GetMoveAsync(Position position, double softMaxTime, double hardMaxTime)
    {
        using var timer = new CancellationTokenSource(TimeSpan.FromSeconds(hardMaxTime));
        
        // Resharper disable once MethodSupportsCancellation
        return await Task.Run(() =>
        {
            _native.ResetStop();
            try
            {
                using var _ = timer.Token.Register(_native.StopSearch);
                return GetMoveCore(position, softMaxTime);
            }
            finally
            {
                _native.ResetStop();
            }
        });
    }
    
    public async Task<SearchResult> SearchAsync(Position position, SearchLimits limits)
    {
        var move = await GetMoveAsync(position, limits.SoftTimeMs / 1000.0, limits.HardTimeMs / 1000.0);
        var info = SearchStatusParser.Parse(move.Status);

        var tablebaseHit = position.PieceCount <= TablebasePieces && move.Result != NativeMoveResult.Unknown;
        var score = tablebaseHit
            ? move.Result switch { NativeMoveResult.Win => 1, NativeMoveResult.Loss => -1, _ => 0 }
            : info.Value ?? 0;

        return new SearchResult(info.Pv.FirstOrDefault() ?? "", info.Pv, score, info.Nodes, info.Depth, tablebaseHit, move.After);
    }

    private MoveResult GetMoveCore(Position position, double maxTime)
    {
        Span<int> board = stackalloc int[64];
        position.WriteToBoard(board);
        
        var (result, status) = _native.CallGetMove(board, (int)position.SideToMove, maxTime, 0, 0);
        
        var resultPosition = Position.WriteFromBoard(board, position.SideToMove.Opposite());
        
        return new MoveResult
        {
            Result = result,
            Before = position,
            After = resultPosition,
            Status = status
        };
    }

    public void Dispose() => _native.Dispose();
}