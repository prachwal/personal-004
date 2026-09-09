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
    public static IReadOnlyList<PetRomImage> Load(string romDirectory, IReadOnlyList<PetRomRequirement> requirements)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romDirectory);
        ArgumentNullException.ThrowIfNull(requirements);

        var images = new List<PetRomImage>(requirements.Count);
        foreach (var requirement in requirements)
        {
            ArgumentNullException.ThrowIfNull(requirement);
            if (Path.GetFileName(requirement.Path) != requirement.Path)
                throw new ArgumentException("PET ROM manifest entries must be file names.", nameof(requirements));

            var path = Path.Combine(romDirectory, requirement.Path);
            if (!File.Exists(path))
                throw new FileNotFoundException($"PET ROM is missing: {path}.", path);

            var image = File.ReadAllBytes(path);
            if ((uint)image.Length != requirement.Length)
                throw new InvalidDataException($"PET ROM {path} has {image.Length} B; expected {requirement.Length} B.");

            images.Add(new PetRomImage(requirement, image));
        }

        return images;
    }
}
