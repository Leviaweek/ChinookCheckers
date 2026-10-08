using ChinookCheckers.Engine.Models;

namespace ChinookCheckers.Engine;

/// <summary>
/// Fixed set of long-lived engines, one search at a time per engine, round-robin assignment.
/// Each worker loads its own copy of the DLL (loading the same path twice would share one module and its state).
/// </summary>
public sealed class EnginePool : IDisposable
{
    private sealed record Worker(KingsRowEngine Engine, SemaphoreSlim Lock);

    private readonly Worker[] _workers;
    private int _next = -1;

    private EnginePool(Worker[] workers) => _workers = workers;

    public int LiveWorkers => _workers.Length;

    /// <summary>
    /// Loads <paramref name="workers"/> engines, points them at the endgame databases and warms them up.
    /// Slow (database initialization); call once at startup. The DLL copies are written next to the original,
    /// because the engine reads its weights and opening book from the DLL directory.
    /// </summary>
    public static EnginePool Create(string dllPath, string dbPath, int dbMbytes, int workers = 2)
    {
        var engines = new List<Worker>();
        try
        {
            for (var i = 0; i < workers; i++)
            {
                var engine = new KingsRowEngine(EnsureWorkerCopy(dllPath, i));
                engines.Add(new Worker(engine, new SemaphoreSlim(1, 1)));

                engine.Configure(dbPath, dbMbytes);
                engine.RequireCommand("set book 0"); // deterministic answers
                engine.WarmUp();
            }
        }
        catch
        {
            foreach (var worker in engines) worker.Engine.Dispose();
            throw;
        }

        return new EnginePool(engines.ToArray());
    }

    /// <summary>
    /// Waits for the next worker (cancelling while waiting throws), then searches. Cancelling during the search
    /// makes the engine return its best move so far.
    /// </summary>
    public async Task<SearchResult> RunAsync(Position position, SearchLimits limits, CancellationToken ct = default)
    {
        var worker = _workers[(uint)Interlocked.Increment(ref _next) % (uint)_workers.Length];

        await worker.Lock.WaitAsync(ct);
        try
        {
            return await worker.Engine.SearchAsync(position, limits, ct);
        }
        finally
        {
            worker.Lock.Release();
        }
    }

    private static string EnsureWorkerCopy(string dllPath, int index)
    {
        var full = Path.GetFullPath(dllPath);
        var copy = Path.Combine(Path.GetDirectoryName(full)!,
            $"{Path.GetFileNameWithoutExtension(full)}.worker{index}{Path.GetExtension(full)}");

        if (!File.Exists(copy) || File.GetLastWriteTimeUtc(copy) < File.GetLastWriteTimeUtc(full))
            File.Copy(full, copy, overwrite: true);

        return copy;
    }

    public void Dispose()
    {
        foreach (var worker in _workers)
        {
            worker.Engine.Dispose();
            worker.Lock.Dispose();
        }
    }
}
