using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PetEmulator.Pet.Roms;

/// <summary>
/// Ported from personal-001's PetRomLoader. Source wraps each image in
/// Emulator.Core.Memory.Rom (an IBusTarget) which does not exist in PetEmulator.Core (that
/// abstraction belongs to memory-mapped-device wiring, out of scope for this port) - so
/// PetRomImage carries the raw bytes directly instead. Behavior (existence/length validation) is
/// otherwise ported verbatim; no checksum verification is added (known gap, out of scope here).
/// </summary>
public sealed record PetRomImage(PetRomRequirement Requirement, byte[] Data);

public static class PetRomLoader
{
    public static IReadOnlyList<PetRomImage> Load(string romDirectory, IReadOnlyList<PetRomRequirement> requirements, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romDirectory);
        ArgumentNullException.ThrowIfNull(requirements);

        var log = logger ?? NullLogger.Instance;
        log.LogInformation("Loading {Count} ROMs from '{Directory}'.", requirements.Count, romDirectory);
        var images = new List<PetRomImage>(requirements.Count);
        foreach (var requirement in requirements)
        {
            ArgumentNullException.ThrowIfNull(requirement);
            if (Path.GetFileName(requirement.Path) != requirement.Path)
                throw new ArgumentException("PET ROM manifest entries must be file names.", nameof(requirements));

            var path = Path.Combine(romDirectory, requirement.Path);
            if (!File.Exists(path))
            {
                var present = DescribeDirectory(romDirectory);
                log.LogError("ROM missing: '{Path}'. Directory contents: {Contents}.", path, present);
                throw new FileNotFoundException(
                    $"PET ROM is missing: {path}. Directory '{romDirectory}' contains: {present}.", path);
            }

            var image = File.ReadAllBytes(path);
            if ((uint)image.Length != requirement.Length)
            {
                log.LogError("ROM size mismatch: '{Path}' has {Actual} B; expected {Expected} B.", path, image.Length, requirement.Length);
                throw new InvalidDataException($"PET ROM {path} has {image.Length} B; expected {requirement.Length} B.");
            }

            log.LogDebug("ROM ok: '{Path}' ({Size} B).", path, image.Length);
            images.Add(new PetRomImage(requirement, image));
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
