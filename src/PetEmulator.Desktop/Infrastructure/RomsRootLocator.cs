using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;

namespace PetEmulator.Desktop.Infrastructure;

/// <summary>
/// Finds the repo's <c>roms/</c> folder. The Avalonia previewer loads the assembly from a
/// temporary directory, so the working directory is also searched; <c>PET_EMULATOR_ROMS</c> can
/// override both locations for packaged or custom layouts. Every step is logged - a wrong ROM
/// folder used to fail later with a bare FileNotFoundException and no hint where lookup ran.
/// </summary>
internal static class RomsRootLocator
{
    private static readonly ILogger Log = EmulatorLogging.CreateLogger("Roms");

    public static string Find()
    {
        var configured = Environment.GetEnvironmentVariable("PET_EMULATOR_ROMS");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var configuredPath = Path.GetFullPath(configured);
            Log.LogDebug("PET_EMULATOR_ROMS={Configured} resolved to {Resolved}.", configured, configuredPath);
            if (IsRomsRoot(configuredPath))
            {
                Log.LogInformation("Using configured ROMs root: {RomsRoot}.", configuredPath);
                return configuredPath;
            }

            var failure = new DirectoryNotFoundException(
                $"PET_EMULATOR_ROMS points to '{configuredPath}', but it is not a valid roms/ folder.");
            Log.LogError(failure, "Configured ROMs root is not a valid roms/ folder.");
            throw failure;
        }

        var starts = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
        foreach (var start in starts.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "roms");
                if (IsRomsRoot(candidate))
                {
                    Log.LogInformation("Found ROMs root {Candidate} walking up from {Start}.", candidate, start);
                    return candidate;
                }

                Log.LogDebug("Not a ROMs root (no pet/ subfolder): {Candidate}.", candidate);
            }
        }

        var notFound = new DirectoryNotFoundException(
            $"Could not locate roms/ walking up from '{AppContext.BaseDirectory}' or '{Environment.CurrentDirectory}'.");
        Log.LogError(notFound, "ROMs root lookup failed.");
        throw notFound;
    }

    private static bool IsRomsRoot(string path) => Directory.Exists(Path.Combine(path, "pet"));
}
