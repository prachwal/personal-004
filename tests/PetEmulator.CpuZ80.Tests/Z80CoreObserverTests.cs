using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80CoreObserverTests
{
    [Fact]
    public void CommonExecutionObserverReceivesZ80InstructionTrace()
    {
        var bus = new TestBus();
        bus.Memory[0] = 0x00;
        var cpu = new Z80Cpu(bus, new InterruptLines());
        var observer = new TestObserver();
        cpu.ExecutionObserver = observer;

        cpu.StepInstruction();

        var trace = Assert.Single(observer.Completed);
        Assert.Equal(OpcodeKey.Base(0x00), trace.Opcode);
        Assert.Equal("OP 00:00", trace.Mnemonic);
        Assert.Equal((ulong)0, trace.Before.InstructionCount);
        Assert.Equal((ulong)1, trace.After.InstructionCount);
        Assert.Equal((ushort)0, trace.Before.State.Registers["PC"]);
        Assert.Equal((ushort)1, trace.After.State.Registers["PC"]);
        Assert.True(trace.Result.InstructionCompleted);
    }

    [Fact]
    public void CommonExecutionObserverReportsZ80MemoryWatchpoint()
    {
        var bus = new TestBus();
        bus.Memory[0] = 0x3A; // LD A,(nn)
        bus.Memory[1] = 0x00;
        bus.Memory[2] = 0x40;
        bus.Memory[0x4000] = 0x5A;
        var cpu = new Z80Cpu(bus, new InterruptLines());
        var observer = new TestObserver { WatchAddress = 0x4000 };
        cpu.ExecutionObserver = observer;

        cpu.StepInstruction();

        var trace = Assert.Single(observer.Completed);
        Assert.True(trace.Result.WatchpointHit);
        Assert.Equal((byte)0x5A, cpu.Registers.A);
        Assert.Contains(observer.Accesses, access => access.Address == 0x4000);
    }

    [Fact]
    public void CommonExecutionObserverCanBreakBeforeZ80Instruction()
    {
        var bus = new TestBus();
        bus.Memory[0] = 0x00;
        var cpu = new Z80Cpu(bus, new InterruptLines());
        var observer = new TestObserver { Break = true };
        cpu.ExecutionObserver = observer;

        cpu.StepInstruction();

        var trace = Assert.Single(observer.Completed);
        Assert.True(trace.Result.BreakpointHit);
        Assert.Equal((ushort)0, cpu.Registers.PC);
        Assert.Equal((ulong)0, cpu.InstructionCount);
    }

    [Fact]
    public void CommonExecutionObserverReceivesZ80ExecutionException()
    {
        var bus = new TestBus();
        bus.Memory[0] = 0x00;
        var cpu = new ThrowingZ80Cpu(bus, new InterruptLines());
        var observer = new TestObserver();
        cpu.ExecutionObserver = observer;

        var exception = Assert.Throws<InvalidOperationException>(() => cpu.StepInstruction());

        Assert.Equal("test opcode failure", exception.Message);
        var failure = Assert.Single(observer.Failures);
        Assert.Same(exception, failure);
    }

    [Fact]
    public void LegacyZ80HookRunsAlongsideCommonExecutionObserver()
    {
        var bus = new TestBus();
        bus.Memory[0] = 0x00;
        var cpu = new Z80Cpu(bus, new InterruptLines());
        var observer = new TestObserver();
        var hookCalls = 0;
        cpu.Hooks.Add(new CpuHook(_ => hookCalls++));
        cpu.ExecutionObserver = observer;

        cpu.StepInstruction();

        Assert.Equal(1, hookCalls);
        Assert.Single(observer.Completed);
        Assert.Equal((ulong)1, observer.Completed[0].After.InstructionCount);
    }

    [Fact]
    public void CommonDebugSnapshotRestoresHaltedZ80()
    {
        var bus = new TestBus();
        bus.Memory[0] = 0x76;
        var cpu = new Z80Cpu(bus, new InterruptLines());

        cpu.StepInstruction();
        var snapshot = cpu.CaptureSnapshot();
        cpu.Reset();
        cpu.RestoreSnapshot(snapshot);

        Assert.True(cpu.Halted);
        Assert.Equal((ushort)1, cpu.Registers.PC);
        Assert.Equal(snapshot.CycleCount, cpu.CycleCount);
        Assert.Equal(snapshot.InstructionCount, cpu.InstructionCount);
    }

    [Fact]
    public void CommonDebugSnapshotRestoresZ80WaitStep()
    {
        var lines = new InterruptLines();
        lines.SetWait(true);
        var cpu = new Z80Cpu(new TestBus(), lines);

        cpu.StepInstruction();
        var snapshot = cpu.CaptureSnapshot();
        cpu.Reset();
        cpu.RestoreSnapshot(snapshot);

        Assert.Equal((ushort)0, cpu.Registers.PC);
        Assert.Equal((ulong)1, cpu.CycleCount);
        Assert.Equal((ulong)0, cpu.InstructionCount);
    }

    [Fact]
    public void CommonDebugSnapshotRestoresZ80ServicedInterrupt()
    {
        var lines = new InterruptLines();
        lines.SetInt(true);
        var cpu = new Z80Cpu(new TestBus(), lines);
        cpu.Registers.Iff1 = true;
        cpu.Registers.InterruptMode = 1;

        cpu.StepInstruction();
        var snapshot = cpu.CaptureSnapshot();
        cpu.Reset();
        cpu.RestoreSnapshot(snapshot);

        Assert.Equal((ushort)0x0038, cpu.Registers.PC);
        Assert.False(cpu.Registers.Iff1);
        Assert.Equal(snapshot.CycleCount, cpu.CycleCount);
    }

    [Fact]
    public void CommonDebugSnapshotRestoresZ80IndexedPrefixResult()
    {
        var bus = new TestBus { Memory = { [0] = 0xDD, [1] = 0x21, [2] = 0x34, [3] = 0x12 } };
        var cpu = new Z80Cpu(bus, new InterruptLines());

        cpu.StepInstruction();
        var snapshot = cpu.CaptureSnapshot();
        cpu.Reset();
        cpu.RestoreSnapshot(snapshot);

        Assert.Equal((ushort)0x1234, cpu.Registers.IX);
        Assert.Equal((ushort)4, cpu.Registers.PC);
        Assert.Equal(snapshot.CycleCount, cpu.CycleCount);
    }

    [Fact]
    public void CommonDebugSnapshotRestoresZ80EiDelay()
    {
        var bus = new TestBus { Memory = { [0] = 0xFB } };
        var cpu = new Z80Cpu(bus, new InterruptLines());

        cpu.StepInstruction();
        var snapshot = cpu.CaptureSnapshot();
        cpu.Reset();
        cpu.RestoreSnapshot(snapshot);

        Assert.True(cpu.Registers.Iff1);
        Assert.True(cpu.Registers.Iff2);
        Assert.Equal(1, cpu.Registers.InterruptDelay);
        Assert.Equal((ushort)1, cpu.Registers.PC);
    }

    [Fact]
    public void CommonExecutionObserverReportsZ80LifecycleOrder()
    {
        var bus = new TestBus { Memory = { [0x38] = 0x76, [0x66] = 0x00 } };
        var lines = new InterruptLines();
        var cpu = new Z80Cpu(bus, lines);
        var observer = new TestObserver();
        cpu.ExecutionObserver = observer;

        lines.SetWait(true);
        lines.SetNmi(true);
        cpu.StepInstruction();

        lines.SetWait(false);
        cpu.StepInstruction();

        lines.SetNmi(false);
        cpu.Registers.Iff1 = true;
        cpu.Registers.InterruptMode = 1;
        lines.SetInt(true);
        cpu.StepInstruction();

        lines.SetInt(false);
        cpu.StepInstruction();
        cpu.StepInstruction();
        cpu.Registers.Halted = false;
        cpu.StepInstruction();

        Assert.Equal(6, observer.Completed.Count);
        Assert.True(observer.Completed[0].Result.Waiting);
        Assert.True(observer.Completed[1].Result.InterruptServiced);
        Assert.True(observer.Completed[2].Result.InterruptServiced);
        Assert.True(observer.Completed[3].Result.InstructionCompleted);
        Assert.True(observer.Completed[3].After.State.Halted);
        Assert.False(observer.Completed[4].Result.InstructionCompleted);
        Assert.Equal((ushort)0x39, observer.Completed[4].After.State.Registers["PC"]);
        Assert.True(observer.Completed[5].Result.InstructionCompleted);
        Assert.Equal((ushort)0x3A, cpu.Registers.PC);
    }

    [Fact]
    public void Z80ResetUsesCommonCoreStateAndClockReset()
    {
        var bus = new TestBus { Memory = { [0] = 0x76 } };
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.A = 0xA5;
        cpu.StepInstruction();

        cpu.Reset();

        Assert.Equal((byte)0, cpu.Registers.A);
        Assert.Equal((ushort)0, cpu.Registers.PC);
        Assert.False(cpu.Halted);
        Assert.Equal((ulong)0, cpu.CycleCount);
        Assert.Equal((ulong)0, cpu.InstructionCount);
    }

    [Fact]
    public void CommonStepUpdatesZ80ClockAndInstructionCountExactlyOnce()
    {
        var bus = new TestBus { Memory = { [0] = 0x00, [0x38] = 0x76 } };
        var lines = new InterruptLines();
        var cpu = new Z80Cpu(bus, lines);

        cpu.StepInstruction();
        Assert.Equal((ulong)4, cpu.CycleCount);
        Assert.Equal((ulong)1, cpu.InstructionCount);

        lines.SetWait(true);
        cpu.StepInstruction();
        Assert.Equal((ulong)5, cpu.CycleCount);
        Assert.Equal((ulong)1, cpu.InstructionCount);

        lines.SetWait(false);
        lines.SetInt(true);
        cpu.Registers.Iff1 = true;
        cpu.Registers.InterruptMode = 1;
        cpu.StepInstruction();
        Assert.Equal((ulong)18, cpu.CycleCount);
        Assert.Equal((ulong)1, cpu.InstructionCount);

        lines.SetInt(false);
        cpu.StepInstruction();
        Assert.Equal((ulong)22, cpu.CycleCount);
        Assert.Equal((ulong)2, cpu.InstructionCount);

        cpu.StepInstruction();
        Assert.Equal((ulong)26, cpu.CycleCount);
        Assert.Equal((ulong)2, cpu.InstructionCount);
    }

    [Fact]
    public void Z80OpcodeRegistrationUsesCommonMetadataForRepresentativePages()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());

        AssertMetadata(cpu.Definition(0x00, 0x00), "NOP", 1, "Implied", 4);
        AssertMetadata(cpu.Definition(0x00, 0x76), "HALT", 1, "Implied", 4);
        AssertMetadata(cpu.Definition(0xCB, 0x00), "RLC B", 2, "Register", 8);
        AssertMetadata(cpu.Definition(0xED, 0x47), "LD I,A", 2, "Implied", 9);
        AssertMetadata(cpu.Definition(0xDD, 0x21), "LD IX,nn", 4, "Immediate16", 14);
        AssertMetadata(cpu.Definition(0xFD, 0x21), "LD IY,nn", 4, "Immediate16", 14);
        AssertMetadata(cpu.Definition(0x00, 0xDB), "IN A,(n)", 2, "ImmediatePort", 11);
    }

    [Fact]
    public void Z80OpcodeRegistrationHasCompleteIndexedPagesAndUniqueKeys()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());
        var definitions = cpu.Definitions.ToArray();

        Assert.Equal(definitions.Length, definitions.Select(definition => definition.Key).Distinct().Count());
        Assert.Equal(256, definitions.Count(definition => definition.Key.Page == 0xCB));
        Assert.Equal(256, definitions.Count(definition => definition.Key.Page == 0xDD));
        Assert.Equal(256, definitions.Count(definition => definition.Key.Page == 0xFD));
        Assert.Contains(definitions, definition => definition.Key.Page == 0xED);
    }

    [Fact]
    public void Z80OpcodeTableSupportsDerivedReplaceAndAddWithoutMutatingBase()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());
        var baseNop = cpu.Definition(0x00, 0x00);
        var derived = cpu.Derive(table =>
        {
            table.Replace(Definition(0x00, 0x00, "CUSTOM NOP"));
            table.Add(Definition(0xED, 0x00, "CUSTOM EXT"));
        });

        Assert.True(derived.IsSealed);
        Assert.Same(baseNop, cpu.Definition(0x00, 0x00));
        Assert.Equal("CUSTOM NOP", derived.Get(OpcodeKey.Base(0x00)).Mnemonic);
        Assert.Equal("CUSTOM EXT", derived.Get(new OpcodeKey(0xED, 0x00)).Mnemonic);
        Assert.Throws<InvalidOperationException>(() => derived.Replace(Definition(0x00, 0x01, "LATE")));
    }

    private static OpcodeDefinition<Z80State> Definition(byte page, byte opcode, string mnemonic)
        => new(
            new OpcodeKey(page, opcode),
            mnemonic,
            1,
            4,
            "Implied",
            (_, _) => CpuStepResult.Completed(4));

    private static void AssertMetadata(
        OpcodeDefinition<Z80State> definition,
        string mnemonic,
        byte length,
        string addressingMode,
        byte baseCycles)
    {
        Assert.Equal(mnemonic, definition.Mnemonic);
        Assert.Equal(length, definition.Length);
        Assert.Equal(addressingMode, definition.AddressingMode);
        Assert.Equal(baseCycles, definition.BaseCycles);
    }

    private sealed class TestObserver : ICpuExecutionObserver
    {
        public List<CpuStepTrace> Completed { get; } = [];

        public List<BusAccess> Accesses { get; } = [];

        public List<Exception> Failures { get; } = [];

        public bool Break { get; init; }

        public ushort? WatchAddress { get; init; }

        public bool ShouldBreak(CpuDebugSnapshot snapshot) => Break;

        public bool ShouldBreakOnMemoryAccess(BusAccess access)
        {
            Accesses.Add(access);
            return WatchAddress == access.Address;
        }

        public void OnStepCompleted(CpuStepTrace trace) => Completed.Add(trace);

        public void OnStepFailed(CpuDebugSnapshot snapshot, Exception exception) => Failures.Add(exception);
    }

    private sealed class ThrowingZ80Cpu(IBus bus, PetEmulator.CpuZ80.Interrupts.IInterruptLines interruptLines) : Z80Cpu(bus, interruptLines)
    {
        protected override void ConfigureOpcodes(OpcodeTable<Z80State> table)
        {
            base.ConfigureOpcodes(table);
            RegisterOpcode(0x00, ThrowFromOpcode);
        }

        private static int ThrowFromOpcode() => throw new InvalidOperationException("test opcode failure");
    }

    private sealed class MetadataProbeZ80Cpu(IBus bus, PetEmulator.CpuZ80.Interrupts.IInterruptLines interruptLines) : Z80Cpu(bus, interruptLines)
    {
        public IReadOnlyCollection<OpcodeDefinition<Z80State>> Definitions => Opcodes.Entries;

        public OpcodeDefinition<Z80State> Definition(byte page, byte opcode)
            => Opcodes.Get(new OpcodeKey(page, opcode));

        public OpcodeTable<Z80State> Derive(Action<OpcodeTable<Z80State>> changes)
            => Opcodes.Derive(changes);
    }

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];

        public byte ReadMemory(ushort address) => Memory[address];

        public void WriteMemory(ushort address, byte value) => Memory[address] = value;

        public byte ReadPort(byte port) => 0xFF;

        public void WritePort(byte port, byte value) { }
    }
}
