namespace PetEmulator.Core;

/// <summary>
/// Identifies an opcode together with its prefix/page.
/// Page zero represents an unprefixed opcode.
/// </summary>
public readonly record struct OpcodeKey(byte Page, byte Opcode)
{
    public static OpcodeKey Base(byte opcode) => new(0, opcode);
}
