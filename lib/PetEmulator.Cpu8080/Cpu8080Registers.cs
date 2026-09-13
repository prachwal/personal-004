namespace PetEmulator.Cpu8080;

/// <summary>Immutable register view useful for tests and debugger adapters.</summary>
public readonly record struct Cpu8080Registers(
    byte A,
    byte B,
    byte C,
    byte D,
    byte E,
    byte H,
    byte L,
    ushort PC,
    ushort SP,
    byte Flags);
