using PetEmulator.Pet.Profiles;

namespace PetEmulator.Pet;

/// <summary>Runtime profile catalog backed by one annotated definition class per machine profile.</summary>
public static class PetProfileCatalog
{
    public static PetProfile Pet2001_8 { get; } = new Pet2001_8Profile().Profile;
    public static PetProfile Pet2001_32 { get; } = new Pet2001_32Profile().Profile;
    public static PetProfile Cbm3008 { get; } = new Cbm3008Profile().Profile;
    public static PetProfile Cbm3016 { get; } = new Cbm3016Profile().Profile;
    public static PetProfile Cbm3032 { get; } = new Cbm3032Profile().Profile;
    public static PetProfile Cbm4032 { get; } = new Cbm4032Profile().Profile;
    public static PetProfile Cbm4008Crtc40N60 { get; } = new Cbm4008Crtc40N60Profile().Profile;
    public static PetProfile Cbm4016Crtc40N60 { get; } = new Cbm4016Crtc40N60Profile().Profile;
    public static PetProfile Cbm4032Crtc40N50 { get; } = new Cbm4032Crtc40N50Profile().Profile;
    public static PetProfile Cbm4032Crtc40B50 { get; } = new Cbm4032Crtc40B50Profile().Profile;
    public static PetProfile Cbm4032Crtc40B60 { get; } = new Cbm4032Crtc40B60Profile().Profile;
    public static PetProfile Cbm8032 { get; } = new Cbm8032Profile().Profile;
    public static PetProfile Cbm8032Crtc80B50 { get; } = new Cbm8032Crtc80B50Profile().Profile;
    public static PetProfile Cbm8016Converted80N50 { get; } = new Cbm8016Converted80N50Profile().Profile;
    public static PetProfile Converted80NUnknown { get; } = new Converted80NUnknownProfile().Profile;
    public static PetProfile Cbm8096French { get; } = new Cbm8096FrenchProfile().Profile;
    public static PetProfile Cbm8296 { get; } = new Cbm8296Profile().Profile;
    public static PetProfile SuperPet6502 { get; } = new SuperPet6502Profile().Profile;
    public static PetProfile SuperPet6809 { get; } = new SuperPet6809Profile().Profile;

    /// <summary>Legacy alias for the default CLI profile, which represents 6809 mode.</summary>
    public static PetProfile SuperPet => SuperPet6809;

    public static IReadOnlyList<PetProfile> Planned { get; } = [Cbm8096French, Cbm8296, SuperPet6502, SuperPet6809];

    public static IReadOnlyList<PetProfile> All { get; } =
    [
        Pet2001_8, Pet2001_32, Cbm3008, Cbm3016, Cbm3032,
        Cbm4008Crtc40N60, Cbm4016Crtc40N60, Cbm4032,
        Cbm4032Crtc40N50, Cbm4032Crtc40B50, Cbm4032Crtc40B60,
        Cbm8032, Cbm8032Crtc80B50, Cbm8016Converted80N50, Converted80NUnknown
    ];

    /// <summary>Profiles that passed the ROM and memory-bus checks and may be selected by a host.</summary>
    public static IReadOnlyList<PetProfile> Available { get; } = [.. All, SuperPet6502, SuperPet6809];

    public static PetProfile Find(string id) =>
        Available.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException(
            $"Unknown PET profile '{id}'. Available: {string.Join(", ", Available.Select(p => p.Id))}.");
}
