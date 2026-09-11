using NUnit.Framework;

namespace PetEmulator.Vic20.Tests.Roms;

/// <summary>Mirrors PetEmulator.Pet.Tests.Roms.RomLocator, pointed at roms/vic20/.</summary>
internal static class RomLocator
{
    public static string Directory(string markerFileName)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms", "vic20");
            if (File.Exists(Path.Combine(candidate, markerFileName)))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate roms/vic20/.");
    }
}
