using PetEmulator.Core;

namespace PetEmulator.Cpu6800;

/// <summary>Base execution lifecycle for Motorola 6800-family processors.</summary>
public abstract partial class M6800Cpu : IProcessor, IDebuggableProcessor
{
    protected readonly IMemoryBus Mmu;
    protected readonly M6800OpcodeTable Opcodes;

    protected M6800Cpu(IMemoryBus memory, M6800State state, M6800OpcodeTable opcodes)
    {
        Mmu = memory ?? throw new ArgumentNullException(nameof(memory));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Opcodes = opcodes ?? throw new ArgumentNullException(nameof(opcodes));
    }

    public M6800State State { get; }
    public ulong CycleCount => checked((ulong)State.Cycles);
    public ulong InstructionCount { get; protected set; }
    public bool Halted => State.Halted;

    protected virtual M6800Flags ConditionCodes => State.Flags;

    public virtual IReadOnlyDictionary<string, ulong> GetRegisters() => new Dictionary<string, ulong>
    {
        ["PC"] = State.PC,
        ["A"] = State.A,
        ["B"] = State.B,
        ["X"] = State.X,
        ["SP"] = State.StackPointer,
        ["P"] = State.Flags.ToByte(),
    };

    public virtual void Reset()
    {
        State.Reset();
        InstructionCount = 0;
        State.PC = Read16(0xFFFE);
    }

    public virtual int Step()
    {
        var cyclesBefore = State.Cycles;
        StepInstruction();
        return checked((int)(State.Cycles - cyclesBefore));
    }

    public virtual void StepInstruction()
    {
        if (State.Halted)
            return;

        byte opcode = Fetch();
        int cycles = ExecuteOpcode(opcode);
        InstructionCount++;
        State.Cycles += cycles;
    }

    public virtual void SetIRQ(bool active)
    {
    }

    public virtual void SetNMI(bool active)
    {
    }

    protected abstract int ExecuteOpcode(byte opcode);

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
