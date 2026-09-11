namespace PetEmulator.Desktop.Infrastructure;

/// <summary>
/// Finds the repo's <c>roms/</c> folder. The Avalonia previewer loads the assembly from a
/// temporary directory, so the working directory is also searched; <c>PET_EMULATOR_ROMS</c> can
/// override both locations for packaged or custom layouts.
/// </summary>
internal static class RomsRootLocator
{
    public static string Find()
    {
        var configured = Environment.GetEnvironmentVariable("PET_EMULATOR_ROMS");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var configuredPath = Path.GetFullPath(configured);
            if (IsRomsRoot(configuredPath))
                return configuredPath;

            throw new DirectoryNotFoundException(
                $"PET_EMULATOR_ROMS points to '{configuredPath}', but it is not a valid roms/ folder.");
        }

        var starts = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
        foreach (var start in starts.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "roms");
                if (IsRomsRoot(candidate))
                    return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not locate roms/ walking up from '{AppContext.BaseDirectory}' or '{Environment.CurrentDirectory}'.");
    }

    private static bool IsRomsRoot(string path) => Directory.Exists(Path.Combine(path, "pet"));
}
