using PetEmulator.Core;
using PetEmulator.Cpu6800;

namespace PetEmulator.Cpu6809;

public partial class M6809Cpu : M6800Cpu, IFirqProcessor
{
protected OpcodeRegistrationTable OpcodeTable = null!;

    protected override int ShortBranchCycles => 3;
    protected override int BranchSubroutineCycles => 7;

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

    protected OpcodeRegistrationTable Page10OpcodeTable = null!;
    protected OpcodeRegistrationTable Page11OpcodeTable = null!;

    public M6809Cpu(IMemoryBus memory)
        : this(memory, new M6809State())
    {
    }

    /// <summary>Constructor for derived classes that use a custom state type.</summary>
    protected M6809Cpu(IMemoryBus memory, M6809State state)
        : base(memory, state)
    {
        FillOpcodeTable();
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
        base.Reset();
        _irqPending = false;
        _firqPending = false;
        _nmiPending = false;
        _nmiArmed = false;
    }

    /// <summary>Execute one instruction or interrupt dispatch; returns cycles elapsed.</summary>
    public override int Step()
    {
        var cyclesBefore = State.Cycles;
        StepInstruction();
        return checked((int)(State.Cycles - cyclesBefore));
    }

    protected override CpuStepResult ExecuteStep()
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
            return CompleteStep(CpuStepResult.Interrupt(19));
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
            return CompleteStep(CpuStepResult.Interrupt(10));
        }

        // Step 3: Check IRQ (level-triggered, blocked by I flag)
        if (_irqPending && !State.Flags.I)
        {
            _irqPending = false;
            State.Halted = false;
            PushFullFrame();
            State.Flags.I = true;
            State.PC = Read16(0xFFF8);
            return CompleteStep(CpuStepResult.Interrupt(19));
        }

        // Step 4: Check HALTed state (SYNC/CWAI waiting)
        if (State.Halted)
        {
            return CompleteStep(CpuStepResult.Idle(2));
        }

        // Step 5: Normal instruction execution
        return base.ExecuteStep();
    }

    protected override OpcodeKey FetchOpcode()
    {
        var opcode = Fetch();
        return opcode is 0x10 or 0x11
            ? new OpcodeKey(opcode, Fetch())
            : OpcodeKey.Base(opcode);
    }

    protected override OpcodeDefinition<M6800State> DecodeOpcode(OpcodeKey key)
        => GetCommonOpcode(key);
}
