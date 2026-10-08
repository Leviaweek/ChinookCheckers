using ChinookCheckers.Api;
using ChinookCheckers.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EngineOptions>(builder.Configuration.GetSection("Engine"));
builder.Services.AddSingleton<EngineHost>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EngineHost>());
builder.Services.AddMemoryCache(options => options.SizeLimit = 20000);
builder.Services.AddSingleton<MoveSuggestService>();

var app = builder.Build();

app.MapGet("/healthz", (EngineHost host) =>
    host.Pool is { } pool
        ? Results.Ok(new { ok = true, workers = pool.LiveWorkers })
        : Results.Json(new { ok = false, workers = 0 }, statusCode: 503));

app.MapPost("/v1/move/suggest", async (SuggestRequest request, MoveSuggestService service, HttpContext http) =>
{
    try
    {
        return Results.Ok(await service.SuggestAsync(request, http.RequestAborted));
    }
    catch (ApiException e)
    {
        return Results.Json(new { error = e.Message }, statusCode: e.StatusCode);
    }
    catch (OperationCanceledException) when (!http.RequestAborted.IsCancellationRequested)
    {
        return Results.Json(new { error = "Timed out waiting for the engine." }, statusCode: 504);
    }
});

app.Run();
