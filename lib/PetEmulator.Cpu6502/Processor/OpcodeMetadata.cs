namespace PetEmulator.Cpu6502;

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
    ZeroPageRelative,
    Unknown
}

internal delegate void OpcodeHandler(Cpu6502 cpu, byte opcode, byte cycle);
