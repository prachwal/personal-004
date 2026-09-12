namespace PetEmulator.Core;

/// <summary>
/// Shared processor lifecycle for CPU families with family-specific state and execution.
/// </summary>
public abstract class CpuProcessorBase<TState> : IProcessor, IDebuggableProcessor
    where TState : CpuState
{
    protected CpuProcessorBase(TState state, IMemoryBus memory, IClock? clock = null, IPortBus? ports = null)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        Clock = clock ?? new EmulationClock();
        ExecutionContext = new CpuExecutionContext(Memory, Clock, ports);
        Opcodes = new OpcodeTable<TState>();
    }

    protected TState State { get; }

    protected IMemoryBus Memory { get; }

    protected IClock Clock { get; }

    protected CpuExecutionContext ExecutionContext { get; }

    protected OpcodeTable<TState> Opcodes { get; }

    /// <summary>
    /// Registers the current processor's opcode set after its constructor has initialized.
    /// Derived constructors should call this once after their own fields are ready.
    /// </summary>
    protected void InitializeOpcodes() => ConfigureOpcodes(Opcodes);

    public bool Halted => State.Halted;

    public ulong CycleCount => Clock.CycleCount;

    public ulong InstructionCount { get; private set; }

    public void Reset()
    {
        State.Reset();
        Clock.Reset();
        InstructionCount = 0;
        OnReset();
    }

    /// <summary>Executes one lifecycle step and preserves the public IProcessor contract.</summary>
    public void StepInstruction()
    {
        var result = ExecuteStep();

        if (result.InstructionCompleted)
            InstructionCount++;
    }

    /// <summary>Convenience alias used by processor-facing code.</summary>
    public void Step() => StepInstruction();

    public virtual void SetIRQ(bool active)
    {
    }

    public virtual void SetNMI(bool active)
    {
    }

    public IReadOnlyDictionary<string, ulong> GetRegisters()
        => State.GetRegisters();

    /// <summary>
    /// Defines the common lifecycle. Families customize its stages through protected hooks.
    /// </summary>
    protected virtual CpuStepResult ExecuteStep()
    {
        BeforeStep();

        if (TryServiceInterrupt(out var interruptResult))
            return CompleteStep(interruptResult);

        if (TryHandleWait(out var waitResult))
            return CompleteStep(waitResult);

        var opcode = FetchOpcode();
        var definition = DecodeOpcode(opcode);
        return CompleteStep(ExecuteOpcode(definition));
    }

    protected virtual void BeforeStep()
    {
    }

    protected virtual bool TryServiceInterrupt(out CpuStepResult result)
    {
        result = default;
        return false;
    }

    protected virtual bool TryHandleWait(out CpuStepResult result)
    {
        result = default;
        return false;
    }

    protected abstract OpcodeKey FetchOpcode();

    protected abstract OpcodeDefinition<TState> DecodeOpcode(OpcodeKey key);

    protected abstract CpuStepResult ExecuteOpcode(OpcodeDefinition<TState> definition);

    protected virtual void ConfigureOpcodes(OpcodeTable<TState> table)
    {
    }

    protected virtual void OnReset()
    {
    }

    protected virtual CpuStepResult CompleteStep(CpuStepResult result)
    {
        Clock.Advance(result.Cycles);
        return result;
    }
}
