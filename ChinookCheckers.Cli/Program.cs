using System.Diagnostics;
using ChinookCheckers.Engine;
using ChinookCheckers.Engine.Models;

if (args.Length < 1)
{
    Console.WriteLine("Usage: Cli <path to KingsRow dll> [path to endgame databases]");
    return 1;
}

Console.WriteLine($"64-bit process: {Environment.Is64BitProcess}");

using var engine = new KingsRowEngine(args[0]);
var failures = 0;

// ---------------------------------------------------------------
Section("1. Handshake");
Console.WriteLine($"name: {engine.Name()}");
Check("protocol version is 2", engine.ProtocolVersion() == "2", engine.ProtocolVersion());
Check("game type is 21 (English checkers)", engine.GameType() == "21", engine.GameType());

// ---------------------------------------------------------------
Section("2. Engine commands");
foreach (var command in new[] { "get hashsize", "get dbmbytes", "get book", "set book 0" })
{
    var (handled, reply) = engine.Command(command);
    Console.WriteLine($"{command,-14} handled={handled} reply='{reply}'");
}

if (args.Length > 1)
{
    var (handled, reply) = engine.Command($"set dbpath {args[1]}");
    Check("set dbpath handled", handled, $"reply='{reply}'");
}

// ---------------------------------------------------------------
// Positions with exactly one legal move: they pin down board orientation and color constants.
Section("3. Geometry and color (one legal move each)");
await ExpectExactAsync("white man 29 can only go to 25", "W:W29:B4", "B:W25:B4");
await ExpectExactAsync("black man 4 can only go to 8", "B:W29:B4", "W:W29:B8");
await ExpectExactAsync("forced capture 22x15", "W:W22:B4,18", "B:W15:B4");
await ExpectExactAsync("white man 5-1 promotes to king", "W:W5:B28", "B:WK1:B28");

// ---------------------------------------------------------------
Section("4. Starting position");
const string startBlack = "B:W21,22,23,24,25,26,27,28,29,30,31,32:B1,2,3,4,5,6,7,8,9,10,11,12";
const string startWhite = "W:W21,22,23,24,25,26,27,28,29,30,31,32:B1,2,3,4,5,6,7,8,9,10,11,12";

Console.WriteLine(engine.Command("set dbmbytes 256"));
Console.WriteLine(engine.Command("set hashsize 64"));

var pos = Position.Parse(startBlack);

var t = new Thread(() =>
{
    var b = new int[64];
    pos.WriteToBoard(b);
    var (r, s) = engine.GetMoveRaw(b, (int)pos.SideToMove, 0.5);
    Console.WriteLine($"{r} '{s}'");
}, maxStackSize: 64 * 1024 * 1024);

t.Start();
t.Join();


// ---------------------------------------------------------------
Section("5. Middlegame (position from the task)");
const string midgame = "B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16";

{
    var (result, ms) = await RunAsync(midgame, 0.5, 2.0);
    PrintInfo("middlegame, 0.5 s", result, ms);
    Check("engine changed the position", result.After.ToPdnString() != result.Before.ToPdnString());
}

// ---------------------------------------------------------------
Section("6. Hard time limit (playnow)");
{
    var (result, ms) = await RunAsync(midgame, soft: 5.0, hard: 0.4);
    Console.WriteLine($"status: '{result.Status}'");
    Check("search stopped long before softMaxTime", ms < 1500, $"{ms} ms for soft=5.0 s, hard=0.4 s");

    var (next, nextMs) = await RunAsync(midgame, 1.0, 3.0);
    Console.WriteLine($"next search after a hard stop: {nextMs} ms (should be around 1 s, not near 0)");
    Console.WriteLine($"status: '{next.Status}'");
}

// ---------------------------------------------------------------
Section("7. Endgame (6 pieces)");
{
    var (result, ms) = await RunAsync("W:WK31,K32,22:BK1,K5,14", 0.25, 2.0);
    PrintInfo("endgame", result, ms);
}

// ---------------------------------------------------------------
Section("8. Stability: 20 repeated calls");
{
    var ok = true;
    for (var i = 0; i < 20; i++)
    {
        var (result, _) = await RunAsync("W:W29:B4", 0.1, 1.0);
        ok &= result.After.ToPdnString() == "B:W25:B4";
    }
    Check("20 identical forced-move calls give the same answer", ok);
}

Section("9. SearchAsync (tablebase hit and strong level)");
{
    engine.WarmUp();

    var sw = Stopwatch.StartNew();
    var tb = await engine.SearchAsync(Position.Parse("B:W29:B4,K12"), new SearchLimits(12, 30, 45));
    sw.Stop();
    Check("3 pieces: tablebase hit under 50 ms", tb.TablebaseHit && sw.ElapsedMilliseconds < 50,
        $"hit={tb.TablebaseHit} wdl={tb.ScoreOrWdl} best={tb.BestMove} {sw.ElapsedMilliseconds} ms");

    sw.Restart();
    var strong = await engine.SearchAsync(Position.Parse(midgame), new SearchLimits(18, 450, 550));
    sw.Stop();
    Check("middlegame strong: move under 600 ms, no tablebase", strong.BestMove.Length > 0 && !strong.TablebaseHit && sw.ElapsedMilliseconds < 600,
        $"best={strong.BestMove} depth={strong.Depth} nodes={strong.Nodes} pv={string.Join(" ", strong.Pv)} {sw.ElapsedMilliseconds} ms");
}

Section("10. EnginePool (2 workers)");
{
    using var pool = EnginePool.Create(args[0], args.Length > 1 ? args[1] : "", 256);
    Check("pool has 2 live workers", pool.LiveWorkers == 2);

    var sw = Stopwatch.StartNew();
    var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
        pool.RunAsync(Position.Parse(midgame), new SearchLimits(0, 300, 350))));
    sw.Stop();

    Check("4 parallel requests all answer with a move", results.All(r => r.BestMove.Length > 0),
        $"{sw.ElapsedMilliseconds} ms, depths={string.Join(",", results.Select(r => r.Depth))}");
    Check("4 requests on 2 workers take about two search rounds", sw.ElapsedMilliseconds is > 500 and < 1100, $"{sw.ElapsedMilliseconds} ms");

    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
    var blocker = pool.RunAsync(Position.Parse(midgame), new SearchLimits(0, 2000, 2000));
    var blocker2 = pool.RunAsync(Position.Parse(midgame), new SearchLimits(0, 2000, 2000));
    var waiting = Record.ExceptionAsync(() => pool.RunAsync(Position.Parse(midgame), new SearchLimits(0, 300, 350), cts.Token));
    Check("request cancelled while queued throws OperationCanceledException", await waiting is OperationCanceledException);
    await Task.WhenAll(blocker, blocker2);
}

Console.WriteLine($"\nFailures: {failures}");
return failures == 0 ? 0 : 1;

void Section(string title) => Console.WriteLine($"\n=== {title} ===");

void Check(string name, bool ok, string details = "")
{
    Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {name}{(details.Length > 0 ? " | " + details : "")}");
    if (!ok) failures++;
}

// Squares that became free / became occupied between two positions.
(List<int> Vacated, List<int> Occupied) Diff(Position before, Position after)
{
    var vacated = new List<int>();
    var occupied = new List<int>();

    for (var square = 1; square <= 32; square++)
    {
        if (before[square] == after[square]) continue;

        if (after[square].IsFree()) vacated.Add(square);
        else occupied.Add(square);
    }

    return (vacated, occupied);
}

async Task<(MoveResult Result, long Ms)> RunAsync(string pdn, double soft, double hard)
{
    var position = Position.Parse(pdn);
    var sw = Stopwatch.StartNew();
    var result = await engine.GetMoveAsync(position, soft, hard);
    sw.Stop();
    return (result, sw.ElapsedMilliseconds);
}

async Task ExpectExactAsync(string title, string pdn, string expectedAfter)
{
    var (result, ms) = await RunAsync(pdn, 0.2, 2.0);
    var after = result.After.ToPdnString();
    
    Console.WriteLine($"before: ({pdn})");
    result.Before.PrintBoard();
    
    Console.WriteLine($"after: ({after}) (expected: {expectedAfter})");
    result.After.PrintBoard();

    Check(title, after == expectedAfter,
        $"before={pdn} after={after} expected={expectedAfter} " +
        $"result={result.Result} status='{result.Status}' {ms} ms");
}

void PrintInfo(string title, MoveResult result, long ms)
{
    var (vacated, occupied) = Diff(result.Before, result.After);
    Console.WriteLine($"{title}: {ms} ms, result={result.Result}");
    Console.WriteLine($"  before: {result.Before.ToPdnString()}");
    Console.WriteLine($"  after:  {result.After.ToPdnString()}");
    Console.WriteLine($"  vacated=[{string.Join(",", vacated)}] occupied=[{string.Join(",", occupied)}]");
    Console.WriteLine($"  status: '{result.Status}'");
}
static class Record
{
    public static async Task<Exception?> ExceptionAsync(Func<Task> action)
    {
        try { await action(); return null; }
        catch (Exception e) { return e; }
    }
}
