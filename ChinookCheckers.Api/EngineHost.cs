using ChinookCheckers.Engine;
using Microsoft.Extensions.Options;

namespace ChinookCheckers.Api;

public sealed class EngineOptions
{
    public string DllPath { get; set; } = "";
    public string DbPath { get; set; } = "";
    public int DbMbytes { get; set; } = 512;
    public int Workers { get; set; } = 2;
}

/// <summary>Creates and warms up the engine pool at startup; the DLLs are never loaded per request.</summary>
public sealed class EngineHost(IOptions<EngineOptions> options) : IHostedService
{
    public EnginePool? Pool { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var o = options.Value;
        Pool = await Task.Run(() => EnginePool.Create(o.DllPath, o.DbPath, o.DbMbytes, o.Workers), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Pool?.Dispose();
        return Task.CompletedTask;
    }
}
