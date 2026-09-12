namespace PetEmulator.Pet.Profiles;

public enum PetProfileFamily
{
    Pet20xx,
    Cbm30xx,
    Cbm40xx,
    Cbm80xx,
    SuperPet
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PetProfileAttribute(string id, string name, PetProfileFamily family) : Attribute
{
    public string Id { get; } = id;

    public string Name { get; } = name;

    public PetProfileFamily Family { get; } = family;

    public PetProfileStatus Status { get; init; } = PetProfileStatus.Implemented;

    public string? Description { get; init; }
}
