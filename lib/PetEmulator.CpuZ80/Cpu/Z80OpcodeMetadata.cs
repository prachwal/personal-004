namespace PetEmulator.CpuZ80.Cpu;

internal readonly record struct Z80OpcodeMetadata(
    string Mnemonic,
    byte Length,
    string AddressingMode,
    byte BaseCycles)
{
    public static Z80OpcodeMetadata For(byte page, byte opcode) => (page, opcode) switch
    {
        (0x00, 0x00) => new("NOP", 1, "Implied", 4),
        (0x00, 0x76) => new("HALT", 1, "Implied", 4),
        (0x00, 0xF3) => new("DI", 1, "Implied", 4),
        (0x00, 0xFB) => new("EI", 1, "Implied", 4),
        (0x00, 0xCB) => new("CB", 1, "Prefix", 4),
        (0x00, 0xED) => new("ED", 1, "Prefix", 4),
        (0x00, 0xDD) => new("DD", 1, "Prefix", 4),
        (0x00, 0xFD) => new("FD", 1, "Prefix", 4),
        (0xCB, 0x00) => new("RLC B", 2, "Register", 8),
        (0xED, 0x47) => new("LD I,A", 2, "Implied", 9),
        (0xDD, 0x21) => new("LD IX,nn", 4, "Immediate16", 14),
        (0xFD, 0x21) => new("LD IY,nn", 4, "Immediate16", 14),
        (0x00, 0xDB) => new("IN A,(n)", 2, "ImmediatePort", 11),
        (0x00, 0xD3) => new("OUT (n),A", 2, "ImmediatePort", 11),
        _ => new($"OP {page:X2}:{opcode:X2}", 1, "Unknown", 0)
    };
}
