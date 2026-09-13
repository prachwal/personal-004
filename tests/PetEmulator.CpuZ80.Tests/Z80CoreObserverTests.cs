using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;
using PetEmulator.CpuZ80.Memory;

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
    public void CoreWatchpointAndLegacyMemoryWatchObserveOneMemoryAccessEach()
    {
        var bus = new SystemBus(new RamMemory());
        bus.WriteMemory(0x0000, 0x3A); // LD A,(nn)
        bus.WriteMemory(0x0001, 0x00);
        bus.WriteMemory(0x0002, 0x40);
        bus.WriteMemory(0x4000, 0x5A);

        var legacyHits = 0;
        bus.Watch.Add(0x4000, new MemoryHook(
            (kind, _, _) => kind == MemoryAccessKind.Read,
            (_, _, _) => legacyHits++));

        var observer = new TestObserver { WatchAddress = 0x4000 };
        var cpu = new Z80Cpu(bus, new InterruptLines())
        {
            ExecutionObserver = observer,
        };

        cpu.StepInstruction();

        var trace = Assert.Single(observer.Completed);
        Assert.True(trace.Result.WatchpointHit);
        Assert.Equal(1, observer.Accesses.Count(access => access.Address == 0x4000));
        Assert.Equal(1, legacyHits);
        Assert.Equal((byte)0x5A, cpu.Registers.A);
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
    public void Z80ImplementsTheCommonProcessorContract()
    {
        var bus = new TestBus { Memory = { [0] = 0x00 } };
        var cpu = new Z80Cpu(bus, new InterruptLines());
        IProcessor processor = cpu;

        processor.StepInstruction();

        Assert.Equal((ulong)4, processor.CycleCount);
        Assert.Equal((ulong)1, processor.InstructionCount);

        processor.Reset();

        Assert.Equal((ulong)0, processor.CycleCount);
        Assert.Equal((ulong)0, processor.InstructionCount);
        Assert.False(processor.Halted);
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
    public void Z80OpcodeRegistrationProvidesMetadataForEveryRegisteredDefinition()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());

        Assert.NotEmpty(cpu.Definitions);
        Assert.All(cpu.Definitions, definition =>
        {
            Assert.NotEqual("Unknown", definition.AddressingMode);
            Assert.NotEmpty(definition.Mnemonic);
            Assert.True(definition.Length > 0);
            Assert.True(definition.BaseCycles > 0);
        });
    }

    [Fact]
    public void Z80CbMetadataUsesTheInstructionMatrix()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());

        AssertMetadata(cpu.Definition(0xCB, 0x00), "RLC B", 2, "Register", 8);
        AssertMetadata(cpu.Definition(0xCB, 0x46), "BIT 0,(HL)", 2, "Memory", 12);
        AssertMetadata(cpu.Definition(0xCB, 0x86), "RES 0,(HL)", 2, "Memory", 15);
        AssertMetadata(cpu.Definition(0xCB, 0xC7), "SET 0,A", 2, "Register", 8);
        AssertMetadata(cpu.Definition(0xCB, 0x3E), "SRL (HL)", 2, "Memory", 15);
    }

    [Fact]
    public void Z80EdMetadataUsesTheRegisteredInstructionFamilies()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());
        var ed = cpu.Definitions.Where(definition => definition.Key.Page == 0xED).ToArray();

        Assert.NotEmpty(ed);
        Assert.DoesNotContain(ed, definition => definition.AddressingMode == "Extended");
        AssertMetadata(cpu.Definition(0xED, 0x40), "IN B,(C)", 2, "RegisterPort", 12);
        AssertMetadata(cpu.Definition(0xED, 0x4B), "LD BC,(nn)", 4, "Absolute16", 20);
        AssertMetadata(cpu.Definition(0xED, 0x56), "IM 1", 2, "Implied", 8);
        AssertMetadata(cpu.Definition(0xED, 0x4D), "RETI", 2, "Implied", 14);
        AssertMetadata(cpu.Definition(0xED, 0xB0), "LDIR", 2, "BlockRepeat", 21);
    }

    [Fact]
    public void Z80IndexedMetadataUsesIxIyDisplacementAndPrefixFamilies()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());

        AssertMetadata(cpu.Definition(0xDD, 0x21), "LD IX,nn", 4, "Immediate16", 14);
        AssertMetadata(cpu.Definition(0xFD, 0x21), "LD IY,nn", 4, "Immediate16", 14);
        AssertMetadata(cpu.Definition(0xDD, 0x7E), "LD A,(IX+d)", 3, "IndexedMemory", 19);
        AssertMetadata(cpu.Definition(0xFD, 0x36), "LD (IY+d),n", 4, "IndexedImmediate8", 19);
        AssertMetadata(cpu.Definition(0xDD, 0x24), "INC IXH", 2, "IndexedRegister", 8);
        AssertMetadata(cpu.Definition(0xFD, 0xCB), "FD CB", 4, "IndexedBit", 20);
    }

    [Fact]
    public void Z80BaseMetadataUsesLoadAndAluMatrices()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());

        AssertMetadata(cpu.Definition(0x00, 0x41), "LD B,C", 1, "Register", 4);
        AssertMetadata(cpu.Definition(0x00, 0x46), "LD B,(HL)", 1, "Memory", 7);
        AssertMetadata(cpu.Definition(0x00, 0x36), "LD (HL),n", 2, "MemoryImmediate8", 10);
        AssertMetadata(cpu.Definition(0x00, 0x80), "ADD A,B", 1, "Register", 4);
        AssertMetadata(cpu.Definition(0x00, 0x86), "ADD A,(HL)", 1, "Memory", 7);
        AssertMetadata(cpu.Definition(0x00, 0xBE), "CP (HL)", 1, "Memory", 7);
    }

    [Fact]
    public void Z80BaseMetadataUsesControlStackAndAccumulatorFamilies()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());

        AssertMetadata(cpu.Definition(0x00, 0x01), "LD BC,nn", 3, "Immediate16", 10);
        AssertMetadata(cpu.Definition(0x00, 0x23), "INC HL", 1, "RegisterPair", 6);
        AssertMetadata(cpu.Definition(0x00, 0x34), "INC (HL)", 1, "Memory", 11);
        AssertMetadata(cpu.Definition(0x00, 0xC3), "JP nn", 3, "Absolute16", 10);
        AssertMetadata(cpu.Definition(0x00, 0xCD), "CALL nn", 3, "Absolute16", 17);
        AssertMetadata(cpu.Definition(0x00, 0xE5), "PUSH HL", 1, "Stack", 11);
        AssertMetadata(cpu.Definition(0x00, 0xC9), "RET", 1, "Implied", 10);
        AssertMetadata(cpu.Definition(0x00, 0x27), "DAA", 1, "Implied", 4);
    }

    [Fact]
    public void Z80RegisteredOpcodeMetadataHasNoGenericFallbacks()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());
        var definitions = cpu.Definitions
            .Where(definition => definition.Key.Page is 0x00 or 0xCB or 0xED or 0xDD or 0xFD)
            .ToArray();

        Assert.NotEmpty(definitions);
        Assert.DoesNotContain(definitions, definition => definition.Mnemonic.StartsWith("OP ", StringComparison.Ordinal));
        Assert.DoesNotContain(definitions, definition => definition.Mnemonic.Contains(" OP ", StringComparison.Ordinal));
        Assert.DoesNotContain(definitions.Where(definition => definition.Key.Page == 0xED),
            definition => definition.AddressingMode == "Extended");
    }

    [Fact]
    public void Z80RepresentativeMetadataCyclesMatchExecutionCycles()
    {
        AssertMetadataCycles([0x00], 0x00, 0x00);
        AssertMetadataCycles([0x41], 0x00, 0x41);
        AssertMetadataCycles([0x46], 0x00, 0x46);
        AssertMetadataCycles([0x80], 0x00, 0x80);
        AssertMetadataCycles([0x86], 0x00, 0x86);
        AssertMetadataCycles([0x01, 0x34, 0x12], 0x00, 0x01);
        AssertMetadataCycles([0xCB, 0x00], 0xCB, 0x00);
        AssertMetadataCycles([0xED, 0x47], 0xED, 0x47);
        AssertMetadataCycles([0xDD, 0x21, 0x34, 0x12], 0xDD, 0x21);
    }

    private static void AssertMetadataCycles(byte[] program, byte page, byte opcode)
    {
        var metadataCpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());
        var bus = new TestBus();
        Array.Copy(program, bus.Memory, program.Length);
        var cpu = new Z80Cpu(bus, new InterruptLines());

        Assert.Equal(metadataCpu.Definition(page, opcode).BaseCycles, cpu.Step());
    }

    [Fact]
    public void Z80DerivedVariantExecutesItsCustomOpcodeThroughTheCommonDecoder()
    {
        var bus = new TestBus { Memory = { [0] = 0xED, [1] = 0x00 } };
        var cpu = new VariantZ80Cpu(bus, new InterruptLines());

        cpu.StepInstruction();

        Assert.Equal((byte)0xA5, cpu.Registers.A);
        Assert.Equal((ushort)2, cpu.Registers.PC);
        Assert.Equal((ulong)1, cpu.InstructionCount);
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

    [Fact]
    public void Z80DerivedOpcodeTableLeavesBaseEntriesUnchanged()
    {
        var cpu = new MetadataProbeZ80Cpu(new TestBus(), new InterruptLines());
        var baseNop = cpu.Definition(0x00, 0x00);
        var baseCount = cpu.Definitions.Count;

        var derived = cpu.Derive(table => table.Replace(Definition(0x00, 0x00, "DERIVED NOP")));

        Assert.Same(baseNop, cpu.Definition(0x00, 0x00));
        Assert.Equal(baseCount, cpu.Definitions.Count);
        Assert.Equal("NOP", cpu.Definition(0x00, 0x00).Mnemonic);
        Assert.Equal("DERIVED NOP", derived.Get(OpcodeKey.Base(0x00)).Mnemonic);
    }

    [Fact]
    public void Z80CoreCapabilitiesExposeWaitInterruptAcknowledgeAndBusCycle()
    {
        var lines = new InterruptLines();
        lines.SetWait(true);
        lines.SetInt(true);
        lines.SetNmi(true);
        var observer = new TestCycleObserver();
        var capabilities = new Z80CoreCapabilities(new TestBus(), lines, observer);

        Assert.True(capabilities.WaitAsserted);
        Assert.True(capabilities.IntAsserted);
        Assert.True(capabilities.NmiAsserted);
        Assert.Equal((byte)0xFF, capabilities.AcknowledgeInterrupt());

        var cycle = new BusCycle(BusCycleKind.Refresh, 0x1234, 0x56, 2, 3, 4);
        capabilities.Observe(cycle);

        Assert.Equal(cycle, Assert.Single(observer.Cycles));
    }

    [Fact]
    public void Z80IoInstructionsPreserveFullSixteenBitPortAddresses()
    {
        var inputBus = new WidePortBus { Memory = { [0] = 0xDB, [1] = 0x5A } };
        inputBus.Ports[0xA55A] = 0x3C;
        var input = new Z80Cpu(inputBus, new InterruptLines());
        input.Registers.A = 0xA5;

        input.StepInstruction();

        Assert.Equal((ushort)0xA55A, inputBus.LastReadPort);
        Assert.Equal((byte)0x3C, input.Registers.A);

        var registerBus = new WidePortBus { Memory = { [0] = 0xED, [1] = 0x40 } };
        registerBus.Ports[0x1234] = 0x7E;
        var registerInput = new Z80Cpu(registerBus, new InterruptLines());
        registerInput.Registers.BC = 0x1234;

        registerInput.StepInstruction();

        Assert.Equal((ushort)0x1234, registerBus.LastReadPort);
        Assert.Equal((byte)0x7E, registerInput.Registers.B);

        var outputBus = new WidePortBus { Memory = { [0] = 0xD3, [1] = 0x5A } };
        var output = new Z80Cpu(outputBus, new InterruptLines());
        output.Registers.A = 0xA5;

        output.StepInstruction();

        Assert.Equal((ushort)0xA55A, outputBus.LastWritePort);
        Assert.Equal((byte)0xA5, outputBus.LastWriteValue);
    }

    [Fact]
    public void Z80CpuUsesFFAndIgnoresWritesForUnmappedIoByDefault()
    {
        var memory = new RamMemory();
        memory.Write(0x0000, 0xDB);
        memory.Write(0x0001, 0x5A);
        memory.Write(0x0002, 0xD3);
        memory.Write(0x0003, 0xA1);
        var bus = new SystemBus(memory);
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.A = 0xA5;

        cpu.StepInstruction();
        cpu.StepInstruction();

        Assert.Equal((byte)0xFF, cpu.Registers.A);
        Assert.Equal((byte)0xFF, bus.ReadPort(0xFFA1));
    }

    [Fact]
    public void Z80BusCycleTraceCoversMigratedOpcodeFamiliesAndInterrupts()
    {
        AssertTraceContains(ExecuteTrace(0x00), BusCycleKind.OpcodeFetch, BusCycleKind.Refresh);
        AssertTraceContains(ExecuteTrace(0xCB, 0x00), BusCycleKind.OpcodeFetch, BusCycleKind.Refresh);
        AssertTraceContains(ExecuteTrace(0xED, 0x47), BusCycleKind.OpcodeFetch, BusCycleKind.Refresh);
        AssertTraceContains(ExecuteTrace(0xDD, 0x21, 0x34, 0x12), BusCycleKind.OpcodeFetch, BusCycleKind.Refresh);
        AssertTraceContains(ExecuteTrace(0xFD, 0x21, 0x34, 0x12), BusCycleKind.OpcodeFetch, BusCycleKind.Refresh);
        AssertTraceContains(ExecuteTrace(0xDB, 0x5A), BusCycleKind.OpcodeFetch, BusCycleKind.IoRead);

        var interruptBus = new TestBus();
        var lines = new InterruptLines();
        lines.SetInt(true);
        var interruptTrace = new TestCycleObserver();
        var interruptCpu = new Z80Cpu(interruptBus, lines, interruptTrace);
        interruptCpu.Registers.Iff1 = true;
        interruptCpu.Registers.InterruptMode = 1;
        interruptCpu.StepInstruction();

        AssertTraceContains(interruptTrace.Cycles,
            BusCycleKind.InterruptAcknowledge,
            BusCycleKind.Refresh,
            BusCycleKind.MemoryWrite);
    }

    private static IReadOnlyList<BusCycle> ExecuteTrace(params byte[] program)
    {
        var bus = new TestBus();
        Array.Copy(program, bus.Memory, program.Length);
        var trace = new TestCycleObserver();
        new Z80Cpu(bus, new InterruptLines(), trace).StepInstruction();
        return trace.Cycles;
    }

    private static void AssertTraceContains(IReadOnlyCollection<BusCycle> trace, params BusCycleKind[] kinds)
    {
        foreach (var kind in kinds)
            Assert.Contains(trace, cycle => cycle.Kind == kind);
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

    private sealed class TestCycleObserver : IBusCycleObserver
    {
        public List<BusCycle> Cycles { get; } = [];

        public void Observe(BusCycle cycle) => Cycles.Add(cycle);
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

    private sealed class VariantZ80Cpu(IBus bus, PetEmulator.CpuZ80.Interrupts.IInterruptLines interruptLines) : Z80Cpu(bus, interruptLines)
    {
        protected override void ConfigureOpcodes(OpcodeTable<Z80State> table)
        {
            base.ConfigureOpcodes(table);
            RegisterOpcode(0xED, 0x00, ExecuteVariantOpcode);
        }

        private int ExecuteVariantOpcode()
        {
            Registers.A = 0xA5;
            return 8;
        }
    }

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];

        public byte ReadMemory(ushort address) => Memory[address];

        public void WriteMemory(ushort address, byte value) => Memory[address] = value;

        public byte ReadPort(byte port) => 0xFF;

        public void WritePort(byte port, byte value) { }
    }

    private sealed class WidePortBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public Dictionary<ushort, byte> Ports { get; } = [];
        public ushort LastReadPort { get; private set; }
        public ushort LastWritePort { get; private set; }
        public byte LastWriteValue { get; private set; }

        public byte ReadMemory(ushort address) => Memory[address];
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => ReadPort((ushort)port);
        public byte ReadPort(ushort port)
        {
            LastReadPort = port;
            return Ports.TryGetValue(port, out var value) ? value : (byte)0xFF;
        }
        public void WritePort(byte port, byte value) => WritePort((ushort)port, value);
        public void WritePort(ushort port, byte value)
        {
            LastWritePort = port;
            LastWriteValue = value;
        }
    }
}
