namespace PetEmulator.Core;

/// <summary>
/// Shared processor lifecycle for CPU families with family-specific state and execution.
/// </summary>
public abstract class CpuProcessorBase<TState> : IProcessor, IDebuggableProcessor
    where TState : CpuState
{
    private OpcodeKey? _currentOpcode;
    private string? _currentMnemonic;
    private bool _watchpointHit;

    protected CpuProcessorBase(TState state, IMemoryBus memory, IClock? clock = null, IPortBus? ports = null)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        Clock = clock ?? new EmulationClock();
        ExecutionContext = new CpuExecutionContext(Memory, Clock, ports);
        Opcodes = new OpcodeTable<TState>();

        if (Memory is IMemoryAccessObservable observable)
            observable.Accessed += OnMemoryAccess;
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

    public ulong InstructionCount { get; protected set; }

    /// <summary>Optional debugger or monitoring integration.</summary>
    public ICpuExecutionObserver? ExecutionObserver { get; set; }

    public virtual void Reset()
    {
        State.Reset();
        Clock.Reset();
        InstructionCount = 0;
        OnReset();
    }

    /// <summary>Captures state, clock and instruction count for debugging or restore.</summary>
    public CpuDebugSnapshot CaptureSnapshot()
        => new(State.CaptureSnapshot(), CycleCount, InstructionCount);

    /// <summary>Restores state, clock and instruction count from a previous snapshot.</summary>
    public virtual void RestoreSnapshot(CpuDebugSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        State.RestoreSnapshot(snapshot.State);
        Clock.Reset();
        Clock.Advance(snapshot.CycleCount);
        InstructionCount = snapshot.InstructionCount;
    }

    /// <summary>Executes one lifecycle step and preserves the public IProcessor contract.</summary>
    public virtual void StepInstruction()
    {
        var observer = ExecutionObserver;
        var before = observer is null ? null : CaptureSnapshot();
        _currentOpcode = null;
        _currentMnemonic = null;
        _watchpointHit = false;

        if (observer?.ShouldBreak(before!) == true)
        {
            var breakpoint = CpuStepResult.Breakpoint();
            observer.OnStepCompleted(new CpuStepTrace(before!, CaptureSnapshot(), null, null, breakpoint));
            return;
        }

        CpuStepResult result;
        try
        {
            result = ExecuteStep();
        }
        catch (Exception exception)
        {
            observer?.OnStepFailed(before!, exception);
            throw;
        }

        if (_watchpointHit)
            result = result with { WatchpointHit = true };

        if (result.InstructionCompleted)
            InstructionCount++;

        observer?.OnStepCompleted(
            new CpuStepTrace(before!, CaptureSnapshot(), _currentOpcode, _currentMnemonic, result));
    }

    /// <summary>Convenience alias used by processor-facing code.</summary>
    public void Step() => StepInstruction();

    public virtual void SetIRQ(bool active)
    {
    }

    public virtual void SetNMI(bool active)
    {
    }

    public virtual IReadOnlyDictionary<string, ulong> GetRegisters()
        => State.GetRegisters();

    public virtual IReadOnlyCollection<string> EightBitRegisterNames
        => State.EightBitRegisterNames;

    /// <summary>
    /// Defines the common lifecycle. Families customize its stages through protected hooks.
    /// </summary>
    protected virtual CpuStepResult ExecuteStep()
    {
        BeforeStep();

        if (TryHandleWaitBeforeInterrupt(out var preInterruptWaitResult))
            return CompleteStep(preInterruptWaitResult);

        if (TryServiceInterrupt(out var interruptResult))
            return CompleteStep(interruptResult);

        if (TryHandleWait(out var waitResult))
            return CompleteStep(waitResult);

        if (TryHandleHalt(out var haltResult))
            return CompleteStep(haltResult);

        var opcode = FetchOpcode();
        _currentOpcode = opcode;
        var definition = DecodeOpcode(opcode);
        _currentMnemonic = definition.Mnemonic;
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

    protected virtual bool TryHandleWaitBeforeInterrupt(out CpuStepResult result)
    {
        result = default;
        return false;
    }

    protected virtual bool TryHandleWait(out CpuStepResult result)
    {
        result = default;
        return false;
    }

    protected virtual bool TryHandleHalt(out CpuStepResult result)
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

    /// <summary>Allows family-specific decoders to expose an opcode in custom step flows.</summary>
    protected void SetCurrentOpcode(OpcodeKey opcode, string? mnemonic = null)
    {
        _currentOpcode = opcode;
        _currentMnemonic = mnemonic;
    }

    private void OnMemoryAccess(BusAccess access)
    {
        if (ExecutionObserver?.ShouldBreakOnMemoryAccess(access) == true)
            _watchpointHit = true;
    }
}
