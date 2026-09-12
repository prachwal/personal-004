namespace PetEmulator.CpuZ80.Disassembly;

/// <summary>
/// A pure, side-effect-free Z80 disassembler: given a byte source and an
/// address, renders one instruction as text and reports how many bytes it
/// occupies. Exists for diagnostics only - <see cref="Cpu.Z80Cpu"/>'s own
/// execution never consults this; it decodes and executes opcodes
/// directly. Built for the exact need this session kept hitting by hand
/// (manually eyeballing hex bytes against a mental opcode table while
/// tracing a real ROM's boot sequence) - see <c>Retro.Debugger</c> for the
/// step/breakpoint tool this backs.
///
/// Covers the unprefixed and CB-prefixed tables fully, the common
/// ED-prefixed opcodes (16-bit LD (nn)/IN-OUT/block/NEG/RETN/RETI/IM/
/// I-R moves/ADC-SBC HL), and DD/FD-prefixed (IX/IY) opcodes: the whole
/// documented indexed set (LD/INC/DEC/ADD/ALU on IX or (IX+d), PUSH/POP/
/// JP (IX)/LD SP,IX/EX (SP),IX) plus DD CB d op (rotate/BIT/RES/SET on
/// (IX+d)). A DD/FD-prefixed opcode that doesn't touch H/L/(HL) executes
/// identically to its unprefixed form on real hardware (the prefix is
/// simply wasted) - rendered here as that same unprefixed mnemonic with
/// the prefix's byte folded into the length, not as a separate case.
/// Undocumented IXH/IXL half-register opcodes aren't decoded (real ROMs
/// essentially never emit them) - falls back to the byte-swap-none
/// unprefixed rendering.
/// </summary>
public static class Z80Disassembler
{
    /// <summary>Reads a byte at an address - the only thing the disassembler needs from whatever's backing memory (a real <c>IBus</c>, a plain array, a snapshot).</summary>
    public delegate byte ByteReader(ushort address);

    /// <summary>Disassembles one instruction at <paramref name="address"/>.</summary>
    /// <returns>The rendered mnemonic and the instruction's length in bytes (at least 1).</returns>
    public static (string Text, int Length) Disassemble(ByteReader read, ushort address)
    {
        var op = read(address);

        if (op == 0xCB)
        {
            var sub = read((ushort)(address + 1));
            return (DecodeCb(sub), 2);
        }

        if (op == 0xED)
        {
            var sub = read((ushort)(address + 1));
            var (text, len) = DecodeEd(read, address, sub);
            return (text, len);
        }

        if (op is 0xDD or 0xFD)
            return DecodeIndexed(read, address, op == 0xDD ? "IX" : "IY");

        return DecodeBase(read, address, op);
    }

    private static string R8(int code) => code switch
    {
        0 => "B", 1 => "C", 2 => "D", 3 => "E", 4 => "H", 5 => "L", 6 => "(HL)", 7 => "A", _ => "?",
    };

    private static string Rp(int code) => code switch { 0 => "BC", 1 => "DE", 2 => "HL", 3 => "SP", _ => "?" };
    private static string Rp2(int code) => code switch { 0 => "BC", 1 => "DE", 2 => "HL", 3 => "AF", _ => "?" };

    private static string Cond(int code) => code switch
    {
        0 => "NZ", 1 => "Z", 2 => "NC", 3 => "C", 4 => "PO", 5 => "PE", 6 => "P", 7 => "M", _ => "?",
    };

    private static string Alu(int code) => code switch
    {
        0 => "ADD A,", 1 => "ADC A,", 2 => "SUB ", 3 => "SBC A,", 4 => "AND ", 5 => "XOR ", 6 => "OR ", 7 => "CP ", _ => "?",
    };

    private static ushort Imm16(ByteReader read, ushort address) => (ushort)(read((ushort)(address + 1)) | (read((ushort)(address + 2)) << 8));

    private static (string Text, int Length) DecodeBase(ByteReader read, ushort address, byte op)
    {
        var x = op >> 6;
        var y = (op >> 3) & 7;
        var z = op & 7;
        var p = y >> 1;
        var q = y & 1;

        switch (x)
        {
            case 0:
                switch (z)
                {
                    case 0:
                        return y switch
                        {
                            0 => ("NOP", 1),
                            1 => ("EX AF,AF'", 1),
                            2 => ($"DJNZ {(sbyte)read((ushort)(address + 1)) + address + 2:X4}h", 2),
                            3 => ($"JR {(sbyte)read((ushort)(address + 1)) + address + 2:X4}h", 2),
                            _ => ($"JR {Cond(y - 4)},{(sbyte)read((ushort)(address + 1)) + address + 2:X4}h", 2),
                        };
                    case 1:
                        return q == 0
                            ? ($"LD {Rp(p)},{Imm16(read, address):X4}h", 3)
                            : ($"ADD HL,{Rp(p)}", 1);
                    case 2:
                        return (p, q) switch
                        {
                            (0, 0) => ("LD (BC),A", 1),
                            (0, 1) => ("LD A,(BC)", 1),
                            (1, 0) => ("LD (DE),A", 1),
                            (1, 1) => ("LD A,(DE)", 1),
                            (2, 0) => ($"LD ({Imm16(read, address):X4}h),HL", 3),
                            (2, 1) => ($"LD HL,({Imm16(read, address):X4}h)", 3),
                            (3, 0) => ($"LD ({Imm16(read, address):X4}h),A", 3),
                            _ => ($"LD A,({Imm16(read, address):X4}h)", 3),
                        };
                    case 3:
                        return q == 0 ? ($"INC {Rp(p)}", 1) : ($"DEC {Rp(p)}", 1);
                    case 4:
                        return ($"INC {R8(y)}", 1);
                    case 5:
                        return ($"DEC {R8(y)}", 1);
                    case 6:
                        return ($"LD {R8(y)},{read((ushort)(address + 1)):X2}h", 2);
                    default: // z == 7
                        return y switch
                        {
                            0 => ("RLCA", 1), 1 => ("RRCA", 1), 2 => ("RLA", 1), 3 => ("RRA", 1),
                            4 => ("DAA", 1), 5 => ("CPL", 1), 6 => ("SCF", 1), _ => ("CCF", 1),
                        };
                }
            case 1:
                return op == 0x76 ? ("HALT", 1) : ($"LD {R8(y)},{R8(z)}", 1);
            case 2:
                return ($"{Alu(y)}{R8(z)}", 1);
            default: // x == 3
                switch (z)
                {
                    case 0:
                        return ($"RET {Cond(y)}", 1);
                    case 1:
                        return q == 0 ? ($"POP {Rp2(p)}", 1) : p switch
                        {
                            0 => ("RET", 1), 1 => ("EXX", 1), 2 => ("JP (HL)", 1), _ => ("LD SP,HL", 1),
                        };
                    case 2:
                        return ($"JP {Cond(y)},{Imm16(read, address):X4}h", 3);
                    case 3:
                        return y switch
                        {
                            0 => ($"JP {Imm16(read, address):X4}h", 3),
                            2 => ($"OUT ({read((ushort)(address + 1)):X2}h),A", 2),
                            3 => ($"IN A,({read((ushort)(address + 1)):X2}h)", 2),
                            4 => ("EX (SP),HL", 1),
                            5 => ("EX DE,HL", 1),
                            6 => ("DI", 1),
                            _ => y == 7 ? ("EI", 1) : ("DB CBh ; CB-prefix mishandled", 1),
                        };
                    case 4:
                        return ($"CALL {Cond(y)},{Imm16(read, address):X4}h", 3);
                    case 5:
                        return q == 0 ? ($"PUSH {Rp2(p)}", 1) : p == 0 ? ($"CALL {Imm16(read, address):X4}h", 3) : ($"DB {op:X2}h ; DD/ED/FD prefix", 1);
                    case 6:
                        return ($"{Alu(y)}{read((ushort)(address + 1)):X2}h", 2);
                    default: // z == 7
                        return ($"RST {y * 8:X2}h", 1);
                }
        }
    }

    private static string DecodeCb(byte op)
    {
        var x = op >> 6;
        var y = (op >> 3) & 7;
        var z = op & 7;
        return x switch
        {
            0 => y switch
            {
                0 => $"RLC {R8(z)}", 1 => $"RRC {R8(z)}", 2 => $"RL {R8(z)}", 3 => $"RR {R8(z)}",
                4 => $"SLA {R8(z)}", 5 => $"SRA {R8(z)}", 6 => $"SLL {R8(z)}", _ => $"SRL {R8(z)}",
            },
            1 => $"BIT {y},{R8(z)}",
            2 => $"RES {y},{R8(z)}",
            _ => $"SET {y},{R8(z)}",
        };
    }

    private static string DispStr(string idx, int d) => d switch
    {
        0 => $"({idx})",
        > 0 => $"({idx}+{d:X2}h)",
        _ => $"({idx}-{-d:X2}h)",
    };

    private static (string Text, int Length) DecodeIndexed(ByteReader read, ushort address, string idx)
    {
        var sub = read((ushort)(address + 1));
        if (sub == 0xCB)
        {
            var d = (sbyte)read((ushort)(address + 2));
            var op2 = read((ushort)(address + 3));
            return (DecodeIndexedCb(idx, d, op2), 4);
        }

        switch (sub)
        {
            case 0x21: return ($"LD {idx},{Imm16(read, (ushort)(address + 1)):X4}h", 4);
            case 0x22: return ($"LD ({Imm16(read, (ushort)(address + 1)):X4}h),{idx}", 4);
            case 0x2A: return ($"LD {idx},({Imm16(read, (ushort)(address + 1)):X4}h)", 4);
            case 0x23: return ($"INC {idx}", 2);
            case 0x2B: return ($"DEC {idx}", 2);
            case 0x09: return ($"ADD {idx},BC", 2);
            case 0x19: return ($"ADD {idx},DE", 2);
            case 0x29: return ($"ADD {idx},{idx}", 2);
            case 0x39: return ($"ADD {idx},SP", 2);
            case 0x34: return ($"INC {DispStr(idx, (sbyte)read((ushort)(address + 2)))}", 3);
            case 0x35: return ($"DEC {DispStr(idx, (sbyte)read((ushort)(address + 2)))}", 3);
            case 0x36:
            {
                var d = (sbyte)read((ushort)(address + 2));
                var n = read((ushort)(address + 3));
                return ($"LD {DispStr(idx, d)},{n:X2}h", 4);
            }
            case 0xE1: return ($"POP {idx}", 2);
            case 0xE5: return ($"PUSH {idx}", 2);
            case 0xE3: return ($"EX (SP),{idx}", 2);
            case 0xE9: return ($"JP ({idx})", 2);
            case 0xF9: return ($"LD SP,{idx}", 2);
        }

        var x = sub >> 6;
        var y = (sub >> 3) & 7;
        var z = sub & 7;
        if (x == 1 && sub != 0x76 && (y == 6 || z == 6))
        {
            var d = (sbyte)read((ushort)(address + 2));
            return z == 6
                ? ($"LD {R8(y)},{DispStr(idx, d)}", 3) // y == 6 excluded by sub != 0x76 above
                : ($"LD {DispStr(idx, d)},{R8(z)}", 3);
        }

        if (x == 2 && z == 6)
        {
            var d = (sbyte)read((ushort)(address + 2));
            return ($"{Alu(y)}{DispStr(idx, d)}", 3);
        }

        // Real hardware: a DD/FD prefix on any opcode that doesn't
        // reference H/L/(HL) is simply wasted - the opcode executes
        // exactly as its unprefixed form, one byte longer (the prefix
        // fetch). Render it that way instead of a misleading "not
        // decoded" placeholder.
        var (text, len) = DecodeBase(read, (ushort)(address + 1), sub);
        return (text, len + 1);
    }

    private static string DecodeIndexedCb(string idx, int d, byte op)
    {
        var x = op >> 6;
        var y = (op >> 3) & 7;
        var target = DispStr(idx, d);
        return x switch
        {
            0 => (op & 7) switch
            {
                0 => $"RLC {target}", 1 => $"RRC {target}", 2 => $"RL {target}", 3 => $"RR {target}",
                4 => $"SLA {target}", 5 => $"SRA {target}", 6 => $"SLL {target}", _ => $"SRL {target}",
            },
            1 => $"BIT {y},{target}",
            2 => $"RES {y},{target}",
            _ => $"SET {y},{target}",
        };
    }

    private static (string Text, int Length) DecodeEd(ByteReader read, ushort address, byte sub)
    {
        var x = sub >> 6;
        var y = (sub >> 3) & 7;
        var z = sub & 7;
        var p = y >> 1;
        var q = y & 1;

        if (x == 1)
        {
            switch (z)
            {
                case 0:
                    return y == 6 ? ("IN (C)", 2) : ($"IN {R8(y)},(C)", 2);
                case 1:
                    return y == 6 ? ("OUT (C),0", 2) : ($"OUT (C),{R8(y)}", 2);
                case 2:
                    return q == 0 ? ($"SBC HL,{Rp(p)}", 2) : ($"ADC HL,{Rp(p)}", 2);
                case 3:
                    return q == 0
                        ? ($"LD ({Imm16(read, (ushort)(address + 1)):X4}h),{Rp(p)}", 4)
                        : ($"LD {Rp(p)},({Imm16(read, (ushort)(address + 1)):X4}h)", 4);
                case 4:
                    return ("NEG", 2);
                case 5:
                    return y == 1 ? ("RETI", 2) : ("RETN", 2);
                case 6:
                    return ($"IM {y switch { 0 or 1 => 0, 2 or 3 => 1, _ => 2 }}", 2);
                default: // z == 7
                    return y switch
                    {
                        0 => ("LD I,A", 2), 1 => ("LD R,A", 2), 2 => ("LD A,I", 2), 3 => ("LD A,R", 2),
                        4 => ("RRD", 2), 5 => ("RLD", 2), _ => ("NOP ; ED NOP", 2),
                    };
            }
        }

        if (x == 2 && z <= 3 && y >= 4)
        {
            var names = new[,] { { "LDI", "CPI", "INI", "OUTI" }, { "LDD", "CPD", "IND", "OUTD" }, { "LDIR", "CPIR", "INIR", "OTIR" }, { "LDDR", "CPDR", "INDR", "OTDR" } };
            return (names[y - 4, z], 2);
        }

        return ($"DB EDh,{sub:X2}h ; unhandled ED", 2);
    }
}
