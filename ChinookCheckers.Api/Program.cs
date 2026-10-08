using System.Diagnostics;
using ChinookCheckers.Api;
using ChinookCheckers.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.Configure<EngineOptions>(builder.Configuration.GetSection("Engine"));
builder.Services.AddSingleton<EngineHost>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EngineHost>());
builder.Services.AddMemoryCache(options => options.SizeLimit = 20000);
builder.Services.AddSingleton<MoveSuggestService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/healthz", (EngineHost host) =>
    host.Pool is { } pool
        ? Results.Ok(new { ok = true, workers = pool.LiveWorkers })
        : Results.Json(new { ok = false, workers = 0 }, statusCode: 503));

app.MapPost("/v1/move/suggest", async (SuggestRequest request, MoveSuggestService service, HttpContext http, ILogger<MoveSuggestService> log) =>
{
    var started = Stopwatch.StartNew();
    var requestId = http.TraceIdentifier;

    IResult Fail(int status, string error)
    {
        log.LogWarning("suggest failed {requestId} {status} {timeMs} {error}", requestId, status, started.ElapsedMilliseconds, error);
        return Results.Json(new { error }, statusCode: status);
    }

    try
    {
        var response = await service.SuggestAsync(request, http.RequestAborted);
        log.LogInformation("suggest {requestId} {timeMs} {depth} {nodes} {tablebaseHit}",
            requestId, response.Info.TimeMs, response.Depth, response.Nodes, response.Info.TablebaseHit);
        return Results.Ok(response);
    }
    catch (ApiException e)
    {
        return Fail(e.StatusCode, e.Message);
    }
    catch (OperationCanceledException) when (!http.RequestAborted.IsCancellationRequested)
    {
        return Fail(504, "Timed out waiting for the engine.");
    }
});

app.MapPost("/v1/move/validate", (ValidateRequest request) =>
{
    try
    {
        var position = MoveSuggestService.ParsePosition(request.Position);
        return Results.Ok(new ValidateResponse(!string.IsNullOrWhiteSpace(request.Move) && MoveGenerator.IsLegal(position, request.Move.Trim())));
    }
    catch (ApiException e)
    {
        return Results.Json(new { error = e.Message }, statusCode: e.StatusCode);
    }
});

app.Run();
