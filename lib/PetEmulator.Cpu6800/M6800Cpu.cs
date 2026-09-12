using PetEmulator.Core;

namespace PetEmulator.Cpu6800;

/// <summary>Base execution lifecycle for Motorola 6800-family processors.</summary>
public partial class M6800Cpu : CpuProcessorBase<M6800State>
{
    public new OpcodeTable<M6800State> Opcodes => base.Opcodes;

    /// <summary>Creates a concrete MC6800 CPU with its standard opcode table.</summary>
    public M6800Cpu(IMemoryBus memory)
        : this(memory, new M6800State())
    {
    }

    /// <summary>
    /// Initializes a 6800-family CPU and registers the MC6800 opcode set in the
    /// shared Core registry.
    /// </summary>
    protected M6800Cpu(IMemoryBus memory, M6800State state)
        : base(state, memory)
    {
        BuildOpcodeTable();
    }

    public new M6800State State => base.State;
    protected IMemoryBus Mmu => Memory;

    protected virtual M6800Flags ConditionCodes => State.Flags;

    public override IReadOnlyDictionary<string, ulong> GetRegisters() => State.GetRegisters();

    public override void Reset()
    {
        base.Reset();
        State.PC = Read16(0xFFFE);
    }

    public new virtual int Step()
    {
        var cyclesBefore = State.Cycles;
        StepInstruction();
        return checked((int)(State.Cycles - cyclesBefore));
    }

    protected void AdvanceCycles(int cycles)
    {
        if (cycles < 0)
            throw new ArgumentOutOfRangeException(nameof(cycles));

        State.Cycles += cycles;
        Clock.Advance((ulong)cycles);
    }

    protected void ResetCycleClock() => Clock.Reset();

    protected override bool TryHandleWait(out CpuStepResult result)
    {
        if (!State.Halted)
        {
            result = default;
            return false;
        }

        result = CpuStepResult.Idle(0);
        return true;
    }

    protected override CpuStepResult CompleteStep(CpuStepResult result)
    {
        var completed = base.CompleteStep(result);
        State.Cycles += (long)result.Cycles;
        return completed;
    }

    protected override OpcodeKey FetchOpcode() => OpcodeKey.Base(Fetch());

    protected override OpcodeDefinition<M6800State> DecodeOpcode(OpcodeKey key)
        => base.Opcodes.Get(key);

    protected OpcodeDefinition<M6800State> GetCommonOpcode(OpcodeKey key)
        => base.Opcodes.Get(key);

    protected void RegisterOpcode(
        OpcodeKey key,
        string mnemonic,
        byte length,
        byte cycles,
        string addressingMode,
        Func<int> execute)
    {
        base.Opcodes.Replace(new OpcodeDefinition<M6800State>(
            key,
            mnemonic,
            length,
            cycles,
            addressingMode,
            (_, _) => CpuStepResult.Completed((ulong)execute())));
    }

    protected override CpuStepResult ExecuteOpcode(OpcodeDefinition<M6800State> definition)
        => definition.Execute(State, ExecutionContext);

    public virtual byte FetchDirect()
    {
        return Mmu.Read(ResolveDirectAddress(Fetch()));
    }

    public virtual byte LdDirect() => FetchDirect();

    public virtual ushort Ld16Direct()
    {
        return Read16(ResolveDirectAddress(Fetch()));
    }

    public virtual ushort FetchExtended() => Fetch16();

    public virtual byte LdExtended() => Mmu.Read(FetchExtended());

    public virtual ushort Ld16Extended() => Read16(FetchExtended());

    protected virtual ushort ResolveDirectAddress(byte offset) => offset;

    protected ushort FetchDirectAddress() => ResolveDirectAddress(Fetch());

    protected byte Fetch()
    {
        byte value = Mmu.Read(State.PC);
        State.PC++;
        return value;
    }

    protected ushort Fetch16()
    {
        byte hi = Fetch();
        byte lo = Fetch();
        return (ushort)((hi << 8) | lo);
    }

    protected ushort Read16(ushort address)
    {
        byte hi = Mmu.Read(address);
        byte lo = Mmu.Read((ushort)(address + 1));
        return (ushort)((hi << 8) | lo);
    }

    protected void Write16(ushort address, ushort value)
    {
        Mmu.Write(address, (byte)(value >> 8));
        Mmu.Write((ushort)(address + 1), (byte)value);
    }
}
