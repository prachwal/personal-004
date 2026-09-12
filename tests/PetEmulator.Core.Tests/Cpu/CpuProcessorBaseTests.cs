using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;

namespace PetEmulator.Core.Tests;

[TestFixture]
public sealed class CpuProcessorBaseTests
{
    [Test]
    public void Base_lifecycle_accounts_cycles_and_completed_instructions()
    {
        var cpu = new TestCpu();

        cpu.StepInstruction();
        cpu.Step();

        cpu.CycleCount.Should().Be(6);
        cpu.InstructionCount.Should().Be(2);
        cpu.Executed.Should().Be(200);
    }

    [Test]
    public void Reset_resets_state_clock_and_instruction_count()
    {
        var cpu = new TestCpu();

        cpu.StepInstruction();
        cpu.SetProgramCounter(0x1234);
        cpu.Reset();

        cpu.CycleCount.Should().Be(0);
        cpu.InstructionCount.Should().Be(0);
        cpu.GetRegisters()["PC"].Should().Be(0);
    }

    [Test]
    public void Halted_reflects_state_and_reset_clears_it()
    {
        var cpu = new TestCpu();

        cpu.SetHalted(true);
        cpu.Halted.Should().BeTrue();

        cpu.Reset();

        cpu.Halted.Should().BeFalse();
    }

    [Test]
    public void Interrupt_hook_completes_step_without_counting_an_instruction()
    {
        var cpu = new TestCpu
        {
            InterruptResult = CpuStepResult.Interrupt(7)
        };
        cpu.InterruptPending = true;

        cpu.StepInstruction();

        cpu.CycleCount.Should().Be(7);
        cpu.InstructionCount.Should().Be(0);
        cpu.InterruptsServiced.Should().Be(1);
        cpu.Executed.Should().Be(0);
    }

    [Test]
    public void Wait_hook_advances_cycles_without_executing_an_instruction()
    {
        var cpu = new TestCpu
        {
            WaitResult = CpuStepResult.Idle(4, waiting: true)
        };
        cpu.WaitPending = true;

        cpu.StepInstruction();

        cpu.CycleCount.Should().Be(4);
        cpu.InstructionCount.Should().Be(0);
        cpu.Executed.Should().Be(0);
    }

    [Test]
    public void Zero_cycle_result_is_valid_and_does_not_advance_clock()
    {
        var cpu = new TestCpu
        {
            ExecutionResult = CpuStepResult.Completed(0)
        };

        cpu.StepInstruction();

        cpu.CycleCount.Should().Be(0);
        cpu.InstructionCount.Should().Be(1);
    }

    [Test]
    public void SetIRQ_and_SetNMI_are_family_hooks()
    {
        var cpu = new TestCpu();

        cpu.SetIRQ(true);
        cpu.SetNMI(true);

        cpu.LastIrq.Should().BeTrue();
        cpu.LastNmi.Should().BeTrue();
    }

    [Test]
    public void Prefixed_opcode_is_resolved_by_the_same_table()
    {
        var cpu = new TestCpu
        {
            CurrentOpcode = new OpcodeKey(0x10, 0x42)
        };

        cpu.StepInstruction();

        cpu.Executed.Should().Be(1000);
        cpu.CycleCount.Should().Be(5);
    }

    [Test]
    public void Handler_exception_is_not_converted_or_counted()
    {
        var cpu = new TestCpu { ThrowFromOpcode = true };

        var action = () => cpu.StepInstruction();

        action.Should().Throw<InvalidOperationException>().WithMessage("opcode failure");
        cpu.CycleCount.Should().Be(0);
        cpu.InstructionCount.Should().Be(0);
    }

    [Test]
    public void Derived_configuration_can_add_and_replace_opcodes()
    {
        var cpu = new TestCpu();

        cpu.RegisteredOpcodes.Should().ContainSingle(x => x.Key == OpcodeKey.Base(0x42));
        cpu.StepInstruction();

        cpu.Executed.Should().Be(100);
    }

    [Test]
    public void Opcode_table_rejects_duplicate_add_and_allows_explicit_replace()
    {
        var table = new OpcodeTable<TestState>();
        var original = Definition(1);
        var replacement = Definition(2);

        table.Add(original);
        var action = () => table.Add(original);

        action.Should().Throw<InvalidOperationException>();
        table.Replace(replacement);
        table.Get(OpcodeKey.Base(0x42)).Execute(new TestState(), new CpuExecutionContext(new TestMemory(), new EmulationClock())).Cycles.Should().Be(2);
        table.TryGet(OpcodeKey.Base(0x42), out var found).Should().BeTrue();
        found.Should().Be(replacement);
        table.TryGet(OpcodeKey.Base(0x99), out _).Should().BeFalse();
        var missing = () => table.Get(OpcodeKey.Base(0x99));
        missing.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void Step_result_factories_preserve_execution_flags()
    {
        CpuStepResult.Completed(3).Should().Be(new CpuStepResult(3, true));
        CpuStepResult.Idle(2, waiting: true).Should().Be(new CpuStepResult(2, false, Waiting: true));
        CpuStepResult.Interrupt(7).Should().Be(new CpuStepResult(7, false, InterruptServiced: true));
    }

    [Test]
    public void Execution_context_exposes_shared_memory_clock_and_optional_ports()
    {
        var memory = new TestMemory();
        var clock = new EmulationClock();
        var ports = new TestPorts();

        var context = new CpuExecutionContext(memory, clock, ports);

        context.Memory.Should().BeSameAs(memory);
        context.Clock.Should().BeSameAs(clock);
        context.Ports.Should().BeSameAs(ports);
    }

    [Test]
    public void Lifecycle_hooks_are_called_once_per_step_and_reset()
    {
        var cpu = new TestCpu();

        cpu.StepInstruction();
        cpu.Reset();

        cpu.BeforeSteps.Should().Be(1);
        cpu.ResetHooks.Should().Be(1);
    }

    [Test]
    public void Snapshot_captures_registers_counters_and_halt_state()
    {
        var cpu = new TestCpu();
        cpu.SetProgramCounter(0x1234);
        cpu.SetHalted(true);
        cpu.StepInstruction();

        var snapshot = cpu.CaptureSnapshot();

        snapshot.Registers["PC"].Should().Be(0x1234);
        snapshot.Halted.Should().BeTrue();
        snapshot.CycleCount.Should().Be(3);
        snapshot.InstructionCount.Should().Be(1);
    }

    [Test]
    public void Snapshot_restore_restores_state_clock_and_instruction_count()
    {
        var cpu = new TestCpu();
        cpu.SetProgramCounter(0x1234);
        cpu.StepInstruction();
        var snapshot = cpu.CaptureSnapshot();

        cpu.SetProgramCounter(0x5678);
        cpu.StepInstruction();
        cpu.RestoreSnapshot(snapshot);

        cpu.GetRegisters()["PC"].Should().Be(0x1234);
        cpu.CycleCount.Should().Be(3);
        cpu.InstructionCount.Should().Be(1);
    }

    [Test]
    public void Observer_receives_completed_trace_with_opcode_and_mnemonic()
    {
        var observer = new TestObserver();
        var cpu = new TestCpu { ExecutionObserver = observer };

        cpu.StepInstruction();

        observer.Completed.Should().ContainSingle();
        var trace = observer.Completed[0];
        trace.Opcode.Should().Be(OpcodeKey.Base(0x42));
        trace.Mnemonic.Should().Be("TST");
        trace.Result.InstructionCompleted.Should().BeTrue();
        trace.Before.InstructionCount.Should().Be(0);
        trace.After.InstructionCount.Should().Be(1);
    }

    [Test]
    public void Observer_can_stop_before_execution_with_breakpoint_result()
    {
        var observer = new TestObserver { Break = true };
        var cpu = new TestCpu { ExecutionObserver = observer };

        cpu.StepInstruction();

        cpu.Executed.Should().Be(0);
        cpu.CycleCount.Should().Be(0);
        observer.Completed[0].Result.BreakpointHit.Should().BeTrue();
    }

    [Test]
    public void Observer_receives_failed_step_and_exception_is_rethrown()
    {
        var observer = new TestObserver();
        var cpu = new TestCpu { ExecutionObserver = observer, ThrowFromOpcode = true };

        var action = () => cpu.StepInstruction();

        action.Should().Throw<InvalidOperationException>().WithMessage("opcode failure");
        observer.Failures.Should().ContainSingle().Which.Message.Should().Be("opcode failure");
    }

    [Test]
    public void Observer_can_mark_memory_access_as_watchpoint_hit()
    {
        var observer = new TestObserver { WatchAddress = 0x1234 };
        var cpu = new TestCpu { ExecutionObserver = observer, ReadMemoryDuringOpcode = true };

        cpu.StepInstruction();

        observer.Accesses.Should().ContainSingle(x => x.Address == 0x1234);
        observer.Completed[0].Result.WatchpointHit.Should().BeTrue();
    }

    private static OpcodeDefinition<TestState> Definition(ulong cycles)
        => new(OpcodeKey.Base(0x42), "TST", 1, (byte)cycles, "Implied", (_, _) => CpuStepResult.Completed(cycles));

    private sealed class TestCpu : CpuProcessorBase<TestState>
    {
        public TestCpu()
            : base(new TestState(), new TestMemory())
        {
            InitializeOpcodes();
        }

        public int Executed { get; private set; }

        public bool InterruptPending { get; set; }

        public bool WaitPending { get; set; }

        public bool ThrowFromOpcode { get; set; }

        public bool ReadMemoryDuringOpcode { get; set; }

        public OpcodeKey CurrentOpcode { get; set; } = OpcodeKey.Base(0x42);

        public CpuStepResult InterruptResult { get; set; } = CpuStepResult.Interrupt(0);

        public CpuStepResult WaitResult { get; set; } = CpuStepResult.Idle(0, waiting: true);

        public CpuStepResult ExecutionResult { get; set; } = CpuStepResult.Completed(3);

        public int InterruptsServiced { get; private set; }

        public int BeforeSteps { get; private set; }

        public int ResetHooks { get; private set; }

        public bool? LastIrq { get; private set; }

        public bool? LastNmi { get; private set; }

        public IReadOnlyCollection<OpcodeDefinition<TestState>> RegisteredOpcodes => Opcodes.Entries;

        public void SetProgramCounter(ushort value) => State.ProgramCounter = value;

        public void SetHalted(bool value) => State.Halted = value;

        protected override void ConfigureOpcodes(OpcodeTable<TestState> table)
        {
            table.Add(new OpcodeDefinition<TestState>(
                OpcodeKey.Base(0x42), "TST", 1, 3, "Implied",
                (_, context) =>
                {
                    Executed += 100;
                    if (ThrowFromOpcode)
                        throw new InvalidOperationException("opcode failure");

                    if (ReadMemoryDuringOpcode)
                        context.Memory.Read(0x1234);

                    return ExecutionResult;
                }));

            table.Add(new OpcodeDefinition<TestState>(
                new OpcodeKey(0x10, 0x42), "PFX", 2, 5, "Implied",
                (_, _) =>
                {
                    Executed += 1000;
                    return CpuStepResult.Completed(5);
                }));
        }

        protected override OpcodeKey FetchOpcode() => CurrentOpcode;

        protected override void BeforeStep() => BeforeSteps++;

        protected override void OnReset() => ResetHooks++;

        protected override bool TryServiceInterrupt(out CpuStepResult result)
        {
            if (!InterruptPending)
            {
                result = default;
                return false;
            }

            InterruptPending = false;
            InterruptsServiced++;
            result = InterruptResult;
            return true;
        }

        protected override bool TryHandleWait(out CpuStepResult result)
        {
            if (!WaitPending)
            {
                result = default;
                return false;
            }

            WaitPending = false;
            result = WaitResult;
            return true;
        }

        public override void SetIRQ(bool active) => LastIrq = active;

        public override void SetNMI(bool active) => LastNmi = active;

        protected override OpcodeDefinition<TestState> DecodeOpcode(OpcodeKey key) => Opcodes.Get(key);

        protected override CpuStepResult ExecuteOpcode(OpcodeDefinition<TestState> definition)
            => definition.Execute(State, ExecutionContext);
    }

    private sealed class TestState : CpuState
    {
        public ushort ProgramCounter { get; set; }

        public override void Reset()
        {
            base.Reset();
            ProgramCounter = 0;
        }

        public override void RestoreSnapshot(CpuStateSnapshot snapshot)
        {
            ProgramCounter = (ushort)snapshot.Registers["PC"];
            Halted = snapshot.Halted;
        }

        public override IReadOnlyDictionary<string, ulong> GetRegisters()
            => new Dictionary<string, ulong> { ["PC"] = ProgramCounter };
    }

    private sealed class TestMemory : IMemoryBus, IMemoryAccessObservable
    {
        public event Action<BusAccess>? Accessed;

        public byte Read(ushort address)
        {
            Accessed?.Invoke(new BusAccess(IsWrite: false, address, 0));
            return 0;
        }

        public void Write(ushort address, byte value)
        {
        }
    }

    private sealed class TestPorts : IPortBus
    {
        public byte Read(ushort port) => 0;

        public void Write(ushort port, byte value)
        {
        }
    }

    private sealed class TestObserver : ICpuExecutionObserver
    {
        public bool Break { get; set; }

        public List<CpuStepTrace> Completed { get; } = new();

        public List<Exception> Failures { get; } = new();

        public List<BusAccess> Accesses { get; } = new();

        public ushort? WatchAddress { get; set; }

        public bool ShouldBreak(CpuDebugSnapshot snapshot) => Break;

        public bool ShouldBreakOnMemoryAccess(BusAccess access)
        {
            Accesses.Add(access);
            return WatchAddress == access.Address;
        }

        public void OnStepCompleted(CpuStepTrace trace) => Completed.Add(trace);

        public void OnStepFailed(CpuDebugSnapshot snapshot, Exception exception) => Failures.Add(exception);
    }
}
