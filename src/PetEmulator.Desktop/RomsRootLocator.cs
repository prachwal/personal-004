namespace PetEmulator.Desktop;

/// <summary>
/// Walks up from the running binary's directory to find the repo's <c>roms/</c> folder, the
/// runtime equivalent of <c>tests/PetEmulator.Pet.Tests/Roms/RomLocator.cs</c>.
/// </summary>
internal static class RomsRootLocator
{
    public static string Find()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "roms", "pet")))
                return Path.Combine(dir.FullName, "roms", "pet");
        }

        throw new DirectoryNotFoundException(
            $"Could not locate roms/pet/ walking up from '{AppContext.BaseDirectory}'.");
    }
}
