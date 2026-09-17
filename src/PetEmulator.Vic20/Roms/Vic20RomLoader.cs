using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PetEmulator.Vic20.Roms;

/// <summary>Ported from PetEmulator.Pet.Roms.PetRomLoader (mirrors it exactly - existence/length
/// validation, no checksum verification) rather than shared: the two are ~20 lines each and live
/// in different projects with no natural common home short of adding a Core dependency neither
/// otherwise needs for one static helper - not worth it (see docs/vic20/migration-plan.md step 0,
/// which DID share PetEmulator.Core.BusAccess/Observer - that one earns a shared home because it's
/// a runtime hot-path contract, not a one-shot file loader).</summary>
public sealed record Vic20RomImage(Vic20RomRequirement Requirement, byte[] Data);

public static class Vic20RomLoader
{
    public static IReadOnlyList<Vic20RomImage> Load(string romDirectory, IReadOnlyList<Vic20RomRequirement> requirements, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romDirectory);
        ArgumentNullException.ThrowIfNull(requirements);

        var log = logger ?? NullLogger.Instance;
        log.LogInformation("Loading {Count} ROMs from '{Directory}'.", requirements.Count, romDirectory);
        var images = new List<Vic20RomImage>(requirements.Count);
        foreach (var requirement in requirements)
        {
            ArgumentNullException.ThrowIfNull(requirement);
            if (Path.GetFileName(requirement.Path) != requirement.Path)
                throw new ArgumentException("VIC-20 ROM manifest entries must be file names.", nameof(requirements));

            var path = Path.Combine(romDirectory, requirement.Path);
            if (!File.Exists(path))
            {
                var present = DescribeDirectory(romDirectory);
                log.LogError("ROM missing: '{Path}'. Directory contents: {Contents}.", path, present);
                throw new FileNotFoundException(
                    $"VIC-20 ROM is missing: {path}. Directory '{romDirectory}' contains: {present}.", path);
            }

            var image = File.ReadAllBytes(path);
            if ((uint)image.Length != requirement.Length)
            {
                log.LogError("ROM size mismatch: '{Path}' has {Actual} B; expected {Expected} B.", path, image.Length, requirement.Length);
                throw new InvalidDataException($"VIC-20 ROM {path} has {image.Length} B; expected {requirement.Length} B.");
            }

            log.LogDebug("ROM ok: '{Path}' ({Size} B).", path, image.Length);
            images.Add(new Vic20RomImage(requirement, image));
        }

        log.LogInformation("All {Count} ROMs loaded from '{Directory}'.", images.Count, romDirectory);
        return images;
    }

    private static string DescribeDirectory(string romDirectory)
    {
        try
        {
            if (!Directory.Exists(romDirectory))
                return "(directory does not exist)";
            var files = Directory.GetFiles(romDirectory)
                .Select(file => $"{Path.GetFileName(file)} ({new FileInfo(file).Length} B)");
            return string.Join(", ", files.DefaultIfEmpty("(empty)"));
        }
        catch (Exception ex)
        {
            return $"(listing failed: {ex.Message})";
        }
    }
}
