using PetEmulator.Cpu6800;
using PetEmulator.Core;

namespace PetEmulator.Cpu6809;

/// <summary>Motorola 6809 CPU emulator with dual stacks and complex addressing modes.</summary>
public class M6809Cpu : M6800Cpu, IFirqProcessor
{
    protected Func<int>[] OpcodeTable = null!;

    protected override int ShortBranchCycles => 3;
    protected override int BranchSubroutineCycles => 7;

    public M6800OpcodeTable OpcodeMetadata { get; private set; } = null!;
    public M6800OpcodeTable Page10OpcodeMetadata { get; private set; } = null!;
    public M6800OpcodeTable Page11OpcodeMetadata { get; private set; } = null!;

    public long Cycles => State.Cycles;
    public new M6809State State => (M6809State)base.State;

    /// <summary>Registers by name for the shared debugger and Desktop status bar.</summary>
    public override IReadOnlyDictionary<string, ulong> GetRegisters() => new Dictionary<string, ulong>
    {
        ["PC"] = State.PC,
        ["A"] = State.A,
        ["B"] = State.B,
        ["X"] = State.X,
        ["Y"] = State.Y,
        ["U"] = State.U,
        ["S"] = State.S,
        // The shared Desktop status view names the active stack pointer SP.
        ["SP"] = State.S,
        ["DP"] = State.DP,
        ["P"] = State.Flags.ToByte(),
    };



    protected bool _irqPending;
    protected bool _firqPending;
    protected bool _nmiPending;
    protected bool _nmiArmed;  // set true when S is written, cleared on Reset

    protected Func<int>[] Page10OpcodeTable = null!;
    protected Func<int>[] Page11OpcodeTable = null!;

    public M6809Cpu(IMemoryBus memory)
        : this(memory, new M6809State())
    {
    }

    /// <summary>Constructor for derived classes that use a custom state type.</summary>
    protected M6809Cpu(IMemoryBus memory, M6809State state)
        : base(memory, state, new M6800OpcodeTable())
    {
        FillOpcodeTable();
        OpcodeMetadata = BuildOpcodeMetadata(OpcodeTable);
        Page10OpcodeMetadata = BuildOpcodeMetadata(Page10OpcodeTable);
        Page11OpcodeMetadata = BuildOpcodeMetadata(Page11OpcodeTable);
    }

    /// <summary>Request an IRQ interrupt (level-triggered, blocked by I flag).</summary>
    public void RequestIrq()
    {
        _irqPending = true;
    }

    /// <summary>Request a FIRQ interrupt (level-triggered, blocked by F flag).</summary>
    public void RequestFirq()
    {
        _firqPending = true;
    }

    public void SetFIRQ(bool active) => _firqPending = active;

    /// <summary>Request an NMI interrupt (edge-triggered, armed when S is written).</summary>
    public void RequestNmi()
    {
        _nmiPending = true;
    }

    /// <summary>Reset CPU: state reset, all latches cleared, PC loaded from 0xFFFE, NMI disarmed.</summary>
    public override void Reset()
    {
        State.Reset();
        _irqPending = false;
        _firqPending = false;
        _nmiPending = false;
        _nmiArmed = false;
        InstructionCount = 0;
        ResetCycleClock();
        State.PC = Read16(0xFFFE);
    }

    /// <summary>Execute one instruction or interrupt dispatch; returns cycles elapsed.</summary>
    public override int Step()
    {
        // Step 1: Check NMI (edge-triggered, highest priority, armed only)
        if (_nmiPending && _nmiArmed)
        {
            _nmiPending = false;
            State.Halted = false;
            PushFullFrame();
            State.Flags.I = true;
            State.Flags.F = true;
            State.PC = Read16(0xFFFC);
            AdvanceCycles(19);
            return 19;
        }

        // Step 2: Check FIRQ (level-triggered, blocked by F flag)
        if (_firqPending && !State.Flags.F)
        {
            _firqPending = false;
            State.Halted = false;
            PushFastFrame();
            State.Flags.I = true;
            State.Flags.F = true;
            State.PC = Read16(0xFFF6);
            AdvanceCycles(10);
            return 10;
        }

        // Step 3: Check IRQ (level-triggered, blocked by I flag)
        if (_irqPending && !State.Flags.I)
        {
            _irqPending = false;
            State.Halted = false;
            PushFullFrame();
            State.Flags.I = true;
            State.PC = Read16(0xFFF8);
            AdvanceCycles(19);
            return 19;
        }

        // Step 4: Check HALTed state (SYNC/CWAI waiting)
        if (State.Halted)
        {
            AdvanceCycles(2);
            return 2;
        }

        // Step 5: Normal instruction execution
        byte op = Fetch();
        int cycles = ExecuteOpcode(op);
        InstructionCount++;
        AdvanceCycles(cycles);
        return cycles;
    }

    private void FillOpcodeTable()
    {
        OpcodeTable = new Func<int>[256];
        Page10OpcodeTable = new Func<int>[256];
        Page11OpcodeTable = new Func<int>[256];

        // Keep the historical 2-cycle compatibility behavior, but expose it as unsupported metadata.
        for (int i = 0; i < 256; i++)
        {
            Page10OpcodeTable[i] = UnsupportedOpcode;
            Page11OpcodeTable[i] = UnsupportedOpcode;
        }

        // Fill Page10 prefix table (0x10 0xNN)
        Page10OpcodeTable[0x20] = () => Lbra(Fetch16());
        Page10OpcodeTable[0x21] = () => Lbrn(Fetch16());
        Page10OpcodeTable[0x22] = () => Lbhi(Fetch16());
        Page10OpcodeTable[0x23] = () => Lbls(Fetch16());
        Page10OpcodeTable[0x24] = () => Lbcc(Fetch16());
        Page10OpcodeTable[0x25] = () => Lbcs(Fetch16());
        Page10OpcodeTable[0x26] = () => Lbne(Fetch16());
        Page10OpcodeTable[0x27] = () => Lbeq(Fetch16());
        Page10OpcodeTable[0x28] = () => Lbvc(Fetch16());
        Page10OpcodeTable[0x29] = () => Lbvs(Fetch16());
        Page10OpcodeTable[0x2A] = () => Lbpl(Fetch16());
        Page10OpcodeTable[0x2B] = () => Lbmi(Fetch16());
        Page10OpcodeTable[0x2C] = () => Lbge(Fetch16());
        Page10OpcodeTable[0x2D] = () => Lblt(Fetch16());
        Page10OpcodeTable[0x2E] = () => Lbgt(Fetch16());
        Page10OpcodeTable[0x2F] = () => Lble(Fetch16());
        Page10OpcodeTable[0x83] = () => CmpD(Fetch16());
        Page10OpcodeTable[0x93] = () => CmpD(Ld16Direct());
        Page10OpcodeTable[0xA3] = () => CmpD(Ld16Indexed(out int exA3)) + exA3;
        Page10OpcodeTable[0xB3] = () => CmpD(Ld16Extended());
        Page10OpcodeTable[0x8C] = () => CmpY(Fetch16());
        Page10OpcodeTable[0x9C] = () => CmpY(Ld16Direct());
        Page10OpcodeTable[0xAC] = () => CmpY(Ld16Indexed(out int exAC)) + exAC;
        Page10OpcodeTable[0xBC] = () => CmpY(Ld16Extended());
        Page10OpcodeTable[0x8E] = () => LdyI(Fetch16());
        Page10OpcodeTable[0x9E] = () => LdyD();
        Page10OpcodeTable[0xAE] = () => LdyIdx();
        Page10OpcodeTable[0xBE] = () => LdyExt();
        Page10OpcodeTable[0x9F] = () => StyD();
        Page10OpcodeTable[0xAF] = () => StyIdx();
        Page10OpcodeTable[0xBF] = () => StyExt();
        Page10OpcodeTable[0xCE] = () => LdsI(Fetch16());
        Page10OpcodeTable[0xDE] = () => LdsD();
        Page10OpcodeTable[0xEE] = () => LdsIdx();
        Page10OpcodeTable[0xFE] = () => LdsExt();
        Page10OpcodeTable[0xDF] = () => StsD();
        Page10OpcodeTable[0xEF] = () => StsIdx();
        Page10OpcodeTable[0xFF] = () => StsExt();
        Page10OpcodeTable[0x3F] = () => Swi2();

        // Fill Page11 prefix table (0x11 0xNN)
        Page11OpcodeTable[0x83] = () => CmpU(Fetch16());
        Page11OpcodeTable[0x93] = () => CmpU(Ld16Direct());
        Page11OpcodeTable[0xA3] = () => CmpU(Ld16Indexed(out int exA3P11)) + exA3P11;
        Page11OpcodeTable[0xB3] = () => CmpU(Ld16Extended());
        Page11OpcodeTable[0x8C] = () => CmpS(Fetch16());
        Page11OpcodeTable[0x9C] = () => CmpS(Ld16Direct());
        Page11OpcodeTable[0xAC] = () => CmpS(Ld16Indexed(out int exACP11)) + exACP11;
        Page11OpcodeTable[0xBC] = () => CmpS(Ld16Extended());
        Page11OpcodeTable[0x3F] = () => Swi3();

        OpcodeTable[0x00] = () => base.Neg(FetchDirect());
        OpcodeTable[0x01] = () => 2;
        OpcodeTable[0x02] = () => 2;
        OpcodeTable[0x03] = () => base.Com(FetchDirect());
        OpcodeTable[0x04] = () => base.Lsr(FetchDirect());
        OpcodeTable[0x05] = () => 2;
        OpcodeTable[0x06] = () => base.Ror(FetchDirect());
        OpcodeTable[0x07] = () => base.Asr(FetchDirect());
        OpcodeTable[0x08] = () => base.Asl(FetchDirect());
        OpcodeTable[0x09] = () => base.Rol(FetchDirect());
        OpcodeTable[0x0A] = () => base.Dec(FetchDirect());
        OpcodeTable[0x0B] = () => 2;
        OpcodeTable[0x0C] = () => base.Inc(FetchDirect());
        OpcodeTable[0x0D] = () => base.Tst(FetchDirect());
        OpcodeTable[0x0E] = () => Jmp(FetchExtended());
        OpcodeTable[0x0F] = () => base.Clr(FetchDirect());
        OpcodeTable[0x10] = () => Page10OpcodeTable[Fetch()]();
        OpcodeTable[0x11] = () => Page11OpcodeTable[Fetch()]();
        OpcodeTable[0x12] = () => 2;
        OpcodeTable[0x13] = Sync;
        OpcodeTable[0x14] = () => 2;
        OpcodeTable[0x15] = () => 2;
        OpcodeTable[0x16] = () => Lbra(Fetch16());
        OpcodeTable[0x17] = () => Lbsr(Fetch16());
        OpcodeTable[0x18] = () => 2;
        OpcodeTable[0x19] = Daa;
        OpcodeTable[0x1A] = () => Orcc(Fetch());
        OpcodeTable[0x1B] = () => 2;
        OpcodeTable[0x1C] = () => Andcc(Fetch());
        OpcodeTable[0x1D] = Sex;
        OpcodeTable[0x1E] = () => Exg(Fetch());
        OpcodeTable[0x1F] = () => Tfr(Fetch());
        OpcodeTable[0x20] = () => base.Bra(FetchSigned());
        OpcodeTable[0x21] = () => base.Brn(FetchSigned());
        OpcodeTable[0x22] = () => base.Bhi(FetchSigned());
        OpcodeTable[0x23] = () => base.Bls(FetchSigned());
        OpcodeTable[0x24] = () => base.Bcc(FetchSigned());
        OpcodeTable[0x25] = () => base.Bcs(FetchSigned());
        OpcodeTable[0x26] = () => base.Bne(FetchSigned());
        OpcodeTable[0x27] = () => base.Beq(FetchSigned());
        OpcodeTable[0x28] = () => base.Bvc(FetchSigned());
        OpcodeTable[0x29] = () => base.Bvs(FetchSigned());
        OpcodeTable[0x2A] = () => base.Bpl(FetchSigned());
        OpcodeTable[0x2B] = () => base.Bmi(FetchSigned());
        OpcodeTable[0x2C] = () => base.Bge(FetchSigned());
        OpcodeTable[0x2D] = () => base.Blt(FetchSigned());
        OpcodeTable[0x2E] = () => base.Bgt(FetchSigned());
        OpcodeTable[0x2F] = () => base.Ble(FetchSigned());
        OpcodeTable[0x30] = () => Leax(FetchIndexed(out int ex30)) + ex30;
        OpcodeTable[0x31] = () => Leay(FetchIndexed(out int ex31)) + ex31;
        OpcodeTable[0x32] = () => Leas(FetchIndexed(out int ex32)) + ex32;
        OpcodeTable[0x33] = () => Leau(FetchIndexed(out int ex33)) + ex33;
        OpcodeTable[0x34] = () => Pshs(Fetch());
        OpcodeTable[0x35] = () => Puls(Fetch());
        OpcodeTable[0x36] = () => Pshu(Fetch());
        OpcodeTable[0x37] = () => Pulu(Fetch());
        OpcodeTable[0x38] = () => 2;
        OpcodeTable[0x39] = base.Rts;
        OpcodeTable[0x3A] = Abx;
        OpcodeTable[0x3B] = Rti;
        OpcodeTable[0x3C] = () => Cwai(Fetch());
        OpcodeTable[0x3D] = Mul;
        OpcodeTable[0x3E] = () => 2;
        OpcodeTable[0x3F] = Swi;

        // RMW A (0x40-0x4F)
        OpcodeTable[0x40] = base.NegA; OpcodeTable[0x41] = () => 2; OpcodeTable[0x42] = () => 2; OpcodeTable[0x43] = base.ComA;
        OpcodeTable[0x44] = base.LsrA; OpcodeTable[0x45] = () => 2; OpcodeTable[0x46] = base.RorA; OpcodeTable[0x47] = base.AsrA;
        OpcodeTable[0x48] = base.AslA; OpcodeTable[0x49] = base.RolA; OpcodeTable[0x4A] = base.DecA; OpcodeTable[0x4B] = () => 2;
        OpcodeTable[0x4C] = base.IncA; OpcodeTable[0x4D] = base.TstA; OpcodeTable[0x4E] = () => 2; OpcodeTable[0x4F] = base.ClrA;

        // RMW B (0x50-0x5F)
        OpcodeTable[0x50] = base.NegB; OpcodeTable[0x51] = () => 2; OpcodeTable[0x52] = () => 2; OpcodeTable[0x53] = base.ComB;
        OpcodeTable[0x54] = base.LsrB; OpcodeTable[0x55] = () => 2; OpcodeTable[0x56] = base.RorB; OpcodeTable[0x57] = base.AsrB;
        OpcodeTable[0x58] = base.AslB; OpcodeTable[0x59] = base.RolB; OpcodeTable[0x5A] = base.DecB; OpcodeTable[0x5B] = () => 2;
        OpcodeTable[0x5C] = base.IncB; OpcodeTable[0x5D] = base.TstB; OpcodeTable[0x5E] = () => 2; OpcodeTable[0x5F] = base.ClrB;

        // Indexed RMW (0x60-0x6F)
        OpcodeTable[0x60] = () => base.Neg(FetchIndexed(out int ex60)) + ex60;
        OpcodeTable[0x61] = () => 2; OpcodeTable[0x62] = () => 2;
        OpcodeTable[0x63] = () => base.Com(FetchIndexed(out int ex63)) + ex63;
        OpcodeTable[0x64] = () => base.Lsr(FetchIndexed(out int ex64)) + ex64;
        OpcodeTable[0x65] = () => 2;
        OpcodeTable[0x66] = () => base.Ror(FetchIndexed(out int ex66)) + ex66;
        OpcodeTable[0x67] = () => base.Asr(FetchIndexed(out int ex67)) + ex67;
        OpcodeTable[0x68] = () => base.Asl(FetchIndexed(out int ex68)) + ex68;
        OpcodeTable[0x69] = () => base.Rol(FetchIndexed(out int ex69)) + ex69;
        OpcodeTable[0x6A] = () => base.Dec(FetchIndexed(out int ex6A)) + ex6A;
        OpcodeTable[0x6B] = () => 2;
        OpcodeTable[0x6C] = () => base.Inc(FetchIndexed(out int ex6C)) + ex6C;
        OpcodeTable[0x6D] = () => base.Tst(FetchIndexed(out int ex6D)) + ex6D;
        OpcodeTable[0x6E] = () => Jmp(FetchIndexed(out int ex6E)) + ex6E;
        OpcodeTable[0x6F] = () => base.Clr(FetchIndexed(out int ex6F)) + ex6F;

        // Extended RMW (0x70-0x7F)
        OpcodeTable[0x70] = () => base.Neg(FetchExtended()); OpcodeTable[0x71] = () => 2; OpcodeTable[0x72] = () => 2;
        OpcodeTable[0x73] = () => base.Com(FetchExtended()); OpcodeTable[0x74] = () => base.Lsr(FetchExtended());
        OpcodeTable[0x75] = () => 2; OpcodeTable[0x76] = () => base.Ror(FetchExtended());
        OpcodeTable[0x77] = () => base.Asr(FetchExtended()); OpcodeTable[0x78] = () => base.Asl(FetchExtended());
        OpcodeTable[0x79] = () => base.Rol(FetchExtended()); OpcodeTable[0x7A] = () => base.Dec(FetchExtended());
        OpcodeTable[0x7B] = () => 2; OpcodeTable[0x7C] = () => base.Inc(FetchExtended());
        OpcodeTable[0x7D] = () => base.Tst(FetchExtended()); OpcodeTable[0x7E] = () => Jmp(FetchExtended());
        OpcodeTable[0x7F] = () => base.Clr(FetchExtended());

        // A-column ALU (0x80-0x8F)
        OpcodeTable[0x80] = () => base.SubA(Fetch()); OpcodeTable[0x81] = () => base.CmpA(Fetch());
        OpcodeTable[0x82] = () => base.SbcA(Fetch()); OpcodeTable[0x83] = () => SubD(Fetch16());
        OpcodeTable[0x84] = () => base.AndA(Fetch()); OpcodeTable[0x85] = () => base.BitA(Fetch());
        OpcodeTable[0x86] = () => base.LdaI(Fetch()); OpcodeTable[0x87] = () => 2;
        OpcodeTable[0x88] = () => base.EorA(Fetch()); OpcodeTable[0x89] = () => base.AdcA(Fetch());
        OpcodeTable[0x8A] = () => base.OraA(Fetch()); OpcodeTable[0x8B] = () => base.AddA(Fetch());
        OpcodeTable[0x8C] = () => base.CmpX(Fetch16()); OpcodeTable[0x8D] = () => base.Bsr(FetchSigned());
        OpcodeTable[0x8E] = () => base.LdxI(Fetch16()); OpcodeTable[0x8F] = () => 2;

        // A-column dir (0x90-0x9F)
        OpcodeTable[0x90] = () => base.SubA(LdDirect()); OpcodeTable[0x91] = () => base.CmpA(LdDirect());
        OpcodeTable[0x92] = () => base.SbcA(LdDirect()); OpcodeTable[0x93] = () => SubD(Ld16Direct());
        OpcodeTable[0x94] = () => base.AndA(LdDirect()); OpcodeTable[0x95] = () => base.BitA(LdDirect());
        OpcodeTable[0x96] = () => base.LoadA(FetchDirectAddress(), 4); OpcodeTable[0x97] = () => base.StoreA(FetchDirectAddress(), 4);
        OpcodeTable[0x98] = () => base.EorA(LdDirect()); OpcodeTable[0x99] = () => base.AdcA(LdDirect());
        OpcodeTable[0x9A] = () => base.OraA(LdDirect()); OpcodeTable[0x9B] = () => base.AddA(LdDirect());
        OpcodeTable[0x9C] = () => base.CmpX(Ld16Direct()); OpcodeTable[0x9D] = JsrD;
        OpcodeTable[0x9E] = () => base.LoadX(FetchDirectAddress(), 5); OpcodeTable[0x9F] = () => base.StoreX(FetchDirectAddress(), 5);

        // A-column indexed (0xA0-0xAF)
        OpcodeTable[0xA0] = () => base.SubA(LdIndexed(out int exA0)) + exA0;
        OpcodeTable[0xA1] = () => base.CmpA(LdIndexed(out int exA1)) + exA1;
        OpcodeTable[0xA2] = () => base.SbcA(LdIndexed(out int exA2)) + exA2;
        OpcodeTable[0xA3] = () => SubD(Ld16Indexed(out int exA3)) + exA3;
        OpcodeTable[0xA4] = () => base.AndA(LdIndexed(out int exA4)) + exA4;
        OpcodeTable[0xA5] = () => base.BitA(LdIndexed(out int exA5)) + exA5;
        OpcodeTable[0xA6] = () => base.LoadA(FetchIndexed(out int exA6), 4 + exA6);
        OpcodeTable[0xA7] = () => base.StoreA(FetchIndexed(out int exA7), 4 + exA7);
        OpcodeTable[0xA8] = () => base.EorA(LdIndexed(out int exA8)) + exA8;
        OpcodeTable[0xA9] = () => base.AdcA(LdIndexed(out int exA9)) + exA9;
        OpcodeTable[0xAA] = () => base.OraA(LdIndexed(out int exAA)) + exAA;
        OpcodeTable[0xAB] = () => base.AddA(LdIndexed(out int exAB)) + exAB;
        OpcodeTable[0xAC] = () => base.CmpX(Ld16Indexed(out int exAC)) + exAC;
        OpcodeTable[0xAD] = JsrIdx;
        OpcodeTable[0xAE] = () => base.LoadX(FetchIndexed(out int exAE), 5 + exAE);
        OpcodeTable[0xAF] = () => base.StoreX(FetchIndexed(out int exAF), 5 + exAF);

        // A-column extended (0xB0-0xBF)
        OpcodeTable[0xB0] = () => base.SubA(LdExtended()); OpcodeTable[0xB1] = () => base.CmpA(LdExtended());
        OpcodeTable[0xB2] = () => base.SbcA(LdExtended()); OpcodeTable[0xB3] = () => SubD(Ld16Extended());
        OpcodeTable[0xB4] = () => base.AndA(LdExtended()); OpcodeTable[0xB5] = () => base.BitA(LdExtended());
        OpcodeTable[0xB6] = () => base.LoadA(FetchExtended(), 5); OpcodeTable[0xB7] = () => base.StoreA(FetchExtended(), 5);
        OpcodeTable[0xB8] = () => base.EorA(LdExtended()); OpcodeTable[0xB9] = () => base.AdcA(LdExtended());
        OpcodeTable[0xBA] = () => base.OraA(LdExtended()); OpcodeTable[0xBB] = () => base.AddA(LdExtended());
        OpcodeTable[0xBC] = () => base.CmpX(Ld16Extended()); OpcodeTable[0xBD] = JsrExt;
        OpcodeTable[0xBE] = () => base.LoadX(FetchExtended(), 6); OpcodeTable[0xBF] = () => base.StoreX(FetchExtended(), 6);

        // B-column ALU (0xC0-0xCF)
        OpcodeTable[0xC0] = () => base.SubB(Fetch()); OpcodeTable[0xC1] = () => base.CmpB(Fetch());
        OpcodeTable[0xC2] = () => base.SbcB(Fetch()); OpcodeTable[0xC3] = () => AddD(Fetch16());
        OpcodeTable[0xC4] = () => base.AndB(Fetch()); OpcodeTable[0xC5] = () => base.BitB(Fetch());
        OpcodeTable[0xC6] = () => base.LdbI(Fetch()); OpcodeTable[0xC7] = () => 2;
        OpcodeTable[0xC8] = () => base.EorB(Fetch()); OpcodeTable[0xC9] = () => base.AdcB(Fetch());
        OpcodeTable[0xCA] = () => base.OraB(Fetch()); OpcodeTable[0xCB] = () => base.AddB(Fetch());
        OpcodeTable[0xCC] = () => LddI(Fetch16()); OpcodeTable[0xCD] = () => 2;
        OpcodeTable[0xCE] = () => LduI(Fetch16()); OpcodeTable[0xCF] = () => 2;

        // B-column dir (0xD0-0xDF)
        OpcodeTable[0xD0] = () => base.SubB(LdDirect()); OpcodeTable[0xD1] = () => base.CmpB(LdDirect());
        OpcodeTable[0xD2] = () => base.SbcB(LdDirect()); OpcodeTable[0xD3] = () => AddD(Ld16Direct());
        OpcodeTable[0xD4] = () => base.AndB(LdDirect()); OpcodeTable[0xD5] = () => base.BitB(LdDirect());
        OpcodeTable[0xD6] = () => base.LoadB(FetchDirectAddress(), 4); OpcodeTable[0xD7] = () => base.StoreB(FetchDirectAddress(), 4);
        OpcodeTable[0xD8] = () => base.EorB(LdDirect()); OpcodeTable[0xD9] = () => base.AdcB(LdDirect());
        OpcodeTable[0xDA] = () => base.OraB(LdDirect()); OpcodeTable[0xDB] = () => base.AddB(LdDirect());
        OpcodeTable[0xDC] = LddD; OpcodeTable[0xDD] = StdD;
        OpcodeTable[0xDE] = LduD; OpcodeTable[0xDF] = StuD;

        // B-column indexed (0xE0-0xEF)
        OpcodeTable[0xE0] = () => base.SubB(LdIndexed(out int exE0)) + exE0;
        OpcodeTable[0xE1] = () => base.CmpB(LdIndexed(out int exE1)) + exE1;
        OpcodeTable[0xE2] = () => base.SbcB(LdIndexed(out int exE2)) + exE2;
        OpcodeTable[0xE3] = () => AddD(Ld16Indexed(out int exE3)) + exE3;
        OpcodeTable[0xE4] = () => base.AndB(LdIndexed(out int exE4)) + exE4;
        OpcodeTable[0xE5] = () => base.BitB(LdIndexed(out int exE5)) + exE5;
        OpcodeTable[0xE6] = () => base.LoadB(FetchIndexed(out int exE6), 4 + exE6);
        OpcodeTable[0xE7] = () => base.StoreB(FetchIndexed(out int exE7), 4 + exE7);
        OpcodeTable[0xE8] = () => base.EorB(LdIndexed(out int exE8)) + exE8;
        OpcodeTable[0xE9] = () => base.AdcB(LdIndexed(out int exE9)) + exE9;
        OpcodeTable[0xEA] = () => base.OraB(LdIndexed(out int exEA)) + exEA;
        OpcodeTable[0xEB] = () => base.AddB(LdIndexed(out int exEB)) + exEB;
        OpcodeTable[0xEC] = LddIdx;
        OpcodeTable[0xED] = StdIdx;
        OpcodeTable[0xEE] = LduIdx;
        OpcodeTable[0xEF] = StuIdx;

        // B-column extended (0xF0-0xFF)
        OpcodeTable[0xF0] = () => base.SubB(LdExtended()); OpcodeTable[0xF1] = () => base.CmpB(LdExtended());
        OpcodeTable[0xF2] = () => base.SbcB(LdExtended()); OpcodeTable[0xF3] = () => AddD(Ld16Extended());
        OpcodeTable[0xF4] = () => base.AndB(LdExtended()); OpcodeTable[0xF5] = () => base.BitB(LdExtended());
        OpcodeTable[0xF6] = () => base.LoadB(FetchExtended(), 5); OpcodeTable[0xF7] = () => base.StoreB(FetchExtended(), 5);
        OpcodeTable[0xF8] = () => base.EorB(LdExtended()); OpcodeTable[0xF9] = () => base.AdcB(LdExtended());
        OpcodeTable[0xFA] = () => base.OraB(LdExtended()); OpcodeTable[0xFB] = () => base.AddB(LdExtended());
        OpcodeTable[0xFC] = LddExt; OpcodeTable[0xFD] = StdExt;
        OpcodeTable[0xFE] = LduExt; OpcodeTable[0xFF] = StuExt;
    }

    protected override int ExecuteOpcode(byte op) => OpcodeTable[op]();

    private static M6800OpcodeTable BuildOpcodeMetadata(Func<int>[] handlers)
    {
        var metadata = new M6800OpcodeTable();
        for (byte opcode = 0; ; opcode++)
        {
            var handler = handlers[opcode];
            if (handler is not null)
            {
                var implemented = handler.Method.Name != nameof(UnsupportedOpcode);
                metadata.Set(new M6800OpcodeDefinition(
                    opcode,
                    $"OP ${opcode:X2}",
                    M6800AddressingMode.Unknown,
                    1,
                    0,
                    (_, _) => handler(),
                    implemented));
            }

            if (opcode == byte.MaxValue)
                break;
        }

        return metadata;
    }

    private int UnsupportedOpcode() => 2;

    protected override ushort ResolveDirectAddress(byte offset) => (ushort)((State.DP << 8) | offset);
    protected override M6800Flags ConditionCodes => State.Flags;

    #region Addressing Modes

    public ushort FetchIndexed(out int extraCycles)
    {
        return ResolveIndexed(out extraCycles);
    }

    public byte LdIndexed(out int extraCycles)
    {
        ushort addr = FetchIndexed(out extraCycles);
        return Mmu.Read(addr);
    }

    public ushort Ld16Indexed(out int extraCycles)
    {
        ushort addr = FetchIndexed(out extraCycles);
        return Read16(addr);
    }

    /// <summary>
    /// Phase D1: Resolve indexed addressing modes per the postbyte table.
    /// Returns the effective address and sets extraCycles (0-5 beyond the base).
    /// </summary>
    private ushort ResolveIndexed(out int extraCycles)
    {
        byte postbyte = Fetch();
        extraCycles = 0;
        // Bit 4 is the sign bit for the compact 5-bit form (0RRnnnnn).
        // It becomes the indirect selector only in the extended 1RR mode.
        bool indirect = (postbyte & 0x80) != 0 && (postbyte & 0x10) != 0;
        int rr = (postbyte & 0x60) >> 5;
        ushort baseReg = GetIndexReg(rr);

        if ((postbyte & 0x80) == 0)
        {
            // 0RRnnnnn: 5-bit signed offset
            int offset5 = postbyte & 0x1F;
            if ((offset5 & 0x10) != 0) offset5 = offset5 - 32;  // sign extend to -16..-1
            ushort addr = (ushort)(baseReg + offset5);
            extraCycles = 1;
            return indirect ? Read16(addr) : addr;
        }

        // 1RR mode
        int mode = postbyte & 0x0F;
        ushort ea;

        switch (mode)
        {
            case 0x00: // ,R+
                ea = baseReg;
                SetIndexReg(rr, (ushort)(baseReg + 1));
                extraCycles = 2;
                break;

            case 0x01: // ,R++
                ea = baseReg;
                SetIndexReg(rr, (ushort)(baseReg + 2));
                extraCycles = 3;
                break;

            case 0x02: // ,−R
                SetIndexReg(rr, (ushort)(baseReg - 1));
                ea = (ushort)(baseReg - 1);
                extraCycles = 2;
                break;

            case 0x03: // ,−−R
                SetIndexReg(rr, (ushort)(baseReg - 2));
                ea = (ushort)(baseReg - 2);
                extraCycles = 3;
                break;

            case 0x04: // ,R
                ea = baseReg;
                extraCycles = 0;
                break;

            case 0x05: // B,R
                ea = (ushort)(baseReg + (sbyte)State.B);
                extraCycles = 1;
                break;

            case 0x06: // A,R
                ea = (ushort)(baseReg + (sbyte)State.A);
                extraCycles = 1;
                break;

            case 0x08: // n8,R
                {
                    sbyte n8 = (sbyte)Fetch();
                    ea = (ushort)(baseReg + n8);
                    extraCycles = 1;
                }
                break;

            case 0x09: // n16,R
                {
                    ushort n16 = Fetch16();
                    ea = (ushort)(baseReg + (short)n16);
                    extraCycles = 4;
                }
                break;

            case 0x0B: // D,R
                ea = (ushort)(baseReg + (short)State.D);
                extraCycles = 4;
                break;

            case 0x0C: // n8,PCR
                {
                    sbyte n8 = (sbyte)Fetch();
                    ea = (ushort)(State.PC + n8);
                    extraCycles = 1;
                }
                break;

            case 0x0D: // n16,PCR
                {
                    ushort n16 = Fetch16();
                    ea = (ushort)(State.PC + (short)n16);
                    extraCycles = 5;
                }
                break;

            case 0x0F: // [n16]
                ea = Fetch16();
                extraCycles = 4;
                break;

            default:
                // ponytail: invalid postbyte mode = no-op addressing
                ea = 0;
                extraCycles = 1;
                break;
        }

        // Add indirect cost if indirect bit is set (except for ,R+ and ,−R forms)
        if (indirect && mode != 0x00 && mode != 0x02)
        {
            ushort addrRef = ea;
            ea = Read16(addrRef);
            extraCycles += 3;
        }

        return ea;
    }

    private ushort GetIndexReg(int rr)
    {
        return rr switch
        {
            0 => State.X,
            1 => State.Y,
            2 => State.U,
            3 => State.S,
            _ => 0,
        };
    }

    private void SetIndexReg(int rr, ushort value)
    {
        switch (rr)
        {
            case 0:
                State.X = value;
                break;
            case 1:
                State.Y = value;
                break;
            case 2:
                State.U = value;
                break;
            case 3:
                State.S = value;
                _nmiArmed = true;  // NMI armed on S write
                break;
        }
    }

    // Special indexed addressing for extended indirect [n16]
    private ushort FetchExtendedIndirect()
    {
        byte postbyte = Fetch();
        if (postbyte == 0x9F)
        {
            // [n16] extended indirect
            ushort addrRef = Fetch16();
            return Read16(addrRef);
        }
        return 0;  // ponytail: error case
    }

    public byte FetchSigned()
    {
        return Fetch();
    }

    #endregion

    #region Instruction Implementations

    private int Jmp(ushort addr)
    {
        State.PC = addr;
        return 3;
    }

    // Single-byte inherent
    private int Sync()
    {
        State.Halted = true;
        return 4;
    }

    private int Daa()
    {
        byte a = State.A;
        byte result = a;
        var carry = State.Flags.C || a > 0x99;
        if (State.Flags.H || (a & 0x0F) > 0x09)
            result = (byte)(a + 0x06);
        if (carry)
            result = (byte)(result + 0x60);
        State.Flags.N = (result & 0x80) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = false;
        State.Flags.C = carry;
        State.A = result;
        return 2;
    }

    private int Orcc(byte imm)
    {
        var flags = M6809Flags.FromByte(State.Flags.ToByte());
        flags = M6809Flags.FromByte((byte)(flags.ToByte() | imm));
        State.Flags = flags;
        return 3;
    }

    private int Andcc(byte imm)
    {
        var flags = M6809Flags.FromByte(State.Flags.ToByte());
        flags = M6809Flags.FromByte((byte)(flags.ToByte() & imm));
        State.Flags = flags;
        return 3;
    }

    private int Sex()
    {
        if ((State.B & 0x80) != 0)
            State.A = 0xFF;
        else
            State.A = 0x00;
        State.Flags.N = (State.A & 0x80) != 0;
        // SEX affects N/Z from the resulting 16-bit D register, not A alone.
        State.Flags.Z = State.D == 0;
        return 2;
    }

    private int Exg(byte postbyte)
    {
        byte src = (byte)(postbyte >> 4);
        byte dst = (byte)(postbyte & 0x0F);
        GetReg(src, out ushort srcVal, out bool src16);
        GetReg(dst, out ushort dstVal, out bool dst16);
        SetReg(src, dstVal, src16);
        SetReg(dst, srcVal, dst16);
        return 8;
    }

    private int Tfr(byte postbyte)
    {
        byte src = (byte)(postbyte >> 4);
        byte dst = (byte)(postbyte & 0x0F);
        GetReg(src, out ushort srcVal, out bool src16);
        SetReg(dst, srcVal, src16);
        return 6;
    }

    protected virtual void GetReg(byte code, out ushort value, out bool is16)
    {
        is16 = true;
        value = code switch
        {
            0x0 => State.D,
            0x1 => State.X,
            0x2 => State.Y,
            0x3 => State.U,
            0x4 => State.S,
            0x5 => State.PC,
            0x8 => (ushort)State.A,
            0x9 => (ushort)State.B,
            0xA => (ushort)State.Flags.ToByte(),
            0xB => (ushort)State.DP,
            _ => 0,
        };
        is16 = code is not (0x8 or 0x9 or 0xA or 0xB);
    }

    protected virtual void SetReg(byte code, ushort value, bool is16)
    {
        switch (code)
        {
            case 0x0:
                State.D = value;
                break;
            case 0x1:
                State.X = value;
                break;
            case 0x2:
                State.Y = value;
                break;
            case 0x3:
                State.U = value;
                break;
            case 0x4:
                State.S = value;
                _nmiArmed = true;
                break;
            case 0x5:
                State.PC = value;
                break;
            case 0x8:
                State.A = (byte)value;
                break;
            case 0x9:
                State.B = (byte)value;
                break;
            case 0xA:
                State.Flags = M6809Flags.FromByte((byte)value);
                break;
            case 0xB:
                State.DP = (byte)value;
                break;
        }
    }

    // Long branches (16-bit offset)
    private int Lbra(ushort offset)
    {
        State.PC = (ushort)(State.PC + (short)offset);
        return 5;
    }

    private int Lbsr(ushort offset)
    {
        // BSR/LBSR use the hardware stack; the U stack is reserved for PSHU/PULU.
        PushS16(State.PC);
        State.PC = (ushort)(State.PC + (short)offset);
        return 9;
    }

    private int Lbrn(ushort offset)
    {
        return 5;
    }

    private int Lbhi(ushort offset)
    {
        if (!State.Flags.C && !State.Flags.Z)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbls(ushort offset)
    {
        if (State.Flags.C || State.Flags.Z)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbcc(ushort offset)
    {
        if (!State.Flags.C)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbcs(ushort offset)
    {
        if (State.Flags.C)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbne(ushort offset)
    {
        if (!State.Flags.Z)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbeq(ushort offset)
    {
        if (State.Flags.Z)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbvc(ushort offset)
    {
        if (!State.Flags.V)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbvs(ushort offset)
    {
        if (State.Flags.V)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbpl(ushort offset)
    {
        if (!State.Flags.N)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbmi(ushort offset)
    {
        if (State.Flags.N)
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbge(ushort offset)
    {
        if ((State.Flags.N && State.Flags.V) || (!State.Flags.N && !State.Flags.V))
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lblt(ushort offset)
    {
        if ((State.Flags.N && !State.Flags.V) || (!State.Flags.N && State.Flags.V))
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lbgt(ushort offset)
    {
        if (!State.Flags.Z && ((State.Flags.N && State.Flags.V) || (!State.Flags.N && !State.Flags.V)))
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    private int Lble(ushort offset)
    {
        if (State.Flags.Z || ((State.Flags.N && !State.Flags.V) || (!State.Flags.N && State.Flags.V)))
        {
            State.PC = (ushort)(State.PC + (short)offset);
            return 6;
        }
        return 5;
    }

    // LEA instructions
    private int Leax(ushort addr)
    {
        State.X = addr;
        State.Flags.Z = addr == 0;
        return 4;
    }

    private int Leay(ushort addr)
    {
        State.Y = addr;
        State.Flags.Z = addr == 0;
        return 4;
    }

    private int Leas(ushort addr)
    {
        State.S = addr;
        _nmiArmed = true;
        return 4;
    }

    private int Leau(ushort addr)
    {
        State.U = addr;
        return 4;
    }

    // Stack operations
    private int Pshs(byte mask)
    {
        int cycles = 5;
        if ((mask & 0x80) != 0) { PushS16(State.PC); cycles++; }
        if ((mask & 0x40) != 0) { PushS16(State.U); cycles++; }
        if ((mask & 0x20) != 0) { PushS16(State.Y); cycles++; }
        if ((mask & 0x10) != 0) { PushS16(State.X); cycles++; }
        if ((mask & 0x08) != 0) { PushS8(State.DP); cycles++; }
        if ((mask & 0x04) != 0) { PushS8(State.B); cycles++; }
        if ((mask & 0x02) != 0) { PushS8(State.A); cycles++; }
        if ((mask & 0x01) != 0) { PushS8(State.Flags.ToByte()); cycles++; }
        return cycles;
    }

    private int Puls(byte mask)
    {
        int cycles = 5;
        if ((mask & 0x01) != 0) { State.Flags = M6809Flags.FromByte(PopS8()); cycles++; }
        if ((mask & 0x02) != 0) { State.A = PopS8(); cycles++; }
        if ((mask & 0x04) != 0) { State.B = PopS8(); cycles++; }
        if ((mask & 0x08) != 0) { State.DP = PopS8(); cycles++; }
        if ((mask & 0x10) != 0) { State.X = PopS16(); cycles += 2; }
        if ((mask & 0x20) != 0) { State.Y = PopS16(); cycles += 2; }
        if ((mask & 0x40) != 0) { State.U = PopS16(); cycles += 2; }
        if ((mask & 0x80) != 0) { State.PC = PopS16(); cycles += 2; }
        return cycles;
    }

    private int Pshu(byte mask)
    {
        int cycles = 5;
        if ((mask & 0x80) != 0) { PushU16(State.PC); cycles++; }
        if ((mask & 0x40) != 0) { PushU16(State.S); cycles++; }
        if ((mask & 0x20) != 0) { PushU16(State.Y); cycles++; }
        if ((mask & 0x10) != 0) { PushU16(State.X); cycles++; }
        if ((mask & 0x08) != 0) { PushU8(State.DP); cycles++; }
        if ((mask & 0x04) != 0) { PushU8(State.B); cycles++; }
        if ((mask & 0x02) != 0) { PushU8(State.A); cycles++; }
        if ((mask & 0x01) != 0) { PushU8(State.Flags.ToByte()); cycles++; }
        return cycles;
    }

    private int Pulu(byte mask)
    {
        int cycles = 5;
        if ((mask & 0x01) != 0) { State.Flags = M6809Flags.FromByte(PopU8()); cycles++; }
        if ((mask & 0x02) != 0) { State.A = PopU8(); cycles++; }
        if ((mask & 0x04) != 0) { State.B = PopU8(); cycles++; }
        if ((mask & 0x08) != 0) { State.DP = PopU8(); cycles++; }
        if ((mask & 0x10) != 0) { State.X = PopU16(); cycles += 2; }
        if ((mask & 0x20) != 0) { State.Y = PopU16(); cycles += 2; }
        if ((mask & 0x40) != 0) { State.S = PopU16(); cycles += 2; _nmiArmed = true; }
        if ((mask & 0x80) != 0) { State.PC = PopU16(); cycles += 2; }
        return cycles;
    }

    // Return/jump
    private int Abx()
    {
        State.X = (ushort)(State.X + State.B);
        return 3;
    }

    private int Rti()
    {
        State.Flags = M6809Flags.FromByte(PopS8());
        if (State.Flags.E)
        {
            State.A = PopS8();
            State.B = PopS8();
            State.DP = PopS8();
            State.X = PopS16();
            State.Y = PopS16();
            State.U = PopS16();
            State.PC = PopS16();
            return 15;
        }
        else
        {
            State.PC = PopS16();
            return 6;
        }
    }

    private int Cwai(byte mask)
    {
        State.Flags = M6809Flags.FromByte((byte)(State.Flags.ToByte() & mask));
        State.Flags.E = true;
        PushFullFrame();
        State.Halted = true;
        return 20;
    }

    private int Mul()
    {
        ushort result = (ushort)(State.A * State.B);
        State.D = result;
        State.Flags.Z = result == 0;
        State.Flags.C = (State.B & 0x80) != 0;
        return 11;
    }

    private int Swi()
    {
        PushFullFrame();
        State.Flags.I = true;
        State.Flags.F = true;
        State.PC = Read16(0xFFFA);
        return 19;
    }

    private int Swi2()
    {
        PushFullFrame();
        State.PC = Read16(0xFFF4);
        return 20;
    }

    private int Swi3()
    {
        PushFullFrame();
        State.PC = Read16(0xFFF2);
        return 20;
    }

    private int SubD(ushort val)
    {
        ushort result = (ushort)(State.D - val);
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.D ^ val) & (State.D ^ result) & 0x8000) != 0;
        State.Flags.C = State.D < val;
        State.D = result;
        return 4;
    }

    private int CmpD(ushort val)
    {
        ushort result = (ushort)(State.D - val);
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.D ^ val) & (State.D ^ result) & 0x8000) != 0;
        State.Flags.C = State.D < val;
        return 5;
    }

    private int CmpY(ushort val)
    {
        ushort result = (ushort)(State.Y - val);
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.Y ^ val) & (State.Y ^ result) & 0x8000) != 0;
        State.Flags.C = State.Y < val;
        return 5;
    }

    private int CmpU(ushort val)
    {
        ushort result = (ushort)(State.U - val);
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.U ^ val) & (State.U ^ result) & 0x8000) != 0;
        State.Flags.C = State.U < val;
        return 5;
    }

    private int CmpS(ushort val)
    {
        ushort result = (ushort)(State.S - val);
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.S ^ val) & (State.S ^ result) & 0x8000) != 0;
        State.Flags.C = State.S < val;
        return 5;
    }

    private int JsrD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        PushS16(State.PC);
        State.PC = addr;
        return 7;
    }

    private int JsrIdx()
    {
        ushort addr = FetchIndexed(out int ex);
        PushS16(State.PC);
        State.PC = addr;
        return 7 + ex;
    }

    private int JsrExt()
    {
        ushort addr = FetchExtended();
        PushS16(State.PC);
        State.PC = addr;
        return 8;
    }

    private int AddD(ushort val)
    {
        ushort result = (ushort)(State.D + val);
        State.Flags.H = ((State.D ^ val ^ result) & 0x100) != 0;
        State.Flags.N = (result & 0x8000) != 0;
        State.Flags.Z = result == 0;
        State.Flags.V = ((State.D ^ val ^ 0x8000) & (State.D ^ result) & 0x8000) != 0;
        State.Flags.C = result < State.D;
        State.D = result;
        return 4;
    }

    private int LddI(ushort val)
    {
        State.D = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 3;
    }

    private int LduI(ushort val)
    {
        State.U = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 3;
    }

    private int LddD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        State.D = Read16(addr);
        State.Flags.N = (State.D & 0x8000) != 0;
        State.Flags.Z = State.D == 0;
        State.Flags.V = false;
        return 5;
    }

    private int StdD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        Write16(addr, State.D);
        State.Flags.N = (State.D & 0x8000) != 0;
        State.Flags.Z = State.D == 0;
        State.Flags.V = false;
        return 5;
    }

    private int LduD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        State.U = Read16(addr);
        State.Flags.N = (State.U & 0x8000) != 0;
        State.Flags.Z = State.U == 0;
        State.Flags.V = false;
        return 5;
    }

    private int StuD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        Write16(addr, State.U);
        State.Flags.N = (State.U & 0x8000) != 0;
        State.Flags.Z = State.U == 0;
        State.Flags.V = false;
        return 5;
    }

    private int LddIdx()
    {
        ushort val = Ld16Indexed(out int ex);
        State.D = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int StdIdx()
    {
        ushort addr = FetchIndexed(out int ex);
        Write16(addr, State.D);
        State.Flags.N = (State.D & 0x8000) != 0;
        State.Flags.Z = State.D == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int LduIdx()
    {
        ushort val = Ld16Indexed(out int ex);
        State.U = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int StuIdx()
    {
        ushort addr = FetchIndexed(out int ex);
        Write16(addr, State.U);
        State.Flags.N = (State.U & 0x8000) != 0;
        State.Flags.Z = State.U == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int LddExt()
    {
        ushort val = Ld16Extended();
        State.D = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 6;
    }

    private int StdExt()
    {
        ushort addr = FetchExtended();
        Write16(addr, State.D);
        State.Flags.N = (State.D & 0x8000) != 0;
        State.Flags.Z = State.D == 0;
        State.Flags.V = false;
        return 6;
    }

    private int LduExt()
    {
        ushort val = Ld16Extended();
        State.U = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 6;
    }

    private int StuExt()
    {
        ushort addr = FetchExtended();
        Write16(addr, State.U);
        State.Flags.N = (State.U & 0x8000) != 0;
        State.Flags.Z = State.U == 0;
        State.Flags.V = false;
        return 6;
    }

    // Page 0x10 specific instructions
    private int LdyI(ushort val)
    {
        State.Y = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 4;
    }

    private int LdyD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        State.Y = Read16(addr);
        State.Flags.N = (State.Y & 0x8000) != 0;
        State.Flags.Z = State.Y == 0;
        State.Flags.V = false;
        return 6;
    }

    private int LdyIdx()
    {
        ushort val = Ld16Indexed(out int ex);
        State.Y = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 6 + ex;
    }

    private int LdyExt()
    {
        ushort val = Ld16Extended();
        State.Y = val;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 7;
    }

    private int StyD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        Write16(addr, State.Y);
        State.Flags.N = (State.Y & 0x8000) != 0;
        State.Flags.Z = State.Y == 0;
        State.Flags.V = false;
        return 5;
    }

    private int StyIdx()
    {
        ushort addr = FetchIndexed(out int ex);
        Write16(addr, State.Y);
        State.Flags.N = (State.Y & 0x8000) != 0;
        State.Flags.Z = State.Y == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int StyExt()
    {
        ushort addr = FetchExtended();
        Write16(addr, State.Y);
        State.Flags.N = (State.Y & 0x8000) != 0;
        State.Flags.Z = State.Y == 0;
        State.Flags.V = false;
        return 6;
    }

    private int LdsI(ushort val)
    {
        State.S = val;
        _nmiArmed = true;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 4;
    }

    private int LdsD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        State.S = Read16(addr);
        _nmiArmed = true;
        State.Flags.N = (State.S & 0x8000) != 0;
        State.Flags.Z = State.S == 0;
        State.Flags.V = false;
        return 6;
    }

    private int LdsIdx()
    {
        ushort val = Ld16Indexed(out int ex);
        State.S = val;
        _nmiArmed = true;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 6 + ex;
    }

    private int LdsExt()
    {
        ushort val = Ld16Extended();
        State.S = val;
        _nmiArmed = true;
        State.Flags.N = (val & 0x8000) != 0;
        State.Flags.Z = val == 0;
        State.Flags.V = false;
        return 7;
    }

    private int StsD()
    {
        ushort addr = (ushort)(((State.DP) << 8) | Fetch());
        Write16(addr, State.S);
        State.Flags.N = (State.S & 0x8000) != 0;
        State.Flags.Z = State.S == 0;
        State.Flags.V = false;
        return 5;
    }

    private int StsIdx()
    {
        ushort addr = FetchIndexed(out int ex);
        Write16(addr, State.S);
        State.Flags.N = (State.S & 0x8000) != 0;
        State.Flags.Z = State.S == 0;
        State.Flags.V = false;
        return 5 + ex;
    }

    private int StsExt()
    {
        ushort addr = FetchExtended();
        Write16(addr, State.S);
        State.Flags.N = (State.S & 0x8000) != 0;
        State.Flags.Z = State.S == 0;
        State.Flags.V = false;
        return 6;
    }

    #endregion

    #region Stack Helpers

    protected void PushS8(byte value)
    {
        State.S--;
        Mmu.Write(State.S, value);
    }

    protected void PushS16(ushort value)
    {
        PushS8((byte)(value & 0xFF));
        PushS8((byte)(value >> 8));
    }

    protected byte PopS8()
    {
        byte value = Mmu.Read(State.S);
        State.S++;
        return value;
    }

    protected ushort PopS16()
    {
        byte hi = PopS8();
        byte lo = PopS8();
        return (ushort)((hi << 8) | lo);
    }

    protected void PushU8(byte value)
    {
        State.U--;
        Mmu.Write(State.U, value);
    }

    protected void PushU16(ushort value)
    {
        PushU8((byte)(value & 0xFF));
        PushU8((byte)(value >> 8));
    }

    protected byte PopU8()
    {
        byte value = Mmu.Read(State.U);
        State.U++;
        return value;
    }

    protected ushort PopU16()
    {
        byte hi = PopU8();
        byte lo = PopU8();
        return (ushort)((hi << 8) | lo);
    }

    protected void PushFullFrame()
    {
        State.Flags.E = true;
        PushS8(State.Flags.ToByte());
        PushS8(State.A);
        PushS8(State.B);
        PushS8(State.DP);
        PushS16(State.X);
        PushS16(State.Y);
        PushS16(State.U);
        PushS16(State.PC);
    }

    protected void PushFastFrame()
    {
        State.Flags.E = false;
        PushS8(State.Flags.ToByte());
        PushS16(State.PC);
    }

    #endregion

    public override void StepInstruction() => Step();

    public override void SetIRQ(bool active) => _irqPending = active;

    public override void SetNMI(bool active)
    {
        if (active)
            _nmiPending = true;
    }

    #region Helper Methods

    public new byte Fetch()
    {
        byte value = Mmu.Read(State.PC);
        State.PC++;
        return value;
    }

    public new ushort Fetch16()
    {
        byte hi = Fetch();
        byte lo = Fetch();
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>Read a 16-bit big-endian value from memory (HIGH byte first).</summary>
    protected new ushort Read16(ushort address)
    {
        byte hi = Mmu.Read(address);
        byte lo = Mmu.Read((ushort)(address + 1));
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>Write a 16-bit big-endian value to memory (HIGH byte first).</summary>
    protected new void Write16(ushort address, ushort value)
    {
        Mmu.Write(address, (byte)(value >> 8));
        Mmu.Write((ushort)(address + 1), (byte)value);
    }

    private void SetNZ(byte r)
    {
        State.Flags.N = (r & 0x80) != 0;
        State.Flags.Z = r == 0;
    }

    private void SetNZ16(ushort r)
    {
        State.Flags.N = (r & 0x8000) != 0;
        State.Flags.Z = r == 0;
    }

    #endregion
}
