namespace PetEmulator.Desktop;

/// <summary>
/// Walks up from the running binary's directory to find the repo's <c>roms/</c> folder itself
/// (not a machine-specific subfolder - <see cref="PetMachineViewModel"/>/
/// <see cref="Vic20MachineViewModel"/> each combine their own "pet"/"vic20" subdirectory), the
/// runtime equivalent of <c>tests/PetEmulator.Pet.Tests/Roms/RomLocator.cs</c>.
/// </summary>
internal static class RomsRootLocator
{
    public static string Find()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            // "pet" existence just confirms this is the real roms/ folder, not that the caller
            // only wants PET ROMs.
            if (Directory.Exists(Path.Combine(dir.FullName, "roms", "pet")))
                return Path.Combine(dir.FullName, "roms");
        }

        throw new DirectoryNotFoundException(
            $"Could not locate roms/ walking up from '{AppContext.BaseDirectory}'.");
    }
}
