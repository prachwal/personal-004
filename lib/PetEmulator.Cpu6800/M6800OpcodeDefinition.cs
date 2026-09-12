namespace PetEmulator.Cpu6800;

public delegate int M6800OpcodeHandler(M6800Cpu cpu, byte opcode);

public sealed record M6800OpcodeDefinition(
    byte Opcode,
    string Mnemonic,
    M6800AddressingMode AddressingMode,
    byte Length,
    byte BaseCycles,
    M6800OpcodeHandler Handler,
    bool IsImplemented = true);
