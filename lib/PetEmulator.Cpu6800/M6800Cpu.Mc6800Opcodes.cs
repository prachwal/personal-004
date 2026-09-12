using PetEmulator.Core;

namespace PetEmulator.Cpu6800;

/// <summary>Concrete Motorola MC6800 processor with its own opcode map.</summary>
public partial class M6800Cpu
{
    private void BuildOpcodeTable()
    {
        for (byte opcode = 0; ; opcode++)
        {
            Set(opcode, $"OP ${opcode:X2}", M6800AddressingMode.Unknown, 1, 2, UnsupportedOpcode);
            if (opcode == byte.MaxValue)
                break;
        }

        Set(0x01, "NOP", M6800AddressingMode.Inherent, 1, 2, () => 2);
        Set(0x06, "TAP", M6800AddressingMode.Inherent, 1, 2, Tap);
        Set(0x07, "TPA", M6800AddressingMode.Inherent, 1, 2, Tpa);
        Set(0x08, "INX", M6800AddressingMode.Inherent, 1, 4, Inx);
        Set(0x09, "DEX", M6800AddressingMode.Inherent, 1, 4, Dex);
        Set(0x0A, "CLV", M6800AddressingMode.Inherent, 1, 2, () => SetFlag(v => v.V = false));
        Set(0x0B, "SEV", M6800AddressingMode.Inherent, 1, 2, () => SetFlag(v => v.V = true));
        Set(0x0C, "CLC", M6800AddressingMode.Inherent, 1, 2, () => SetFlag(v => v.C = false));
        Set(0x0D, "SEC", M6800AddressingMode.Inherent, 1, 2, () => SetFlag(v => v.C = true));
        Set(0x0E, "CLI", M6800AddressingMode.Inherent, 1, 2, () => SetFlag(v => v.I = false));
        Set(0x0F, "SEI", M6800AddressingMode.Inherent, 1, 2, () => SetFlag(v => v.I = true));

        Set(0x10, "SBA", M6800AddressingMode.Inherent, 1, 2, Sba);
        Set(0x11, "CBA", M6800AddressingMode.Inherent, 1, 2, Cba);
        Set(0x16, "TAB", M6800AddressingMode.Inherent, 1, 2, Tab);
        Set(0x17, "TBA", M6800AddressingMode.Inherent, 1, 2, Tba);
        Set(0x19, "DAA", M6800AddressingMode.Inherent, 1, 2, Daa);
        Set(0x1B, "ABA", M6800AddressingMode.Inherent, 1, 2, Aba);

        SetBranches();
        SetStackAndControl();
        SetUnary();
        SetAccumulatorOperations();
        SetMemoryOperations();
        SetIndexOperations();
    }

    private void SetBranches()
    {
        Set(0x20, "BRA", M6800AddressingMode.Relative, 2, 4, () => Bra(Fetch()));
        Set(0x22, "BHI", M6800AddressingMode.Relative, 2, 4, () => Bhi(Fetch()));
        Set(0x23, "BLS", M6800AddressingMode.Relative, 2, 4, () => Bls(Fetch()));
        Set(0x24, "BCC", M6800AddressingMode.Relative, 2, 4, () => Bcc(Fetch()));
        Set(0x25, "BCS", M6800AddressingMode.Relative, 2, 4, () => Bcs(Fetch()));
        Set(0x26, "BNE", M6800AddressingMode.Relative, 2, 4, () => Bne(Fetch()));
        Set(0x27, "BEQ", M6800AddressingMode.Relative, 2, 4, () => Beq(Fetch()));
        Set(0x28, "BVC", M6800AddressingMode.Relative, 2, 4, () => Bvc(Fetch()));
        Set(0x29, "BVS", M6800AddressingMode.Relative, 2, 4, () => Bvs(Fetch()));
        Set(0x2A, "BPL", M6800AddressingMode.Relative, 2, 4, () => Bpl(Fetch()));
        Set(0x2B, "BMI", M6800AddressingMode.Relative, 2, 4, () => Bmi(Fetch()));
        Set(0x2C, "BGE", M6800AddressingMode.Relative, 2, 4, () => Bge(Fetch()));
        Set(0x2D, "BLT", M6800AddressingMode.Relative, 2, 4, () => Blt(Fetch()));
        Set(0x2E, "BGT", M6800AddressingMode.Relative, 2, 4, () => Bgt(Fetch()));
        Set(0x2F, "BLE", M6800AddressingMode.Relative, 2, 4, () => Ble(Fetch()));
    }

    private void SetStackAndControl()
    {
        Set(0x30, "TSX", M6800AddressingMode.Inherent, 1, 4, Tsx);
        Set(0x31, "INS", M6800AddressingMode.Inherent, 1, 4, () => { State.StackPointer++; return 4; });
        Set(0x32, "PULA", M6800AddressingMode.Inherent, 1, 4, () => { State.A = PopStack8(); return 4; });
        Set(0x33, "PULB", M6800AddressingMode.Inherent, 1, 4, () => { State.B = PopStack8(); return 4; });
        Set(0x34, "DES", M6800AddressingMode.Inherent, 1, 4, () => { State.StackPointer--; return 4; });
        Set(0x35, "TXS", M6800AddressingMode.Inherent, 1, 4, Txs);
        Set(0x36, "PSHA", M6800AddressingMode.Inherent, 1, 4, () => { PushStack8(State.A); return 4; });
        Set(0x37, "PSHB", M6800AddressingMode.Inherent, 1, 4, () => { PushStack8(State.B); return 4; });
        Set(0x39, "RTS", M6800AddressingMode.Inherent, 1, 5, Rts);
        Set(0x3B, "RTI", M6800AddressingMode.Inherent, 1, 10, Rti);
        Set(0x3E, "WAI", M6800AddressingMode.Inherent, 1, 9, Wai);
        Set(0x3F, "SWI", M6800AddressingMode.Inherent, 1, 12, Swi);
    }

    private void SetUnary()
    {
        (byte op, string name, Func<int> action)[] accA =
        [
            (0x40, "NEGA", NegA), (0x43, "COMA", ComA), (0x44, "LSRA", LsrA),
            (0x46, "RORA", RorA), (0x47, "ASRA", AsrA), (0x48, "ASLA", AslA),
            (0x49, "ROLA", RolA), (0x4A, "DECA", DecA), (0x4C, "INCA", IncA),
            (0x4D, "TSTA", TstA), (0x4F, "CLRA", ClrA),
        ];
        (byte op, string name, Func<int> action)[] accB =
        [
            (0x50, "NEGB", NegB), (0x53, "COMB", ComB), (0x54, "LSRB", LsrB),
            (0x56, "RORB", RorB), (0x57, "ASRB", AsrB), (0x58, "ASLB", AslB),
            (0x59, "ROLB", RolB), (0x5A, "DECB", DecB), (0x5C, "INCB", IncB),
            (0x5D, "TSTB", TstB), (0x5F, "CLRB", ClrB),
        ];
        foreach (var item in accA.Concat(accB))
            Set(item.op, item.name, M6800AddressingMode.Inherent, 1, 2, item.action);

        SetMemoryUnary(0x00, "NEG", Neg);
        SetMemoryUnary(0x03, "COM", Com);
        SetMemoryUnary(0x04, "LSR", Lsr);
        SetMemoryUnary(0x06, "ROR", Ror);
        SetMemoryUnary(0x07, "ASR", Asr);
        SetMemoryUnary(0x08, "ASL", Asl);
        SetMemoryUnary(0x09, "ROL", Rol);
        SetMemoryUnary(0x0A, "DEC", Dec);
        SetMemoryUnary(0x0C, "INC", Inc);
        SetMemoryUnary(0x0D, "TST", Tst);
        SetMemoryUnary(0x0F, "CLR", Clr);
    }

    private void SetMemoryUnary(byte opcode, string name, Func<ushort, int> action)
    {
        Set(opcode, name, M6800AddressingMode.Direct, 2, 6, () => action(FetchDirectAddress()));
        Set((byte)(opcode + 0x60), name, M6800AddressingMode.Indexed, 2, 6, () => action(FetchIndexed()));
        Set((byte)(opcode + 0x70), name, M6800AddressingMode.Extended, 3, 6, () => action(FetchExtended()));
    }

    private void SetAccumulatorOperations()
    {
        SetAluGroup(0x80, true, M6800AddressingMode.Immediate, 2);
        SetAluGroup(0x90, true, M6800AddressingMode.Direct, 3);
        SetAluGroup(0xA0, true, M6800AddressingMode.Indexed, 5);
        SetAluGroup(0xB0, true, M6800AddressingMode.Extended, 4);
        SetAluGroup(0xC0, false, M6800AddressingMode.Immediate, 2);
        SetAluGroup(0xD0, false, M6800AddressingMode.Direct, 3);
        SetAluGroup(0xE0, false, M6800AddressingMode.Indexed, 5);
        SetAluGroup(0xF0, false, M6800AddressingMode.Extended, 4);
    }

    private void SetAluGroup(byte start, bool registerA, M6800AddressingMode mode, byte cycles)
    {
        Func<int> operand = mode switch
        {
            M6800AddressingMode.Immediate => () => Fetch(),
            M6800AddressingMode.Direct => () => LdDirect(),
            M6800AddressingMode.Indexed => () => LdIndexed(),
            _ => () => LdExtended(),
        };
        byte length = mode == M6800AddressingMode.Immediate ? (byte)2 : (byte)(mode == M6800AddressingMode.Extended ? 3 : 2);
        byte[] offsets = [0x00, 0x01, 0x02, 0x04, 0x05, 0x06, 0x08, 0x09, 0x0A, 0x0B];
        string[] names = registerA
            ? ["SUBA", "CMPA", "SBCA", "ANDA", "BITA", "LDAA", "EORA", "ADCA", "ORAA", "ADDA"]
            : ["SUBB", "CMPB", "SBCB", "ANDB", "BITB", "LDAB", "EORB", "ADCB", "ORAB", "ADDB"];

        for (var i = 0; i < offsets.Length; i++)
        {
            byte opcode = (byte)(start + offsets[i]);
            var index = i;
            Func<int> action = () => ExecuteAlu(index, registerA, operand()) + cycles - 2;
            Set(opcode, names[i], mode, length, cycles, action);
        }
    }

    private int ExecuteAlu(int operation, bool registerA, int value)
    {
        byte operand = (byte)value;
        return operation switch
        {
            0 => registerA ? SubA(operand) : SubB(operand),
            1 => registerA ? CmpA(operand) : CmpB(operand),
            2 => registerA ? SbcA(operand) : SbcB(operand),
            3 => registerA ? AndA(operand) : AndB(operand),
            4 => registerA ? BitA(operand) : BitB(operand),
            5 => registerA ? LdaI(operand) : LdbI(operand),
            6 => registerA ? EorA(operand) : EorB(operand),
            7 => registerA ? AdcA(operand) : AdcB(operand),
            8 => registerA ? OraA(operand) : OraB(operand),
            _ => registerA ? AddA(operand) : AddB(operand),
        };
    }

    private void SetMemoryOperations()
    {
        SetLoadStore(0x96, true, M6800AddressingMode.Direct, 3, 3);
        SetLoadStore(0xA6, true, M6800AddressingMode.Indexed, 2, 5);
        SetLoadStore(0xB6, true, M6800AddressingMode.Extended, 3, 4);
        SetLoadStore(0xD6, false, M6800AddressingMode.Direct, 3, 3);
        SetLoadStore(0xE6, false, M6800AddressingMode.Indexed, 2, 5);
        SetLoadStore(0xF6, false, M6800AddressingMode.Extended, 3, 4);
    }

    private void SetLoadStore(byte loadOpcode, bool registerA, M6800AddressingMode mode, byte length, byte cycles)
    {
        Func<ushort> address = mode switch
        {
            M6800AddressingMode.Direct => FetchDirectAddress,
            M6800AddressingMode.Indexed => FetchIndexed,
            _ => FetchExtended,
        };
        string register = registerA ? "A" : "B";
        Set(loadOpcode, $"LD{register}", mode, length, cycles, () => LoadAOrB(registerA, address(), cycles));
        Set((byte)(loadOpcode + 1), $"ST{register}", mode, length, cycles, () => StoreAOrB(registerA, address(), cycles));
    }

    private void SetIndexOperations()
    {
        Set(0x8C, "CPX", M6800AddressingMode.Immediate, 3, 4, () => CmpX(Fetch16()));
        Set(0x9C, "CPX", M6800AddressingMode.Direct, 2, 5, () => CmpX(Ld16Direct()) + 1);
        Set(0xAC, "CPX", M6800AddressingMode.Indexed, 2, 6, () => CmpX(Ld16Indexed()) + 2);
        Set(0xBC, "CPX", M6800AddressingMode.Extended, 3, 6, () => CmpX(Ld16Extended()) + 2);
        Set(0xCE, "LDX", M6800AddressingMode.Immediate, 3, 3, () => LdxI(Fetch16()));
        Set(0xDE, "LDX", M6800AddressingMode.Direct, 2, 5, () => LoadX(FetchDirectAddress(), 5));
        Set(0xEE, "LDX", M6800AddressingMode.Indexed, 2, 6, () => LoadX(FetchIndexed(), 6));
        Set(0xFE, "LDX", M6800AddressingMode.Extended, 3, 6, () => LoadX(FetchExtended(), 6));
        Set(0xDF, "STX", M6800AddressingMode.Direct, 2, 5, () => StoreX(FetchDirectAddress(), 5));
        Set(0xEF, "STX", M6800AddressingMode.Indexed, 2, 6, () => StoreX(FetchIndexed(), 6));
        Set(0xFF, "STX", M6800AddressingMode.Extended, 3, 6, () => StoreX(FetchExtended(), 6));
    }

    private void Set(byte opcode, string mnemonic, M6800AddressingMode mode, byte length, byte cycles, Func<int> action)
    {
        Opcodes.Replace(new OpcodeDefinition<M6800State>(
            OpcodeKey.Base(opcode),
            mnemonic,
            length,
            cycles,
            mode.ToString(),
            (_, _) => CpuStepResult.Completed((ulong)action())));
    }

    private int UnsupportedOpcode() => 2;

    private ushort FetchIndexed() => (ushort)(State.X + Fetch());
    private byte LdIndexed() => Mmu.Read(FetchIndexed());
    private ushort Ld16Indexed() => Read16(FetchIndexed());

    private int LoadAOrB(bool registerA, ushort address, int cycles) => registerA ? LoadA(address, cycles) : LoadB(address, cycles);
    private int StoreAOrB(bool registerA, ushort address, int cycles) => registerA ? StoreA(address, cycles) : StoreB(address, cycles);

    private int SetFlag(Action<M6800Flags> action)
    {
        action(ConditionCodes);
        return 2;
    }

    private int Tap() { State.Flags = M6800Flags.FromByte(State.A); return 2; }
    private int Tpa() { State.A = ConditionCodes.ToByte(); return 2; }
    private int Inx() { State.X++; ConditionCodes.Z = State.X == 0; return 4; }
    private int Dex() { State.X--; ConditionCodes.Z = State.X == 0; return 4; }
    private int Tsx() { State.X = (ushort)(State.StackPointer + 1); return 4; }
    private int Txs() { State.StackPointer = (ushort)(State.X - 1); return 4; }
    private int Sba() { State.A = (byte)(State.A - State.B); ConditionCodes.Z = State.A == 0; ConditionCodes.N = (State.A & 0x80) != 0; return 2; }
    private int Cba() { return CmpA(State.B); }
    private int Tab() { State.B = State.A; SetLogical(State.B); return 2; }
    private int Tba() { State.A = State.B; SetLogical(State.A); return 2; }
    private int Aba() { return AddA(State.B); }

    private int Daa()
    {
        byte correction = (byte)(((ConditionCodes.H || (State.A & 0x0F) > 9) ? 0x06 : 0) + ((ConditionCodes.C || State.A > 0x99) ? 0x60 : 0));
        int result = State.A + correction;
        State.A = (byte)result;
        SetLogical(State.A);
        ConditionCodes.C = ConditionCodes.C || result > 0xFF;
        return 2;
    }

    private int Rti()
    {
        State.Flags = M6800Flags.FromByte(PopStack8());
        State.B = PopStack8();
        State.A = PopStack8();
        State.X = PopStack16();
        State.PC = PopStack16();
        return 10;
    }

    private int Wai()
    {
        State.Halted = true;
        return 9;
    }

    private int Swi()
    {
        PushStack16(State.PC);
        PushStack16(State.X);
        PushStack8(State.A);
        PushStack8(State.B);
        PushStack8(ConditionCodes.ToByte());
        ConditionCodes.I = true;
        State.PC = Read16(0xFFFA);
        return 12;
    }

    private void SetLogical(byte value)
    {
        ConditionCodes.N = (value & 0x80) != 0;
        ConditionCodes.Z = value == 0;
        ConditionCodes.V = false;
    }
}
