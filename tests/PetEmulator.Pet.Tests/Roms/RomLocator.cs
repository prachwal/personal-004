using NUnit.Framework;

namespace PetEmulator.Pet.Tests.Roms;

/// <summary>
/// Walks up from the test binary directory to find a <c>roms/pet/&lt;relativePath&gt;</c> folder
/// under the repo root. Ported from personal-001's <c>TestDisks</c> helper and generalized to any
/// subfolder under <c>roms/pet/</c> (per-profile ROM sets as well as the shared test-disks fixtures).
/// </summary>
internal static class RomLocator
{
    public static string Directory(string relativePath, string markerFileName)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms", "pet", relativePath);
            if (File.Exists(Path.Combine(candidate, markerFileName)))
                return candidate;
        }

        throw new DirectoryNotFoundException($"Could not locate roms/pet/{relativePath}.");
    }
}
