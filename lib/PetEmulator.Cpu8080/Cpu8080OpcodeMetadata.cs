namespace PetEmulator.Cpu8080;

/// <summary>Addressing modes used by the Intel 8080 opcode metadata.</summary>
public enum Cpu8080AddressingMode
{
    Implied,
    Register,
    Immediate,
    Direct,
    RegisterIndirect,
    Restart,
}

/// <summary>Instruction families used by the 8080 debugger and test matrices.</summary>
public enum Cpu8080OpcodeFamily
{
    Miscellaneous,
    DataTransfer,
    Arithmetic,
    Logic,
    ControlFlow,
    Stack,
    Io,
    Interrupt,
}
