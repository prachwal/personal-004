using PetEmulator.Core;

namespace PetEmulator.Cpu8080;

/// <summary>Initial shared-Core implementation of the Intel 8080 lifecycle.</summary>
public sealed partial class Cpu8080 : CpuProcessorBase<Cpu8080State>
{
    private bool _interruptPending;

    public Cpu8080(IMemoryBus memory, IClock? clock = null, IPortBus? ports = null)
        : base(new Cpu8080State(), memory, clock, ports)
    {
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
        => definition.Execute(State, ExecutionContext);

    protected override bool TryServiceInterrupt(out CpuStepResult result)
    {
        result = default;
        return false;
    }
}
