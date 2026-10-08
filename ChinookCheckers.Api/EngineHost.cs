using ChinookCheckers.Engine;
using Microsoft.Extensions.Options;

namespace ChinookCheckers.Api;

public sealed class EngineOptions
{
    public string DllPath { get; set; } = "";
    public string DbPath { get; set; } = "";
    public int DbMbytes { get; set; } = 512;
    public int Workers { get; set; } = 2;

    /// <summary>Folder (relative to the app folder) used as the engine's user profile; must be writable.</summary>
    public string ProfileDir { get; set; } = "profile";
}

/// <summary>Creates and warms up the engine pool at startup; the DLLs are never loaded per request.</summary>
public sealed class EngineHost(IOptions<EngineOptions> options, IHostEnvironment environment, ILogger<EngineHost> log) : IHostedService
{
    public EnginePool? Pool { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var o = options.Value;
        UseAppLocalProfile(Path.GetFullPath(o.ProfileDir, environment.ContentRootPath));

        Pool = await Task.Run(() => EnginePool.Create(o.DllPath, o.DbPath, o.DbMbytes, o.Workers), cancellationToken);
    }

    // KingsRow resolves the user's Documents and AppData folders from the profile environment variables and
    // crashes (stack cookie failure) when they do not exist. Service accounts such as an IIS app pool identity
    // have no usable profile (it resolves to the SYSTEM profile, which they cannot write to), so give the
    // engine a profile of its own.
    private void UseAppLocalProfile(string profile)
    {
        var roaming = Path.Combine(profile, "AppData", "Roaming");
        var local = Path.Combine(profile, "AppData", "Local");

        foreach (var folder in new[] { Path.Combine(profile, "Documents"), roaming, local })
            Directory.CreateDirectory(folder);

        Environment.SetEnvironmentVariable("USERPROFILE", profile);
        Environment.SetEnvironmentVariable("APPDATA", roaming);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", local);
        log.LogInformation("Engine profile folder: {profile}", profile);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Pool?.Dispose();
        return Task.CompletedTask;
    }
}
