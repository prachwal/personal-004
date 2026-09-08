namespace Cpu6502;

public enum AddressingMode
{
    Implied,
    Accumulator,
    Immediate,
    ZeroPage,
    ZeroPageX,
    ZeroPageY,
    ZeroPageIndirect,
    Absolute,
    AbsoluteX,
    AbsoluteY,
    Indirect,
    IndirectX,
    IndirectY,
    Relative,
    Unknown
}

public delegate void OpcodeHandler(Cpu6502 cpu, byte opcode, byte cycle);

public sealed record OpcodeDefinition(
    byte Opcode,
    string Mnemonic,
    AddressingMode AddressingMode,
    byte Length,
    byte BaseCycles,
    OpcodeHandler Handler,
    bool HasPageCrossPenalty = false);
