using PetEmulator.Core;

namespace PetEmulator.Cpu8080;

/// <summary>Initial shared-Core implementation of the Intel 8080 lifecycle.</summary>
public partial class Cpu8080 : CpuProcessorBase<Cpu8080State>
{
    private bool _interruptPending;
    private readonly ICpu8080InterruptBus? _interruptBus;

    public Cpu8080(
        IMemoryBus memory,
        IClock? clock = null,
        IPortBus? ports = null,
        ICpu8080InterruptBus? interruptBus = null)
        : base(new Cpu8080State(), memory, clock, ports)
    {
        _interruptBus = interruptBus;
        Reset();
        InitializeOpcodes();
        Opcodes.Seal();
    }

    public Cpu8080State CpuState => State;

    public IReadOnlyCollection<OpcodeDefinition<Cpu8080State>> OpcodeDefinitions => Opcodes.Entries;

    public void RequestInterrupt() => _interruptPending = true;

    public override void Reset()
    {
        base.Reset();
        _interruptPending = false;
    }

    public override void SetIRQ(bool active) => _interruptPending = active;

    protected override OpcodeKey FetchOpcode()
    {
        var opcode = Memory.Read(State.PC);
        State.PC++;
        return OpcodeKey.Base(opcode);
    }

    protected override OpcodeDefinition<Cpu8080State> DecodeOpcode(OpcodeKey key)
        => Opcodes.Get(key);

    protected override CpuStepResult ExecuteOpcode(OpcodeDefinition<Cpu8080State> definition)
    {
        var result = definition.Execute(State, ExecutionContext);
        if (State.EiPending && definition.Key != OpcodeKey.Base(0xFB))
        {
            State.InterruptsEnabled = true;
            State.EiPending = false;
        }

        return result;
    }

    protected override bool TryServiceInterrupt(out CpuStepResult result)
    {
        if (!_interruptPending || !State.InterruptsEnabled)
        {
            result = default;
            return false;
        }

        var interruptOpcode = _interruptBus?.AcknowledgeInterrupt() ?? 0xFF;
        if ((interruptOpcode & 0xC7) != 0xC7)
            throw new InvalidOperationException($"8080 interrupt acknowledge returned non-RST opcode 0x{interruptOpcode:X2}.");

        Push16(State.PC);
        State.PC = (ushort)(((interruptOpcode >> 3) & 7) * 8);
        State.InterruptsEnabled = false;
        State.EiPending = false;
        State.Halted = false;
        _interruptPending = false;
        result = CpuStepResult.Interrupt(11);
        return true;
    }

    protected override bool TryHandleHalt(out CpuStepResult result)
    {
        if (!State.Halted)
        {
            result = default;
            return false;
        }

        result = CpuStepResult.Idle(4);
        return true;
    }

    private void Push16(ushort value)
    {
        State.SP--;
        Memory.Write(State.SP, (byte)(value >> 8));
        State.SP--;
        Memory.Write(State.SP, (byte)value);
    }

    private ushort Pop16()
    {
        var low = Memory.Read(State.SP++);
        var high = Memory.Read(State.SP++);
        return (ushort)((high << 8) | low);
    }
}
