namespace PetEmulator.CpuZ80.Cpu;

internal readonly record struct Z80OpcodeMetadata(
    string Mnemonic,
    byte Length,
    string AddressingMode,
    byte BaseCycles)
{
    public static Z80OpcodeMetadata For(byte page, byte opcode)
    {
        var known = (page, opcode) switch
        {
            (0x00, 0x00) => new Z80OpcodeMetadata("NOP", 1, "Implied", 4),
            (0x00, 0x76) => new Z80OpcodeMetadata("HALT", 1, "Implied", 4),
            (0x00, 0xF3) => new Z80OpcodeMetadata("DI", 1, "Implied", 4),
            (0x00, 0xFB) => new Z80OpcodeMetadata("EI", 1, "Implied", 4),
            (0x00, 0xCB) => new Z80OpcodeMetadata("CB", 1, "Prefix", 4),
            (0x00, 0xED) => new Z80OpcodeMetadata("ED", 1, "Prefix", 4),
            (0x00, 0xDD) => new Z80OpcodeMetadata("DD", 1, "Prefix", 4),
            (0x00, 0xFD) => new Z80OpcodeMetadata("FD", 1, "Prefix", 4),
            (0xCB, 0x00) => new Z80OpcodeMetadata("RLC B", 2, "Register", 8),
            (0xED, 0x47) => new Z80OpcodeMetadata("LD I,A", 2, "Implied", 9),
            (0xDD, 0x21) => new Z80OpcodeMetadata("LD IX,nn", 4, "Immediate16", 14),
            (0xFD, 0x21) => new Z80OpcodeMetadata("LD IY,nn", 4, "Immediate16", 14),
            (0x00, 0xDB) => new Z80OpcodeMetadata("IN A,(n)", 2, "ImmediatePort", 11),
            (0x00, 0xD3) => new Z80OpcodeMetadata("OUT (n),A", 2, "ImmediatePort", 11),
            _ => (Z80OpcodeMetadata?)null,
        };

        return known ?? CreateDefault(page, opcode);
    }

    private static Z80OpcodeMetadata CreateDefault(byte page, byte opcode)
    {
        if (page == 0xCB)
            return CreateCbMetadata(opcode);

        if (page == 0xED)
            return new($"ED {opcode:X2}", 2, "Extended", 8);

        if (page is 0xDD or 0xFD)
        {
            if (opcode == 0xCB)
                return new($"{(page == 0xDD ? "DD" : "FD")} CB", 4, "IndexedBit", 20);

            return new($"{(page == 0xDD ? "DD" : "FD")} {opcode:X2}", 2, "Indexed", 8);
        }

        var length = opcode switch
        {
            0x01 or 0x11 or 0x21 or 0x31 or 0x22 or 0x2A or 0xC3 or 0xCD or
            0xC2 or 0xCA or 0xD2 or 0xDA or 0xE2 or 0xEA or 0xF2 or 0xFA => (byte)3,
            0x10 or 0x18 or 0x20 or 0x28 or 0x30 or 0x38 or 0x32 or 0x3A or
            0xC4 or 0xCC or 0xD4 or 0xDC or 0xE4 or 0xEC or 0xF4 or 0xFC or
            0xC6 or 0xCE or 0xD3 or 0xD6 or 0xDB or 0xDE or 0xE6 or 0xEE or
            0xF6 or 0xFE => (byte)2,
            _ => (byte)1,
        };

        var addressingMode = length switch
        {
            3 => "Immediate16",
            2 when opcode is 0xD3 or 0xDB => "ImmediatePort",
            2 => "Immediate8",
            _ => "Opcode",
        };

        return new($"OP {opcode:X2}", length, addressingMode, 4);
    }

    private static Z80OpcodeMetadata CreateCbMetadata(byte opcode)
    {
        var group = opcode >> 6;
        var operation = (opcode >> 3) & 7;
        var operand = (opcode & 7) == 6 ? "(HL)" : RegisterName(opcode & 7);
        var mnemonic = group switch
        {
            0 => $"{RotateName(operation)} {operand}",
            1 => $"BIT {operation},{operand}",
            2 => $"RES {operation},{operand}",
            _ => $"SET {operation},{operand}",
        };
        var memory = (opcode & 7) == 6;
        var cycles = group == 1
            ? (byte)(memory ? 12 : 8)
            : (byte)(memory ? 15 : 8);

        return new(mnemonic, 2, memory ? "Memory" : "Register", cycles);
    }

    private static string RotateName(int operation) => operation switch
    {
        0 => "RLC",
        1 => "RRC",
        2 => "RL",
        3 => "RR",
        4 => "SLA",
        5 => "SRA",
        6 => "SLL",
        _ => "SRL",
    };

    private static string RegisterName(int register) => register switch
    {
        0 => "B",
        1 => "C",
        2 => "D",
        3 => "E",
        4 => "H",
        5 => "L",
        _ => "A",
    };
}
