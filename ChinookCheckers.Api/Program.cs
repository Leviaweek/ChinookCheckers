using ChinookCheckers.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EngineOptions>(builder.Configuration.GetSection("Engine"));
builder.Services.AddSingleton<EngineHost>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EngineHost>());

var app = builder.Build();

app.MapGet("/healthz", (EngineHost host) =>
    host.Pool is { } pool
        ? Results.Ok(new { ok = true, workers = pool.LiveWorkers })
        : Results.Json(new { ok = false, workers = 0 }, statusCode: 503));

app.Run();
