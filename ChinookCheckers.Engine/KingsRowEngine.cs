using ChinookCheckers.Engine.Models;

namespace ChinookCheckers.Engine;

public sealed class KingsRowEngine: IDisposable
{
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