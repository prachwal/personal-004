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
            return CreateEdMetadata(opcode);

        if (page is 0xDD or 0xFD)
            return CreateIndexedMetadata(page, opcode);

        if (page == 0x00 && CreateBaseMatrixMetadata(opcode) is { } matrix)
            return matrix;

        if (page == 0x00 && CreateBaseControlMetadata(opcode) is { } control)
            return control;

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

    private static Z80OpcodeMetadata? CreateBaseControlMetadata(byte opcode)
    {
        if ((opcode & 0xC7) == 0xC1)
            return new($"POP {PairName((opcode >> 4) & 3)}", 1, "Stack", 10);

        if ((opcode & 0xC7) == 0xC5)
            return new($"PUSH {PairName((opcode >> 4) & 3)}", 1, "Stack", 11);

        if ((opcode & 0xCF) == 0x01)
            return new($"LD {PairName((opcode >> 4) & 3)},nn", 3, "Immediate16", 10);

        if ((opcode & 0xCF) == 0x09)
            return new($"ADD HL,{PairName((opcode >> 4) & 3)}", 1, "RegisterPair", 11);

        if ((opcode & 0xC7) == 0x03)
            return new($"{((opcode & 8) == 0 ? "INC" : "DEC")} {PairName((opcode >> 4) & 3)}", 1, "RegisterPair", 6);

        if ((opcode & 0xC7) is 0x04 or 0x05)
        {
            var register = BaseRegisterName((opcode >> 3) & 7);
            var memory = ((opcode >> 3) & 7) == 6;
            return new($"{((opcode & 1) == 0 ? "INC" : "DEC")} {register}", 1, memory ? "Memory" : "Register", (byte)(memory ? 11 : 4));
        }

        if ((opcode & 0xC7) == 0x06)
            return null;

        if ((opcode & 0xC7) == 0xC6)
            return new(AluMnemonic((opcode >> 3) & 7, "n"), 2, "Immediate8", 7);

        return opcode switch
        {
            0x08 => new("EX AF,AF'", 1, "Implied", 4),
            0x02 => new("LD (BC),A", 1, "Memory", 7),
            0x0A => new("LD A,(BC)", 1, "Memory", 7),
            0x12 => new("LD (DE),A", 1, "Memory", 7),
            0x1A => new("LD A,(DE)", 1, "Memory", 7),
            0x22 => new("LD (nn),HL", 3, "Absolute16", 16),
            0x2A => new("LD HL,(nn)", 3, "Absolute16", 16),
            0x32 => new("LD (nn),A", 3, "Absolute16", 13),
            0x3A => new("LD A,(nn)", 3, "Absolute16", 13),
            0x07 => new("RLCA", 1, "Implied", 4),
            0x0F => new("RRCA", 1, "Implied", 4),
            0x17 => new("RLA", 1, "Implied", 4),
            0x1F => new("RRA", 1, "Implied", 4),
            0x27 => new("DAA", 1, "Implied", 4),
            0x2F => new("CPL", 1, "Implied", 4),
            0x37 => new("SCF", 1, "Implied", 4),
            0x3F => new("CCF", 1, "Implied", 4),
            0x10 => new("DJNZ e", 2, "Relative", 13),
            0x18 => new("JR e", 2, "Relative", 12),
            0x20 or 0x28 or 0x30 or 0x38 => new($"JR {ConditionName((opcode >> 3) & 3)},e", 2, "Relative", 12),
            0xC3 => new("JP nn", 3, "Absolute16", 10),
            0xC2 or 0xCA or 0xD2 or 0xDA or 0xE2 or 0xEA or 0xF2 or 0xFA => new($"JP {ConditionName((opcode >> 3) & 7)},nn", 3, "Absolute16", 10),
            0xCD => new("CALL nn", 3, "Absolute16", 17),
            0xC4 or 0xCC or 0xD4 or 0xDC or 0xE4 or 0xEC or 0xF4 or 0xFC => new($"CALL {ConditionName((opcode >> 3) & 7)},nn", 3, "Absolute16", 17),
            0xC9 => new("RET", 1, "Implied", 10),
            0xC0 or 0xC8 or 0xD0 or 0xD8 or 0xE0 or 0xE8 or 0xF0 or 0xF8 => new($"RET {ConditionName((opcode >> 3) & 7)}", 1, "Implied", 11),
            0xE9 => new("JP (HL)", 1, "Register", 4),
            0xE3 => new("EX (SP),HL", 1, "StackMemory", 19),
            0xE5 => new("PUSH HL", 1, "Stack", 11),
            0xE1 => new("POP HL", 1, "Stack", 10),
            0xF9 => new("LD SP,HL", 1, "RegisterPair", 6),
            0xD9 => new("EXX", 1, "Implied", 4),
            0xEB => new("EX DE,HL", 1, "RegisterPair", 4),
            0xC7 or 0xCF or 0xD7 or 0xDF or 0xE7 or 0xEF or 0xF7 or 0xFF => new($"RST {opcode & 0x38:X2}H", 1, "Restart", 11),
            _ => null,
        };
    }

    private static string ConditionName(int condition) => condition switch
    {
        0 => "NZ",
        1 => "Z",
        2 => "NC",
        3 => "C",
        4 => "PO",
        5 => "PE",
        6 => "P",
        _ => "M",
    };

    private static Z80OpcodeMetadata? CreateBaseMatrixMetadata(byte opcode)
    {
        if ((opcode & 0xC0) == 0x40 && opcode != 0x76)
        {
            var destination = BaseRegisterName((opcode >> 3) & 7);
            var source = BaseRegisterName(opcode & 7);
            var memory = ((opcode >> 3) & 7) == 6 || (opcode & 7) == 6;
            return new($"LD {destination},{source}", 1, memory ? "Memory" : "Register", (byte)(memory ? 7 : 4));
        }

        if ((opcode & 0xC7) == 0x06)
        {
            var register = BaseRegisterName((opcode >> 3) & 7);
            var memory = ((opcode >> 3) & 7) == 6;
            return new($"LD {register},n", 2, memory ? "MemoryImmediate8" : "Immediate8", (byte)(memory ? 10 : 7));
        }

        if ((opcode & 0xC0) == 0x80)
        {
            var source = BaseRegisterName(opcode & 7);
            var memory = (opcode & 7) == 6;
            return new($"{AluMnemonic((opcode >> 3) & 7, source)}", 1, memory ? "Memory" : "Register", (byte)(memory ? 7 : 4));
        }

        return null;
    }

    private static Z80OpcodeMetadata CreateIndexedMetadata(byte page, byte opcode)
    {
        var index = page == 0xDD ? "IX" : "IY";
        var prefix = page == 0xDD ? "DD" : "FD";

        if (opcode == 0xCB)
            return new($"{prefix} CB", 4, "IndexedBit", 20);

        if ((opcode & 0xC0) == 0x40 && opcode != 0x76)
        {
            var destination = IndexedRegisterName((opcode >> 3) & 7, index, opcode & 7);
            var source = IndexedRegisterName(opcode & 7, index, (opcode >> 3) & 7);
            var memory = ((opcode >> 3) & 7) == 6 || (opcode & 7) == 6;
            return new($"LD {destination},{source}", (byte)(memory ? 3 : 2), memory ? "IndexedMemory" : "IndexedRegister", (byte)(memory ? 19 : 8));
        }

        if ((opcode & 0xC7) == 0x06 && ((opcode >> 3) & 7) is 4 or 5)
            return new($"LD {IndexedRegisterName((opcode >> 3) & 7, index, 0)},n", 3, "IndexedImmediate8", 11);

        if (((opcode & 0xC7) is 0x04 or 0x05) && ((opcode >> 3) & 7) is 4 or 5)
            return new($"{((opcode & 1) == 0 ? "INC" : "DEC")} {IndexedRegisterName((opcode >> 3) & 7, index, 0)}", 2, "IndexedRegister", 8);

        if ((opcode & 0xC0) == 0x80)
        {
            var sourceCode = opcode & 7;
            var source = IndexedRegisterName(sourceCode, index, sourceCode);
            var memory = sourceCode == 6;
            return new($"{AluName((opcode >> 3) & 7)} {source}", (byte)(memory ? 3 : 2), memory ? "IndexedMemory" : "IndexedRegister", (byte)(memory ? 19 : 8));
        }

        return opcode switch
        {
            0x21 => new($"LD {index},nn", 4, "Immediate16", 14),
            0x22 => new($"LD (nn),{index}", 4, "Absolute16", 20),
            0x2A => new($"LD {index},(nn)", 4, "Absolute16", 20),
            0x23 => new($"INC {index}", 2, "IndexedRegister", 10),
            0x2B => new($"DEC {index}", 2, "IndexedRegister", 10),
            0x09 or 0x19 or 0x29 or 0x39 => new($"ADD {index},{IndexedPairName((opcode >> 4) & 3, index)}", 2, "IndexedRegisterPair", 15),
            0xE9 => new($"JP ({index})", 2, "IndexedRegister", 8),
            0xE5 => new($"PUSH {index}", 2, "IndexedRegister", 15),
            0xE1 => new($"POP {index}", 2, "IndexedRegister", 14),
            0xE3 => new($"EX (SP),{index}", 2, "IndexedMemory", 23),
            0xF9 => new($"LD SP,{index}", 2, "IndexedRegister", 10),
            0x36 => new($"LD ({index}+d),n", 4, "IndexedImmediate8", 19),
            0x34 => new($"INC ({index}+d)", 3, "IndexedMemory", 23),
            0x35 => new($"DEC ({index}+d)", 3, "IndexedMemory", 23),
            _ => CreateIndexedFallback(page, opcode),
        };
    }

    private static Z80OpcodeMetadata CreateIndexedFallback(byte page, byte opcode)
    {
        var prefix = page == 0xDD ? "DD" : "FD";
        var baseMetadata = For(0, opcode);
        return new($"{prefix} {baseMetadata.Mnemonic}", (byte)(baseMetadata.Length + 1), "Indexed", (byte)(baseMetadata.BaseCycles + 4));
    }

    private static string IndexedRegisterName(int register, string index, int otherRegister) => register switch
    {
        4 when otherRegister != 6 => $"{index}H",
        5 when otherRegister != 6 => $"{index}L",
        6 => $"({index}+d)",
        _ => RegisterName(register),
    };

    private static string BaseRegisterName(int register) => register == 6 ? "(HL)" : RegisterName(register);

    private static string IndexedPairName(int pair, string index) => pair == 2 ? index : PairName(pair);

    private static string AluName(int operation) => operation switch
    {
        0 => "ADD A,",
        1 => "ADC A,",
        2 => "SUB ",
        3 => "SBC A,",
        4 => "AND ",
        5 => "XOR ",
        6 => "OR ",
        _ => "CP ",
    };

    private static string AluMnemonic(int operation, string source) => operation switch
    {
        0 => $"ADD A,{source}",
        1 => $"ADC A,{source}",
        2 => $"SUB {source}",
        3 => $"SBC A,{source}",
        4 => $"AND {source}",
        5 => $"XOR {source}",
        6 => $"OR {source}",
        _ => $"CP {source}",
    };

    private static Z80OpcodeMetadata CreateEdMetadata(byte opcode)
    {
        if ((opcode & 0xC7) == 0x40)
        {
            var register = RegisterName((opcode >> 3) & 7);
            return new(
                (opcode & 0x38) == 0x38 ? "IN (C)" : $"IN {register},(C)",
                2,
                "RegisterPort",
                12);
        }

        if ((opcode & 0xC7) == 0x41)
        {
            var register = RegisterName((opcode >> 3) & 7);
            return new(
                (opcode & 0x38) == 0x38 ? "OUT (C),0" : $"OUT (C),{register}",
                2,
                "RegisterPort",
                12);
        }

        if ((opcode & 0xCF) == 0x42 || (opcode & 0xCF) == 0x4A)
        {
            var pair = PairName((opcode >> 4) & 3);
            var operation = (opcode & 0x0F) == 0x0A ? "ADC" : "SBC";
            return new($"{operation} HL,{pair}", 2, "RegisterPair", 15);
        }

        if ((opcode & 0xCF) == 0x43 || (opcode & 0xCF) == 0x4B)
        {
            var pair = PairName((opcode >> 4) & 3);
            var operation = (opcode & 0x0F) == 0x0B ? $"LD {pair},(nn)" : $"LD (nn),{pair}";
            return new(operation, 4, "Absolute16", 20);
        }

        if ((opcode & 0xC7) == 0x44)
            return new("NEG", 2, "Implied", 8);

        if ((opcode & 0xC7) == 0x45)
            return new(opcode == 0x4D ? "RETI" : "RETN", 2, "Implied", 14);

        if ((opcode & 0xC7) == 0x46)
            return new($"IM {InterruptMode(opcode)}", 2, "Implied", 8);

        return opcode switch
        {
            0x47 => new("LD I,A", 2, "Implied", 9),
            0x4F => new("LD R,A", 2, "Implied", 9),
            0x57 => new("LD A,I", 2, "Implied", 9),
            0x5F => new("LD A,R", 2, "Implied", 9),
            0x67 => new("RRD", 2, "Memory", 18),
            0x6F => new("RLD", 2, "Memory", 18),
            0xA0 => new("LDI", 2, "Block", 16),
            0xA1 => new("CPI", 2, "Block", 16),
            0xA2 => new("INI", 2, "Block", 16),
            0xA3 => new("OUTI", 2, "Block", 16),
            0xA8 => new("LDD", 2, "Block", 16),
            0xA9 => new("CPD", 2, "Block", 16),
            0xAA => new("IND", 2, "Block", 16),
            0xAB => new("OUTD", 2, "Block", 16),
            0xB0 => new("LDIR", 2, "BlockRepeat", 21),
            0xB1 => new("CPIR", 2, "BlockRepeat", 21),
            0xB2 => new("INIR", 2, "BlockRepeat", 21),
            0xB3 => new("OTIR", 2, "BlockRepeat", 21),
            0xB8 => new("LDDR", 2, "BlockRepeat", 21),
            0xB9 => new("CPDR", 2, "BlockRepeat", 21),
            0xBA => new("INDR", 2, "BlockRepeat", 21),
            0xBB => new("OTDR", 2, "BlockRepeat", 21),
            _ => new($"ED {opcode:X2}", 2, "Extended", 8),
        };
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

    private static string PairName(int pair) => pair switch
    {
        0 => "BC",
        1 => "DE",
        2 => "HL",
        _ => "SP",
    };

    private static int InterruptMode(byte opcode) => opcode switch
    {
        0x5E or 0x7E => 2,
        0x56 or 0x76 => 1,
        _ => 0,
    };
}
