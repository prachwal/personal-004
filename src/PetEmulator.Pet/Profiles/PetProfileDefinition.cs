using PetEmulator.Pet.Roms;

namespace PetEmulator.Pet.Profiles;

public abstract class PetProfileDefinition
{
    protected PetProfileDefinition(
        string basicVersion,
        int columns,
        int rows,
        uint ramSize,
        uint videoRamStart,
        uint videoRamLength,
        bool requiresCrtc,
        string characterRomPath,
        IReadOnlyList<PetRomRequirement> romManifest,
        PetCursorTracking cursorTracking,
        PetKeyboardLayout keyboardLayout)
    {
        var metadata = GetType().GetCustomAttributes(typeof(PetProfileAttribute), false)
            .OfType<PetProfileAttribute>()
            .SingleOrDefault()
            ?? throw new InvalidOperationException($"Profile definition {GetType().Name} is missing [PetProfile].");

        Profile = new PetProfile(
            metadata.Id,
            metadata.Name,
            basicVersion,
            columns,
            rows,
            ramSize,
            videoRamStart,
            videoRamLength,
            requiresCrtc,
            characterRomPath,
            romManifest,
            metadata.Status,
            cursorTracking,
            keyboardLayout);
    }

    public PetProfile Profile { get; }
}
