using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Cpu;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class Z80TimingTests
{
    [Theory]
    [InlineData(0x00, 4)]
    [InlineData(0x3E, 7)]
    [InlineData(0x32, 13)]
    [InlineData(0xC3, 10)]
    [InlineData(0xCD, 17)]
    [InlineData(0xC9, 10)]
    [InlineData(0xCB, 8)]
    [InlineData(0xED, 15)]
    [InlineData(0xDD, 14)]
    public void RepresentativeInstructionsReturnDocumentedTStates(byte opcode, int expectedTStates)
    {
        var bus = new TestBus();
        bus.Memory[0] = opcode;
        bus.Memory[1] = opcode switch
        {
            0xCB => (byte)0x00,
            0xED => (byte)0x4A,
            0xDD => (byte)0x21,
            _ => (byte)0x00
        };
        var cpu = new Z80Cpu(bus, new InterruptLines());
        cpu.Registers.SP = 0x4000;

        Assert.Equal(expectedTStates, cpu.Step());
    }

    [Fact]
    public void ConditionalRelativeJumpUsesTakenAndUntakenTiming()
    {
        var takenBus = new TestBus { Memory = { [0] = 0x20, [1] = 0x02 } };
        var taken = new Z80Cpu(takenBus, new InterruptLines());
        Assert.Equal(12, taken.Step());

        var skippedBus = new TestBus { Memory = { [0] = 0x20, [1] = 0x02 } };
        var skipped = new Z80Cpu(skippedBus, new InterruptLines());
        skipped.Registers.F = Z80Flags.Zero;
        Assert.Equal(7, skipped.Step());
    }

    [Theory]
    [InlineData(0x20, 0x00, 12, 0x0004)]
    [InlineData(0x20, 0x40, 7, 0x0002)]
    [InlineData(0x28, 0x40, 12, 0x0004)]
    [InlineData(0x28, 0x00, 7, 0x0002)]
    [InlineData(0x30, 0x00, 12, 0x0004)]
    [InlineData(0x30, 0x01, 7, 0x0002)]
    [InlineData(0x38, 0x01, 12, 0x0004)]
    [InlineData(0x38, 0x00, 7, 0x0002)]
    public void TraceOrdersAllConditionalRelativeJumpPaths(byte opcode, byte flags, int expectedTStates, ushort expectedPc)
    {
        var (cpu, trace) = CreateTracedCpu(opcode, 0x02);
        cpu.Registers.F = flags;

        Assert.Equal(expectedTStates, cpu.Step());
        Assert.Equal(expectedPc, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x02));
    }

    [Fact]
    public void OpcodeFetchesAreVisibleSeparatelyFromDataReads()
    {
        var bus = new TestBus { Memory = { [0] = 0x3E, [1] = 0x42 } };
        var cpu = new Z80Cpu(bus, new InterruptLines());

        Assert.Equal(7, cpu.Step());
        Assert.Equal(1, bus.OpcodeFetches);
        Assert.Equal(1, bus.MemoryReads);
    }

    [Fact]
    public void InterruptAcknowledgeIsVisibleAndUsesDocumentedTiming()
    {
        var bus = new TestBus { InterruptOpcode = 0xFF, Memory = { [0] = 0xED, [1] = 0x56, [2] = 0xFB, [3] = 0x00 } };
        var lines = new TestInterruptLines { IntAsserted = true };
        var cpu = new Z80Cpu(bus, lines);
        cpu.Step();
        cpu.Step();
        cpu.Step();

        Assert.Equal(13, cpu.Step());
        Assert.Equal(1, bus.AcknowledgeCount);
    }

    [Fact]
    public void InterruptAcknowledgeTraceUsesTheSixTStateM1Cycle()
    {
        var bus = new TestBus { InterruptOpcode = 0xFF, Memory = { [0] = 0xED, [1] = 0x56, [2] = 0xFB } };
        var trace = new TraceObserver();
        var lines = new TestInterruptLines();
        var cpu = new Z80Cpu(bus, lines, trace);
        cpu.Step();
        cpu.Step();
        cpu.Step();

        trace.Cycles.Clear();
        lines.IntAsserted = true;
        cpu.Step();

        Assert.Equal((1, 1, 6), (trace.Cycles[0].MachineCycle, trace.Cycles[0].TState, trace.Cycles[0].TStates));
        Assert.Equal((1, 6, 6), (trace.Cycles[1].MachineCycle, trace.Cycles[1].TState, trace.Cycles[1].TStates));
        Assert.Equal((2, 1, 3), (trace.Cycles[2].MachineCycle, trace.Cycles[2].TState, trace.Cycles[2].TStates));
        Assert.Equal((3, 1, 3), (trace.Cycles[3].MachineCycle, trace.Cycles[3].TState, trace.Cycles[3].TStates));
    }

    [Fact]
    public void TraceReportsStandaloneHaltAndNmiRefreshCycles()
    {
        var haltBus = new TestBus { Memory = { [0] = 0x76 } };
        var haltTrace = new TraceObserver();
        var halt = new Z80Cpu(haltBus, new TestInterruptLines(), haltTrace);
        halt.Step();
        halt.Step();
        Assert.Equal((1, 1, 4), (haltTrace.Cycles[0].MachineCycle, haltTrace.Cycles[0].TState, haltTrace.Cycles[0].TStates));
        Assert.Equal((1, 4, 4), (haltTrace.Cycles[1].MachineCycle, haltTrace.Cycles[1].TState, haltTrace.Cycles[1].TStates));
        Assert.Equal((1, 1, 4), (haltTrace.Cycles[2].MachineCycle, haltTrace.Cycles[2].TState, haltTrace.Cycles[2].TStates));

        var nmiTrace = new TraceObserver();
        var nmi = new Z80Cpu(new TestBus(), new TestInterruptLines { NmiAsserted = true }, nmiTrace);
        nmi.Step();
        Assert.Equal((1, 1, 5), (nmiTrace.Cycles[0].MachineCycle, nmiTrace.Cycles[0].TState, nmiTrace.Cycles[0].TStates));
    }

    [Fact]
    public void TraceOrdersFetchAndRefreshForNop()
    {
        var (cpu, trace) = CreateTracedCpu(0x00);

        cpu.Step();

        AssertCycles(trace, (BusCycleKind.OpcodeFetch, 0, 0x00), (BusCycleKind.Refresh, 1, 1));
    }

    [Fact]
    public void TraceIncludesMachineCycleAndTStateForAbsoluteLoad()
    {
        var (cpu, trace) = CreateTracedCpu(0x3A, 0x00, 0x40);

        cpu.Step();

        Assert.Equal((1, 1, 4), (trace.Cycles[0].MachineCycle, trace.Cycles[0].TState, trace.Cycles[0].TStates));
        Assert.Equal((1, 4, 4), (trace.Cycles[1].MachineCycle, trace.Cycles[1].TState, trace.Cycles[1].TStates));
        Assert.Equal((2, 1, 3), (trace.Cycles[2].MachineCycle, trace.Cycles[2].TState, trace.Cycles[2].TStates));
        Assert.Equal((3, 1, 3), (trace.Cycles[3].MachineCycle, trace.Cycles[3].TState, trace.Cycles[3].TStates));
        Assert.Equal((4, 1, 3), (trace.Cycles[4].MachineCycle, trace.Cycles[4].TState, trace.Cycles[4].TStates));
    }

    [Fact]
    public void RefreshUsesIRAddress()
    {
        var (cpu, trace) = CreateTracedCpu(0x00);
        cpu.Registers.I = 0xA5;

        cpu.Step();

        Assert.Equal((BusCycleKind.Refresh, (ushort)0xA501, (byte)1),
            (trace.Cycles[1].Kind, trace.Cycles[1].Address, trace.Cycles[1].Value));
    }

    [Fact]
    public void TraceOrdersFetchAndImmediateReadForLdAImmediate()
    {
        var (cpu, trace) = CreateTracedCpu(0x3E, 0x42);

        cpu.Step();

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x3E),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x42));
    }

    [Theory]
    [InlineData(0x41, 4)]
    [InlineData(0x4A, 4)]
    [InlineData(0x53, 4)]
    [InlineData(0x5C, 4)]
    [InlineData(0x65, 4)]
    [InlineData(0x6F, 4)]
    [InlineData(0x70, 7)]
    [InlineData(0x7E, 7)]
    public void TraceOrdersRegisterAndMemoryLdVariants(byte opcode, int expectedTStates)
    {
        var (cpu, trace) = CreateTracedCpu(opcode);
        cpu.Registers.B = 0xA5;
        cpu.Registers.HL = 0x4000;

        Assert.Equal(expectedTStates, cpu.Step());
        Assert.Equal((ushort)0x0001, cpu.Registers.PC);
        if (opcode == 0x70)
        {
            AssertCycles(trace,
                (BusCycleKind.OpcodeFetch, 0, opcode),
                (BusCycleKind.Refresh, 1, 1),
                (BusCycleKind.MemoryWrite, 0x4000, 0xA5));
            return;
        }

        if (opcode == 0x7E)
        {
            AssertCycles(trace,
                (BusCycleKind.OpcodeFetch, 0, opcode),
                (BusCycleKind.Refresh, 1, 1),
                (BusCycleKind.MemoryRead, 0x4000, 0x00));
            return;
        }

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1));
    }

    [Theory]
    [InlineData(0x80, 4)]
    [InlineData(0x81, 4)]
    [InlineData(0x82, 4)]
    [InlineData(0x83, 4)]
    [InlineData(0x84, 4)]
    [InlineData(0x85, 4)]
    [InlineData(0x86, 7)]
    [InlineData(0x87, 4)]
    public void TraceOrdersAllRegisterAluSourceVariants(byte opcode, int expectedTStates)
    {
        var (cpu, trace) = CreateTracedCpu(opcode);
        cpu.Registers.A = 0x01;
        cpu.Registers.B = cpu.Registers.C = cpu.Registers.D = cpu.Registers.E = 0x02;
        cpu.Registers.H = cpu.Registers.L = 0x02;
        cpu.Registers.HL = 0x4000;

        Assert.Equal(expectedTStates, cpu.Step());
        Assert.Equal((ushort)0x0001, cpu.Registers.PC);
        if (opcode == 0x86)
        {
            AssertCycles(trace,
                (BusCycleKind.OpcodeFetch, 0, opcode),
                (BusCycleKind.Refresh, 1, 1),
                (BusCycleKind.MemoryRead, 0x4000, 0x00));
            return;
        }

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1));
    }

    [Fact]
    public void TraceOrdersEdPrefixFetchAndRefresh()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x47);

        cpu.Step();

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x47),
            (BusCycleKind.Refresh, 2, 2));
    }

    [Theory]
    [InlineData(0x40)]
    [InlineData(0x48)]
    [InlineData(0x50)]
    [InlineData(0x58)]
    [InlineData(0x60)]
    [InlineData(0x68)]
    [InlineData(0x70)]
    [InlineData(0x78)]
    public void TraceOrdersAllEdInputRegisterVariants(byte opcode)
    {
        var (cpu, trace) = CreateTracedCpu(0xED, opcode);
        cpu.Registers.BC = 0xA012;

        Assert.Equal(12, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, opcode),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.IoRead, 0xA012, 0xFF));
    }

    [Theory]
    [InlineData(0x41)]
    [InlineData(0x49)]
    [InlineData(0x51)]
    [InlineData(0x59)]
    [InlineData(0x61)]
    [InlineData(0x69)]
    [InlineData(0x71)]
    [InlineData(0x79)]
    public void TraceOrdersAllEdOutputRegisterVariants(byte opcode)
    {
        var (cpu, trace) = CreateTracedCpu(0xED, opcode);
        cpu.Registers.BC = 0xA012;
        cpu.Registers.A = 0x5A;
        cpu.Registers.B = cpu.Registers.C = cpu.Registers.D = cpu.Registers.E = 0x5A;
        cpu.Registers.H = cpu.Registers.L = 0x5A;
        var port = cpu.Registers.BC;
        var output = opcode == 0x71 ? (byte)0x00 : (byte)0x5A;

        Assert.Equal(12, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, opcode),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.IoWrite, port, output));
    }

    [Fact]
    public void TraceOrdersCbPrefixFetchAndRefresh()
    {
        var (cpu, trace) = CreateTracedCpu(0xCB, 0x00);

        cpu.Step();

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xCB),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x00),
            (BusCycleKind.Refresh, 2, 2));
    }

    [Fact]
    public void TraceOrdersFdPrefixRefreshOnIrAddress()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0x00);
        cpu.Registers.I = 0xA5;

        cpu.Step();

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 0xA501, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x00),
            (BusCycleKind.Refresh, 0xA502, 2));
    }

    [Fact]
    public void TraceOrdersChainedIndexPrefixes()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0xFD, 0x00);

        Assert.Equal(12, cpu.Step());

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xFD),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.OpcodeFetch, 2, 0x00),
            (BusCycleKind.Refresh, 3, 3));
    }

    [Fact]
    public void TraceOrdersUndefinedEdOpcodeAsTwoFetches()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x00);

        Assert.Equal(8, cpu.Step());

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x00),
            (BusCycleKind.Refresh, 2, 2));
    }

    [Theory]
    [InlineData(0xDD)]
    [InlineData(0xFD)]
    public void TraceOrdersIndexedCbDisplacementAndOpcodeReads(byte prefix)
    {
        var (cpu, trace) = CreateTracedCpu(prefix, 0xCB, 0x01, 0x46);
        if (prefix == 0xDD)
            cpu.Registers.IX = 0x4000;
        else
            cpu.Registers.IY = 0x4000;

        cpu.Step();

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, prefix),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xCB),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 3, 0x46),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Theory]
    [InlineData(0xDD)]
    [InlineData(0xFD)]
    public void TraceOrdersIndexedCbMemoryReadBeforeWrite(byte prefix)
    {
        var (cpu, trace) = CreateTracedCpu(0x3E, 0x80, 0x32, 0x01, 0x40, prefix, 0xCB, 0x01, 0x00);
        if (prefix == 0xDD)
            cpu.Registers.IX = 0x4000;
        else
            cpu.Registers.IY = 0x4000;
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();

        cpu.Step();

        Assert.Equal((BusCycleKind.MemoryRead, (ushort)0x4001, (byte)0x80),
            (trace.Cycles[^2].Kind, trace.Cycles[^2].Address, trace.Cycles[^2].Value));
        Assert.Equal((BusCycleKind.MemoryWrite, (ushort)0x4001, (byte)0x01),
            (trace.Cycles[^1].Kind, trace.Cycles[^1].Address, trace.Cycles[^1].Value));
    }

    [Fact]
    public void TraceOrdersAddressReadsBeforeMemoryWrite()
    {
        var (cpu, trace) = CreateTracedCpu(0x32, 0x00, 0x40);
        cpu.Registers.A = 0x5A;

        cpu.Step();

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x32),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x00),
            (BusCycleKind.MemoryRead, 2, 0x40),
            (BusCycleKind.MemoryWrite, 0x4000, 0x5A));
    }

    [Fact]
    public void TraceOrdersCallTargetReadsBeforeReturnAddressWrites()
    {
        var (cpu, trace) = CreateTracedCpu(0xCD, 0x06, 0x00);
        cpu.Registers.SP = 0x4000;

        Assert.Equal(17, cpu.Step());

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xCD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x06),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x03),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x00));
    }

    [Fact]
    public void TraceOrdersReturnStackReadsAfterOpcodeFetch()
    {
        var (cpu, trace) = CreateTracedCpu(0x21, 0x34, 0x12, 0xE5, 0xC9);
        cpu.Registers.SP = 0x4000;
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x1234, cpu.Registers.PC);

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 4, 0xC9),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.MemoryRead, 0x3FFE, 0x34),
            (BusCycleKind.MemoryRead, 0x3FFF, 0x12));
    }

    [Theory]
    [InlineData(0xC0, 0x00)]
    [InlineData(0xC8, 0x40)]
    [InlineData(0xD0, 0x00)]
    [InlineData(0xD8, 0x01)]
    [InlineData(0xE0, 0x00)]
    [InlineData(0xE8, 0x04)]
    [InlineData(0xF0, 0x00)]
    [InlineData(0xF8, 0x80)]
    public void TraceOrdersAllConditionalReturnVariants(byte opcode, byte flags)
    {
        var (cpu, trace) = CreateTracedCpu(opcode);
        cpu.Registers.F = flags;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(11, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.PC);
        Assert.Equal((ushort)0x4002, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Theory]
    [InlineData(0xC0, 0x40)]
    [InlineData(0xC8, 0x00)]
    [InlineData(0xD0, 0x01)]
    [InlineData(0xD8, 0x00)]
    [InlineData(0xE0, 0x04)]
    [InlineData(0xE8, 0x00)]
    [InlineData(0xF0, 0x80)]
    [InlineData(0xF8, 0x00)]
    public void TraceOrdersAllConditionalReturnUntakenPaths(byte opcode, byte flags)
    {
        var (cpu, trace) = CreateTracedCpu(opcode);
        cpu.Registers.F = flags;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(5, cpu.Step());
        Assert.Equal((ushort)0x0001, cpu.Registers.PC);
        Assert.Equal((ushort)0x4000, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1));
    }

    [Theory]
    [InlineData(0xC5, true)]
    [InlineData(0xD5, true)]
    [InlineData(0xE5, true)]
    [InlineData(0xF5, true)]
    [InlineData(0xC1, false)]
    [InlineData(0xD1, false)]
    [InlineData(0xE1, false)]
    [InlineData(0xF1, false)]
    public void TraceOrdersAllStackPairVariants(byte opcode, bool push)
    {
        var (cpu, trace) = CreateTracedCpu(opcode);
        cpu.Registers.BC = cpu.Registers.DE = cpu.Registers.HL = cpu.Registers.AF = 0x1234;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(push ? 11 : 10, cpu.Step());
        Assert.Equal(push ? (ushort)0x3FFE : (ushort)0x4002, cpu.Registers.SP);
        if (push)
        {
            AssertCycles(trace,
                (BusCycleKind.OpcodeFetch, 0, opcode),
                (BusCycleKind.Refresh, 1, 1),
                (BusCycleKind.MemoryWrite, 0x3FFE, 0x34),
                (BusCycleKind.MemoryWrite, 0x3FFF, 0x12));
        }
        else
        {
            AssertCycles(trace,
                (BusCycleKind.OpcodeFetch, 0, opcode),
                (BusCycleKind.Refresh, 1, 1),
                (BusCycleKind.MemoryRead, 0x4000, 0x00),
                (BusCycleKind.MemoryRead, 0x4001, 0x00));
        }
    }

    [Fact]
    public void TraceOmitsStackReadsForUntakenConditionalReturn()
    {
        var (untaken, untakenTrace) = CreateTracedCpu(0xC0);
        untaken.Registers.F = Z80Flags.Zero;

        Assert.Equal(5, untaken.Step());
        AssertCycles(untakenTrace,
            (BusCycleKind.OpcodeFetch, 0, 0xC0),
            (BusCycleKind.Refresh, 1, 1));

        var (taken, takenTrace) = CreateTracedCpu(0x21, 0x34, 0x12, 0xE5, 0xC0);
        taken.Registers.SP = 0x4000;
        taken.Step();
        taken.Step();
        takenTrace.Cycles.Clear();

        Assert.Equal(11, taken.Step());
        Assert.Equal((ushort)0x1234, taken.Registers.PC);
        AssertCycles(takenTrace,
            (BusCycleKind.OpcodeFetch, 4, 0xC0),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.MemoryRead, 0x3FFE, 0x34),
            (BusCycleKind.MemoryRead, 0x3FFF, 0x12));
    }

    [Fact]
    public void TraceOmitsStackWritesForUntakenConditionalCall()
    {
        var (untaken, untakenTrace) = CreateTracedCpu(0xC4, 0x06, 0x00);
        untaken.Registers.F = Z80Flags.Zero;

        Assert.Equal(10, untaken.Step());
        AssertCycles(untakenTrace,
            (BusCycleKind.OpcodeFetch, 0, 0xC4),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x06),
            (BusCycleKind.MemoryRead, 2, 0x00));

        var (taken, takenTrace) = CreateTracedCpu(0xC4, 0x06, 0x00);
        taken.Registers.SP = 0x4000;

        Assert.Equal(17, taken.Step());
        AssertCycles(takenTrace,
            (BusCycleKind.OpcodeFetch, 0, 0xC4),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x06),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x03),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x00));
    }

    [Fact]
    public void TraceReadsRelativeDisplacementForBothConditionalJumpPaths()
    {
        var (taken, takenTrace) = CreateTracedCpu(0x20, 0x02);
        Assert.Equal(12, taken.Step());
        Assert.Equal((ushort)0x0004, taken.Registers.PC);
        AssertCycles(takenTrace,
            (BusCycleKind.OpcodeFetch, 0, 0x20),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x02));

        var (untaken, untakenTrace) = CreateTracedCpu(0x20, 0x02);
        untaken.Registers.F = Z80Flags.Zero;
        Assert.Equal(7, untaken.Step());
        Assert.Equal((ushort)0x0002, untaken.Registers.PC);
        AssertCycles(untakenTrace,
            (BusCycleKind.OpcodeFetch, 0, 0x20),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x02));
    }

    [Fact]
    public void TraceReadsDjnzDisplacementForTakenAndUntakenPaths()
    {
        var (taken, takenTrace) = CreateTracedCpu(0x10, 0xFE);
        taken.Registers.B = 2;
        Assert.Equal(13, taken.Step());
        Assert.Equal((ushort)0x0000, taken.Registers.PC);
        AssertCycles(takenTrace,
            (BusCycleKind.OpcodeFetch, 0, 0x10),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0xFE));

        var (untaken, untakenTrace) = CreateTracedCpu(0x10, 0xFE);
        untaken.Registers.B = 1;
        Assert.Equal(8, untaken.Step());
        Assert.Equal((ushort)0x0002, untaken.Registers.PC);
        AssertCycles(untakenTrace,
            (BusCycleKind.OpcodeFetch, 0, 0x10),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0xFE));
    }

    [Fact]
    public void TraceOrdersLdirTransferAndRepeatFetch()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xB0);
        cpu.Registers.HL = 0x4000;
        cpu.Registers.DE = 0x5000;
        cpu.Registers.BC = 2;

        Assert.Equal(21, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xB0),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryWrite, 0x5000, 0x00));

        trace.Cycles.Clear();
        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.OpcodeFetch, 1, 0xB0),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.MemoryWrite, 0x5001, 0x00));
    }

    [Fact]
    public void TraceOrdersCpirCompareAndRepeatFetch()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xB1);
        cpu.Registers.A = 0x01;
        cpu.Registers.HL = 0x4000;
        cpu.Registers.BC = 2;

        Assert.Equal(21, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xB1),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4000, 0x00));

        trace.Cycles.Clear();
        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.OpcodeFetch, 1, 0xB1),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceStopsCpirOnMatchingByteWithoutRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0x3E, 0x01, 0x32, 0x00, 0x40, 0xED, 0xB1);
        cpu.Registers.HL = 0x4000;
        cpu.Registers.BC = 2;
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0007, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 5, 0xED),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.OpcodeFetch, 6, 0xB1),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.MemoryRead, 0x4000, 0x01));
    }

    [Fact]
    public void TraceOrdersLdiTransferWithoutRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xA0);
        cpu.Registers.HL = 0x4000;
        cpu.Registers.DE = 0x5000;
        cpu.Registers.BC = 1;

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((ushort)0x4001, cpu.Registers.HL);
        Assert.Equal((ushort)0x5001, cpu.Registers.DE);
        Assert.Equal((ushort)0x0000, cpu.Registers.BC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xA0),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryWrite, 0x5000, 0x00));
    }

    [Fact]
    public void TraceOrdersLddTransferWithDecrementedAddresses()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xA8);
        cpu.Registers.HL = 0x4001;
        cpu.Registers.DE = 0x5001;
        cpu.Registers.BC = 1;

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x4000, cpu.Registers.HL);
        Assert.Equal((ushort)0x5000, cpu.Registers.DE);
        Assert.Equal((ushort)0x0000, cpu.Registers.BC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xA8),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.MemoryWrite, 0x5001, 0x00));
    }

    [Fact]
    public void TraceOrdersLddrRepeatWithDecrementedAddresses()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xB8);
        cpu.Registers.HL = 0x4001;
        cpu.Registers.DE = 0x5001;
        cpu.Registers.BC = 2;

        Assert.Equal(21, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.PC);
        Assert.Equal((ushort)0x4000, cpu.Registers.HL);
        Assert.Equal((ushort)0x5000, cpu.Registers.DE);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xB8),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.MemoryWrite, 0x5001, 0x00));

        trace.Cycles.Clear();
        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((ushort)0x3FFF, cpu.Registers.HL);
        Assert.Equal((ushort)0x4FFF, cpu.Registers.DE);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.OpcodeFetch, 1, 0xB8),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryWrite, 0x5000, 0x00));
    }

    [Fact]
    public void TraceOrdersInirPortReadMemoryWriteAndRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xB2);
        cpu.Registers.BC = 0x0210;
        cpu.Registers.HL = 0x4000;

        Assert.Equal(21, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.PC);
        Assert.Equal((byte)1, cpu.Registers.B);
        Assert.Equal((ushort)0x4001, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xB2),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.IoRead, 0x0210, 0xFF),
            (BusCycleKind.MemoryWrite, 0x4000, 0xFF));

        trace.Cycles.Clear();
        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((byte)0, cpu.Registers.B);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.OpcodeFetch, 1, 0xB2),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.IoRead, 0x0110, 0xFF),
            (BusCycleKind.MemoryWrite, 0x4001, 0xFF));
    }

    [Fact]
    public void TraceOrdersOtirMemoryReadPortWriteAndRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xB3);
        cpu.Registers.BC = 0x0210;
        cpu.Registers.HL = 0x4000;

        Assert.Equal(21, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.PC);
        Assert.Equal((byte)1, cpu.Registers.B);
        Assert.Equal((ushort)0x4001, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xB3),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.IoWrite, 0x0210, 0x00));

        trace.Cycles.Clear();
        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((byte)0, cpu.Registers.B);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.OpcodeFetch, 1, 0xB3),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.IoWrite, 0x0110, 0x00));
    }

    [Fact]
    public void TraceOrdersIndrPortReadMemoryWriteAndDecrementRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xBA);
        cpu.Registers.BC = 0x0210;
        cpu.Registers.HL = 0x4001;

        Assert.Equal(21, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.PC);
        Assert.Equal((byte)1, cpu.Registers.B);
        Assert.Equal((ushort)0x4000, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xBA),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.IoRead, 0x0210, 0xFF),
            (BusCycleKind.MemoryWrite, 0x4001, 0xFF));

        trace.Cycles.Clear();
        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((byte)0, cpu.Registers.B);
        Assert.Equal((ushort)0x3FFF, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.OpcodeFetch, 1, 0xBA),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.IoRead, 0x0110, 0xFF),
            (BusCycleKind.MemoryWrite, 0x4000, 0xFF));
    }

    [Fact]
    public void TraceOrdersOtdrMemoryReadPortWriteAndDecrementRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xBB);
        cpu.Registers.BC = 0x0210;
        cpu.Registers.HL = 0x4001;

        Assert.Equal(21, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.PC);
        Assert.Equal((byte)1, cpu.Registers.B);
        Assert.Equal((ushort)0x4000, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xBB),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.IoWrite, 0x0210, 0x00));

        trace.Cycles.Clear();
        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((byte)0, cpu.Registers.B);
        Assert.Equal((ushort)0x3FFF, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.OpcodeFetch, 1, 0xBB),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.IoWrite, 0x0110, 0x00));
    }

    [Fact]
    public void TraceOrdersCpiCompareWithoutRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xA1);
        cpu.Registers.A = 0x01;
        cpu.Registers.HL = 0x4000;
        cpu.Registers.BC = 1;

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((ushort)0x4001, cpu.Registers.HL);
        Assert.Equal((ushort)0x0000, cpu.Registers.BC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xA1),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4000, 0x00));
    }

    [Fact]
    public void TraceOrdersCpdCompareWithDecrementedAddress()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xA9);
        cpu.Registers.A = 0x01;
        cpu.Registers.HL = 0x4001;
        cpu.Registers.BC = 1;

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((ushort)0x4000, cpu.Registers.HL);
        Assert.Equal((ushort)0x0000, cpu.Registers.BC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xA9),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersIniWithoutRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xA2);
        cpu.Registers.BC = 0x0210;
        cpu.Registers.HL = 0x4000;

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((byte)1, cpu.Registers.B);
        Assert.Equal((ushort)0x4001, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xA2),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.IoRead, 0x0210, 0xFF),
            (BusCycleKind.MemoryWrite, 0x4000, 0xFF));
    }

    [Fact]
    public void TraceOrdersOutiWithoutRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xA3);
        cpu.Registers.BC = 0x0210;
        cpu.Registers.HL = 0x4000;

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((byte)1, cpu.Registers.B);
        Assert.Equal((ushort)0x4001, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xA3),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.IoWrite, 0x0210, 0x00));
    }

    [Fact]
    public void TraceReadsAbsoluteTargetForTakenAndUntakenConditionalJump()
    {
        var (taken, takenTrace) = CreateTracedCpu(0xC2, 0x06, 0x00);
        Assert.Equal(10, taken.Step());
        Assert.Equal((ushort)0x0006, taken.Registers.PC);
        AssertCycles(takenTrace,
            (BusCycleKind.OpcodeFetch, 0, 0xC2),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x06),
            (BusCycleKind.MemoryRead, 2, 0x00));

        var (untaken, untakenTrace) = CreateTracedCpu(0xC2, 0x06, 0x00);
        untaken.Registers.F = Z80Flags.Zero;
        Assert.Equal(10, untaken.Step());
        Assert.Equal((ushort)0x0003, untaken.Registers.PC);
        AssertCycles(untakenTrace,
            (BusCycleKind.OpcodeFetch, 0, 0xC2),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x06),
            (BusCycleKind.MemoryRead, 2, 0x00));
    }

    [Theory]
    [InlineData(0xC2, 0x00)]
    [InlineData(0xCA, 0x40)]
    [InlineData(0xD2, 0x00)]
    [InlineData(0xDA, 0x01)]
    [InlineData(0xE2, 0x00)]
    [InlineData(0xEA, 0x04)]
    [InlineData(0xF2, 0x00)]
    [InlineData(0xFA, 0x80)]
    public void TraceOrdersAllConditionalAbsoluteJumpVariants(byte opcode, byte flags)
    {
        var (cpu, trace) = CreateTracedCpu(opcode, 0x06, 0x00);
        cpu.Registers.F = flags;

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x0006, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x06),
            (BusCycleKind.MemoryRead, 2, 0x00));
    }

    [Theory]
    [InlineData(0xC2, 0x40)]
    [InlineData(0xCA, 0x00)]
    [InlineData(0xD2, 0x01)]
    [InlineData(0xDA, 0x00)]
    [InlineData(0xE2, 0x04)]
    [InlineData(0xEA, 0x00)]
    [InlineData(0xF2, 0x80)]
    [InlineData(0xFA, 0x00)]
    public void TraceOrdersAllConditionalAbsoluteJumpUntakenPaths(byte opcode, byte flags)
    {
        var (cpu, trace) = CreateTracedCpu(opcode, 0x06, 0x00);
        cpu.Registers.F = flags;

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x0003, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x06),
            (BusCycleKind.MemoryRead, 2, 0x00));
    }

    [Theory]
    [InlineData(0xC7, 0x00)]
    [InlineData(0xCF, 0x08)]
    [InlineData(0xD7, 0x10)]
    [InlineData(0xDF, 0x18)]
    [InlineData(0xE7, 0x20)]
    [InlineData(0xEF, 0x28)]
    [InlineData(0xF7, 0x30)]
    [InlineData(0xFF, 0x38)]
    public void TraceOrdersAllRestartVectors(byte opcode, byte vector)
    {
        var (cpu, trace) = CreateTracedCpu(opcode);
        cpu.Registers.SP = 0x4000;

        Assert.Equal(11, cpu.Step());
        Assert.Equal((ushort)vector, cpu.Registers.PC);
        Assert.Equal((ushort)0x3FFE, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x01),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x00));
    }

    [Theory]
    [InlineData(0x04, 4)]
    [InlineData(0x0C, 4)]
    [InlineData(0x14, 4)]
    [InlineData(0x1C, 4)]
    [InlineData(0x24, 4)]
    [InlineData(0x2C, 4)]
    [InlineData(0x34, 11)]
    [InlineData(0x3C, 4)]
    public void TraceOrdersAllIncrementRegisterVariants(byte opcode, int expectedTStates)
    {
        var (cpu, trace) = CreateTracedCpu(opcode);
        cpu.Registers.HL = 0x4000;

        Assert.Equal(expectedTStates, cpu.Step());
        Assert.Equal((ushort)0x0001, cpu.Registers.PC);

        if (opcode == 0x34)
        {
            AssertCycles(trace,
                (BusCycleKind.OpcodeFetch, 0, opcode),
                (BusCycleKind.Refresh, 1, 1),
                (BusCycleKind.MemoryRead, 0x4000, 0x00),
                (BusCycleKind.MemoryWrite, 0x4000, 0x01));
            return;
        }

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1));
    }

    [Theory]
    [InlineData(0x05, 4)]
    [InlineData(0x0D, 4)]
    [InlineData(0x15, 4)]
    [InlineData(0x1D, 4)]
    [InlineData(0x25, 4)]
    [InlineData(0x2D, 4)]
    [InlineData(0x35, 11)]
    [InlineData(0x3D, 4)]
    public void TraceOrdersAllDecrementRegisterVariants(byte opcode, int expectedTStates)
    {
        var (cpu, trace) = CreateTracedCpu(opcode);
        cpu.Registers.HL = 0x4000;

        Assert.Equal(expectedTStates, cpu.Step());
        Assert.Equal((ushort)0x0001, cpu.Registers.PC);

        if (opcode == 0x35)
        {
            AssertCycles(trace,
                (BusCycleKind.OpcodeFetch, 0, opcode),
                (BusCycleKind.Refresh, 1, 1),
                (BusCycleKind.MemoryRead, 0x4000, 0x00),
                (BusCycleKind.MemoryWrite, 0x4000, 0xFF));
            return;
        }

        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1));
    }

    [Theory]
    [InlineData(0xC4, 0x00)]
    [InlineData(0xCC, 0x40)]
    [InlineData(0xD4, 0x00)]
    [InlineData(0xDC, 0x01)]
    [InlineData(0xE4, 0x00)]
    [InlineData(0xEC, 0x04)]
    [InlineData(0xF4, 0x00)]
    [InlineData(0xFC, 0x80)]
    public void TraceOrdersAllConditionalCallVariants(byte opcode, byte flags)
    {
        var (cpu, trace) = CreateTracedCpu(opcode, 0x06, 0x00);
        cpu.Registers.F = flags;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(17, cpu.Step());
        Assert.Equal((ushort)0x0006, cpu.Registers.PC);
        Assert.Equal((ushort)0x3FFE, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x06),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x03),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x00));
    }

    [Theory]
    [InlineData(0xC4, 0x40)]
    [InlineData(0xCC, 0x00)]
    [InlineData(0xD4, 0x01)]
    [InlineData(0xDC, 0x00)]
    [InlineData(0xE4, 0x04)]
    [InlineData(0xEC, 0x00)]
    [InlineData(0xF4, 0x80)]
    [InlineData(0xFC, 0x00)]
    public void TraceOrdersAllConditionalCallUntakenPaths(byte opcode, byte flags)
    {
        var (cpu, trace) = CreateTracedCpu(opcode, 0x06, 0x00);
        cpu.Registers.F = flags;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x0003, cpu.Registers.PC);
        Assert.Equal((ushort)0x4000, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, opcode),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x06),
            (BusCycleKind.MemoryRead, 2, 0x00));
    }

    [Fact]
    public void TraceOrdersOutdWithoutRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xAB);
        cpu.Registers.BC = 0x0210;
        cpu.Registers.HL = 0x4001;

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((byte)1, cpu.Registers.B);
        Assert.Equal((ushort)0x4000, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xAB),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.IoWrite, 0x0210, 0x00));
    }

    [Fact]
    public void TraceOrdersIndWithoutRepeat()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0xAA);
        cpu.Registers.BC = 0x0210;
        cpu.Registers.HL = 0x4001;

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        Assert.Equal((byte)1, cpu.Registers.B);
        Assert.Equal((ushort)0x4000, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xAA),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.IoRead, 0x0210, 0xFF),
            (BusCycleKind.MemoryWrite, 0x4001, 0xFF));
    }

    [Fact]
    public void TraceOrdersRstStackWritesAfterOpcodeFetch()
    {
        var (cpu, trace) = CreateTracedCpu(0xCF);
        cpu.Registers.SP = 0x4000;

        Assert.Equal(11, cpu.Step());
        Assert.Equal((ushort)0x0008, cpu.Registers.PC);
        Assert.Equal((ushort)0x3FFE, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xCF),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x01),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x00));
    }

    [Fact]
    public void TraceOrdersExSpHlReadsBeforeStackWrites()
    {
        var (cpu, trace) = CreateTracedCpu(0xE3);
        cpu.Registers.HL = 0x1234;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((ushort)0x0001, cpu.Registers.PC);
        Assert.Equal((ushort)0x0000, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xE3),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.MemoryWrite, 0x4000, 0x34),
            (BusCycleKind.MemoryWrite, 0x4001, 0x12));
    }

    [Fact]
    public void TraceOrdersPushBcLowByteBeforeHighByte()
    {
        var (cpu, trace) = CreateTracedCpu(0xC5);
        cpu.Registers.BC = 0x1234;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(11, cpu.Step());
        Assert.Equal((ushort)0x3FFE, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xC5),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x34),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x12));
    }

    [Fact]
    public void TraceOrdersPopBcLowByteBeforeHighByte()
    {
        var (cpu, trace) = CreateTracedCpu(
            0x01, 0x34, 0x12,
            0xC5,
            0x01, 0x00, 0x00,
            0xC1);
        cpu.Registers.SP = 0x4000;

        cpu.Step();
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x1234, cpu.Registers.BC);
        Assert.Equal((ushort)0x4000, cpu.Registers.SP);
        Assert.Equal((ushort)0x0008, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 7, 0xC1),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.MemoryRead, 0x3FFE, 0x34),
            (BusCycleKind.MemoryRead, 0x3FFF, 0x12));
    }

    [Fact]
    public void TraceOrdersPushIxPrefixedFetchAndStackWrites()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0xE5);
        cpu.Registers.IX = 0x1234;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(15, cpu.Step());
        Assert.Equal((ushort)0x3FFE, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xE5),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x34),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x12));
    }

    [Fact]
    public void TraceOrdersPushIyPrefixedFetchAndStackWrites()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0xE5);
        cpu.Registers.IY = 0x5678;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(15, cpu.Step());
        Assert.Equal((ushort)0x3FFE, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xE5),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x78),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x56));
    }

    [Fact]
    public void TraceOrdersPopIxPrefixedFetchAndStackReads()
    {
        var (cpu, trace) = CreateTracedCpu(
            0x21, 0x34, 0x12,
            0xE5,
            0x21, 0x00, 0x00,
            0xDD, 0xE1);
        cpu.Registers.SP = 0x4000;

        cpu.Step();
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();

        Assert.Equal(14, cpu.Step());
        Assert.Equal((ushort)0x1234, cpu.Registers.IX);
        Assert.Equal((ushort)0x4000, cpu.Registers.SP);
        Assert.Equal((ushort)0x0009, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 7, 0xDD),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.OpcodeFetch, 8, 0xE1),
            (BusCycleKind.Refresh, 5, 5),
            (BusCycleKind.MemoryRead, 0x3FFE, 0x34),
            (BusCycleKind.MemoryRead, 0x3FFF, 0x12));
    }

    [Fact]
    public void TraceOrdersPopIyPrefixedFetchAndStackReads()
    {
        var (cpu, trace) = CreateTracedCpu(
            0x21, 0x78, 0x56,
            0xE5,
            0x21, 0x00, 0x00,
            0xFD, 0xE1);
        cpu.Registers.SP = 0x4000;

        cpu.Step();
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();

        Assert.Equal(14, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.IY);
        Assert.Equal((ushort)0x4000, cpu.Registers.SP);
        Assert.Equal((ushort)0x0009, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 7, 0xFD),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.OpcodeFetch, 8, 0xE1),
            (BusCycleKind.Refresh, 5, 5),
            (BusCycleKind.MemoryRead, 0x3FFE, 0x78),
            (BusCycleKind.MemoryRead, 0x3FFF, 0x56));
    }

    [Fact]
    public void TraceOrdersLdSpIxPrefixedFetch()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0xF9);
        cpu.Registers.IX = 0x5678;

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xF9),
            (BusCycleKind.Refresh, 2, 2));
    }

    [Fact]
    public void TraceOrdersLdSpIyPrefixedFetch()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0xF9);
        cpu.Registers.IY = 0x9ABC;

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x9ABC, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xF9),
            (BusCycleKind.Refresh, 2, 2));
    }

    [Fact]
    public void TraceOrdersStoreAbsoluteIxLowByteBeforeHighByte()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x22, 0x00, 0x40);
        cpu.Registers.IX = 0x1234;

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x22),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryWrite, 0x4000, 0x34),
            (BusCycleKind.MemoryWrite, 0x4001, 0x12));
    }

    [Fact]
    public void TraceOrdersStoreAbsoluteIyLowByteBeforeHighByte()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0x22, 0x00, 0x40);
        cpu.Registers.IY = 0x5678;

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x22),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryWrite, 0x4000, 0x78),
            (BusCycleKind.MemoryWrite, 0x4001, 0x56));
    }

    [Fact]
    public void TraceOrdersLoadAbsoluteIntoIxAfterAddressReads()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x2A, 0x00, 0x40);

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.IX);
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x2A),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersLoadAbsoluteIntoIyAfterAddressReads()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0x2A, 0x00, 0x40);

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.IY);
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x2A),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersLoadImmediateIntoIx()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x21, 0x34, 0x12);

        Assert.Equal(14, cpu.Step());
        Assert.Equal((ushort)0x1234, cpu.Registers.IX);
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x21),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x34),
            (BusCycleKind.MemoryRead, 3, 0x12));
    }

    [Fact]
    public void TraceOrdersLoadImmediateIntoIy()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0x21, 0x78, 0x56);

        Assert.Equal(14, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.IY);
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x21),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x78),
            (BusCycleKind.MemoryRead, 3, 0x56));
    }

    [Fact]
    public void TraceOrdersLoadAbsoluteIntoHlAfterAddressReads()
    {
        var (cpu, trace) = CreateTracedCpu(0x2A, 0x00, 0x40);

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.HL);
        Assert.Equal((ushort)0x0003, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x2A),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x00),
            (BusCycleKind.MemoryRead, 2, 0x40),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersStoreAbsoluteHlLowByteBeforeHighByte()
    {
        var (cpu, trace) = CreateTracedCpu(0x22, 0x00, 0x40);
        cpu.Registers.HL = 0x1234;

        Assert.Equal(16, cpu.Step());
        Assert.Equal((ushort)0x0003, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x22),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x00),
            (BusCycleKind.MemoryRead, 2, 0x40),
            (BusCycleKind.MemoryWrite, 0x4000, 0x34),
            (BusCycleKind.MemoryWrite, 0x4001, 0x12));
    }

    [Fact]
    public void TraceOrdersLdSpHl()
    {
        var (cpu, trace) = CreateTracedCpu(0xF9);
        cpu.Registers.HL = 0x5678;

        Assert.Equal(6, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xF9),
            (BusCycleKind.Refresh, 1, 1));
    }

    [Fact]
    public void TraceOrdersExDeHlRegisterSwap()
    {
        var (cpu, trace) = CreateTracedCpu(0xEB);
        cpu.Registers.DE = 0x1234;
        cpu.Registers.HL = 0x5678;

        Assert.Equal(4, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.DE);
        Assert.Equal((ushort)0x1234, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xEB),
            (BusCycleKind.Refresh, 1, 1));
    }

    [Fact]
    public void TraceOrdersExAfAlternateRegisterSwap()
    {
        var (cpu, trace) = CreateTracedCpu(0x08);
        cpu.Registers.AF = 0x1234;
        cpu.Registers.AlternateAF = 0x5678;

        Assert.Equal(4, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.AF);
        Assert.Equal((ushort)0x1234, cpu.Registers.AlternateAF);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x08),
            (BusCycleKind.Refresh, 1, 1));
    }

    [Fact]
    public void TraceOrdersExxAlternatePairSwaps()
    {
        var (cpu, trace) = CreateTracedCpu(0xD9);
        cpu.Registers.BC = 0x1234;
        cpu.Registers.DE = 0x5678;
        cpu.Registers.HL = 0x9ABC;
        cpu.Registers.AlternateBC = 0x2345;
        cpu.Registers.AlternateDE = 0x6789;
        cpu.Registers.AlternateHL = 0xABCD;

        Assert.Equal(4, cpu.Step());
        Assert.Equal((ushort)0x2345, cpu.Registers.BC);
        Assert.Equal((ushort)0x6789, cpu.Registers.DE);
        Assert.Equal((ushort)0xABCD, cpu.Registers.HL);
        Assert.Equal((ushort)0x1234, cpu.Registers.AlternateBC);
        Assert.Equal((ushort)0x5678, cpu.Registers.AlternateDE);
        Assert.Equal((ushort)0x9ABC, cpu.Registers.AlternateHL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xD9),
            (BusCycleKind.Refresh, 1, 1));
    }

    [Fact]
    public void TraceOrdersIndirectJumpThroughHl()
    {
        var (cpu, trace) = CreateTracedCpu(0xE9);
        cpu.Registers.HL = 0x4567;

        Assert.Equal(4, cpu.Step());
        Assert.Equal((ushort)0x4567, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xE9),
            (BusCycleKind.Refresh, 1, 1));
    }

    [Fact]
    public void TraceOrdersExSpIxReadsBeforeStackWrites()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0xE3);
        cpu.Registers.IX = 0x1234;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(23, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.IX);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xE3),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.MemoryWrite, 0x4000, 0x34),
            (BusCycleKind.MemoryWrite, 0x4001, 0x12));
    }

    [Fact]
    public void TraceOrdersExSpIyReadsBeforeStackWrites()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0xE3);
        cpu.Registers.IY = 0x5678;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(23, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.IY);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xE3),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.MemoryWrite, 0x4000, 0x78),
            (BusCycleKind.MemoryWrite, 0x4001, 0x56));
    }

    [Fact]
    public void TraceOrdersRrdMemoryReadBeforeWrite()
    {
        var (cpu, trace) = CreateTracedCpu(0x3E, 0x12, 0x32, 0x00, 0x40, 0xED, 0x67);
        cpu.Registers.HL = 0x4000;
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();

        Assert.Equal(18, cpu.Step());
        Assert.Equal((byte)0x12, cpu.Registers.A);
        Assert.Equal((byte)0x21, trace.Cycles[^1].Value);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 5, 0xED),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.OpcodeFetch, 6, 0x67),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.MemoryRead, 0x4000, 0x12),
            (BusCycleKind.MemoryWrite, 0x4000, 0x21));
    }

    [Fact]
    public void TraceOrdersRldMemoryReadBeforeWrite()
    {
        var (cpu, trace) = CreateTracedCpu(
            0x3E, 0x5C, 0x32, 0x00, 0x40,
            0x3E, 0xA3,
            0xED, 0x6F);
        cpu.Registers.HL = 0x4000;
        cpu.Step();
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();

        Assert.Equal(18, cpu.Step());
        Assert.Equal((byte)0xA5, cpu.Registers.A);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 7, 0xED),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.OpcodeFetch, 8, 0x6F),
            (BusCycleKind.Refresh, 5, 5),
            (BusCycleKind.MemoryRead, 0x4000, 0x5C),
            (BusCycleKind.MemoryWrite, 0x4000, 0xC3));
    }

    [Fact]
    public void TraceOrdersIndexedImmediateStoreAfterDisplacementRead()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x36, 0x01, 0x5A);
        cpu.Registers.IX = 0x4000;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x36),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 3, 0x5A),
            (BusCycleKind.MemoryWrite, 0x4001, 0x5A));
    }

    [Fact]
    public void TraceOrdersIndexedMemoryLoadAfterDisplacementRead()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x7E, 0x01);
        cpu.Registers.IX = 0x4000;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x00, cpu.Registers.A);
        Assert.Equal((ushort)0x0003, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x7E),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersNegativeIndexedMemoryLoadThroughIy()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0x7E, 0xFF);
        cpu.Registers.IY = 0x4000;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x00, cpu.Registers.A);
        Assert.Equal((ushort)0x0003, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x7E),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0xFF),
            (BusCycleKind.MemoryRead, 0x3FFF, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedMemoryIncrementReadBeforeWrite()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x34, 0x01);
        cpu.Registers.IX = 0x4000;

        Assert.Equal(23, cpu.Step());
        Assert.Equal((byte)0x01, trace.Cycles[^1].Value);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x34),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.MemoryWrite, 0x4001, 0x01));
    }

    [Fact]
    public void TraceOrdersIndexedMemoryDecrementReadBeforeWrite()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0x35, 0xFF);
        cpu.Registers.IY = 0x4000;

        Assert.Equal(23, cpu.Step());
        Assert.Equal((byte)0xFF, trace.Cycles[^1].Value);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x35),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0xFF),
            (BusCycleKind.MemoryRead, 0x3FFF, 0x00),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0xFF));
    }

    [Fact]
    public void TraceOrdersNegativeIndexedBitReadWithoutWrite()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0xCB, 0xFF, 0x46);
        cpu.Registers.IX = 0x4000;

        Assert.Equal(20, cpu.Step());
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xCB),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0xFF),
            (BusCycleKind.MemoryRead, 3, 0x46),
            (BusCycleKind.MemoryRead, 0x3FFF, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedSetReadWriteAndRegisterCopy()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0xCB, 0x01, 0xC0);
        cpu.Registers.IY = 0x4000;

        Assert.Equal(23, cpu.Step());
        Assert.Equal((byte)0x01, cpu.Registers.B);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xCB),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 3, 0xC0),
            (BusCycleKind.MemoryRead, 0x4001, 0x00),
            (BusCycleKind.MemoryWrite, 0x4001, 0x01));
    }

    [Fact]
    public void TraceOrdersInputRegisterPortReadBeforeRegisterUpdate()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x40);
        cpu.Registers.BC = 0x0210;

        Assert.Equal(12, cpu.Step());
        Assert.Equal((byte)0xFF, cpu.Registers.B);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x40),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.IoRead, 0x0210, 0xFF));
    }

    [Fact]
    public void TraceOrdersOutputRegisterPortWrite()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x41);
        cpu.Registers.BC = 0x0210;

        Assert.Equal(12, cpu.Step());
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x41),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.IoWrite, 0x0210, 0x02));
    }

    [Fact]
    public void TraceOrdersEdLoadBcAbsoluteAfterAddressReads()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x4B, 0x00, 0x40);

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.BC);
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x4B),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersEdStoreBcAbsoluteLowByteBeforeHighByte()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x43, 0x00, 0x40);
        cpu.Registers.BC = 0x1234;

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x43),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryWrite, 0x4000, 0x34),
            (BusCycleKind.MemoryWrite, 0x4001, 0x12));
    }

    [Fact]
    public void TraceOrdersEdLoadDeAbsoluteAfterAddressReads()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x5B, 0x00, 0x40);

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.DE);
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x5B),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersEdStoreDeAbsoluteLowByteBeforeHighByte()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x53, 0x00, 0x40);
        cpu.Registers.DE = 0x5678;

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x53),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryWrite, 0x4000, 0x78),
            (BusCycleKind.MemoryWrite, 0x4001, 0x56));
    }

    [Fact]
    public void TraceOrdersEdLoadSpAbsoluteAfterAddressReads()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x7B, 0x00, 0x40);

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.SP);
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x7B),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersEdStoreSpAbsoluteLowByteBeforeHighByte()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x73, 0x00, 0x40);
        cpu.Registers.SP = 0x9ABC;

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x73),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryWrite, 0x4000, 0xBC),
            (BusCycleKind.MemoryWrite, 0x4001, 0x9A));
    }

    [Fact]
    public void TraceOrdersEdLoadHlAbsoluteAfterAddressReads()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x6B, 0x00, 0x40);

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0000, cpu.Registers.HL);
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x6B),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryRead, 0x4000, 0x00),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersEdStoreHlAbsoluteLowByteBeforeHighByte()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x63, 0x00, 0x40);
        cpu.Registers.HL = 0x9ABC;

        Assert.Equal(20, cpu.Step());
        Assert.Equal((ushort)0x0004, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x63),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x00),
            (BusCycleKind.MemoryRead, 3, 0x40),
            (BusCycleKind.MemoryWrite, 0x4000, 0xBC),
            (BusCycleKind.MemoryWrite, 0x4001, 0x9A));
    }

    [Fact]
    public void TraceOrdersLoadImmediateIntoBc()
    {
        var (cpu, trace) = CreateTracedCpu(0x01, 0x34, 0x12);

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x1234, cpu.Registers.BC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x01),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x34),
            (BusCycleKind.MemoryRead, 2, 0x12));
    }

    [Fact]
    public void TraceOrdersLoadImmediateIntoDe()
    {
        var (cpu, trace) = CreateTracedCpu(0x11, 0x78, 0x56);

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.DE);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x11),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x78),
            (BusCycleKind.MemoryRead, 2, 0x56));
    }

    [Fact]
    public void TraceOrdersLoadImmediateIntoSp()
    {
        var (cpu, trace) = CreateTracedCpu(0x31, 0xBC, 0x9A);

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x9ABC, cpu.Registers.SP);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x31),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0xBC),
            (BusCycleKind.MemoryRead, 2, 0x9A));
    }

    [Fact]
    public void TraceOrdersLoadImmediateIntoHl()
    {
        var (cpu, trace) = CreateTracedCpu(0x21, 0x78, 0x56);

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x5678, cpu.Registers.HL);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x21),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x78),
            (BusCycleKind.MemoryRead, 2, 0x56));
    }

    [Fact]
    public void TraceOrdersStoreAccumulatorThroughBc()
    {
        var (cpu, trace) = CreateTracedCpu(0x02);
        cpu.Registers.BC = 0x4000;
        cpu.Registers.A = 0x5A;

        Assert.Equal(7, cpu.Step());
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x02),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryWrite, 0x4000, 0x5A));
    }

    [Fact]
    public void TraceOrdersLoadAccumulatorThroughBc()
    {
        var (cpu, trace) = CreateTracedCpu(0x0A);
        cpu.Registers.BC = 0x4000;

        Assert.Equal(7, cpu.Step());
        Assert.Equal((byte)0x00, cpu.Registers.A);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x0A),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 0x4000, 0x00));
    }

    [Fact]
    public void TraceOrdersStoreAccumulatorThroughDe()
    {
        var (cpu, trace) = CreateTracedCpu(0x12);
        cpu.Registers.DE = 0x4000;
        cpu.Registers.A = 0x5A;

        Assert.Equal(7, cpu.Step());
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x12),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryWrite, 0x4000, 0x5A));
    }

    [Fact]
    public void TraceOrdersLoadAccumulatorThroughDe()
    {
        var (cpu, trace) = CreateTracedCpu(0x1A);
        cpu.Registers.DE = 0x4000;

        Assert.Equal(7, cpu.Step());
        Assert.Equal((byte)0x00, cpu.Registers.A);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x1A),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 0x4000, 0x00));
    }

    [Fact]
    public void TraceOrdersAbsoluteAccumulatorLoadAfterAddressReads()
    {
        var (cpu, trace) = CreateTracedCpu(0x3A, 0x00, 0x40);

        Assert.Equal(13, cpu.Step());
        Assert.Equal((byte)0x00, cpu.Registers.A);
        Assert.Equal((ushort)0x0003, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x3A),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x00),
            (BusCycleKind.MemoryRead, 2, 0x40),
            (BusCycleKind.MemoryRead, 0x4000, 0x00));
    }

    [Fact]
    public void TraceOrdersLoadImmediateIntoMemoryAtHl()
    {
        var (cpu, trace) = CreateTracedCpu(0x36, 0x5A);
        cpu.Registers.HL = 0x4000;

        Assert.Equal(10, cpu.Step());
        Assert.Equal((ushort)0x0002, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x36),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 1, 0x5A),
            (BusCycleKind.MemoryWrite, 0x4000, 0x5A));
    }

    [Fact]
    public void TraceOrdersLoadAccumulatorFromMemoryAtHl()
    {
        var (cpu, trace) = CreateTracedCpu(0x7E);
        cpu.Registers.HL = 0x4000;

        Assert.Equal(7, cpu.Step());
        Assert.Equal((byte)0x00, cpu.Registers.A);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x7E),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 0x4000, 0x00));
    }

    [Fact]
    public void TraceOrdersStoreAccumulatorAtMemoryHl()
    {
        var (cpu, trace) = CreateTracedCpu(0x77);
        cpu.Registers.HL = 0x4000;
        cpu.Registers.A = 0x5A;

        Assert.Equal(7, cpu.Step());
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x77),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryWrite, 0x4000, 0x5A));
    }

    [Fact]
    public void TraceOrdersIndexedAccumulatorStoreThroughIx()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x77, 0x01);
        cpu.Registers.IX = 0x4000;
        cpu.Registers.A = 0x5A;

        Assert.Equal(19, cpu.Step());
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x77),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryWrite, 0x4001, 0x5A));
    }

    [Fact]
    public void TraceOrdersNegativeIndexedAccumulatorStoreThroughIy()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0x77, 0xFF);
        cpu.Registers.IY = 0x4000;
        cpu.Registers.A = 0xA5;

        Assert.Equal(19, cpu.Step());
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x77),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0xFF),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0xA5));
    }

    [Fact]
    public void TraceOrdersRegisterStoreAtMemoryHl()
    {
        var (cpu, trace) = CreateTracedCpu(0x70);
        cpu.Registers.HL = 0x4000;
        cpu.Registers.B = 0x5A;

        Assert.Equal(7, cpu.Step());
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x70),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryWrite, 0x4000, 0x5A));
    }

    [Fact]
    public void TraceOrdersRegisterLoadFromMemoryHl()
    {
        var (cpu, trace) = CreateTracedCpu(0x46);
        cpu.Registers.HL = 0x4000;

        Assert.Equal(7, cpu.Step());
        Assert.Equal((byte)0x00, cpu.Registers.B);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0x46),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.MemoryRead, 0x4000, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedRegisterStoreThroughIx()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x70, 0x01);
        cpu.Registers.IX = 0x4000;
        cpu.Registers.B = 0x5A;

        Assert.Equal(19, cpu.Step());
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x70),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryWrite, 0x4001, 0x5A));
    }

    [Fact]
    public void TraceOrdersIndexedRegisterLoadThroughIx()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x46, 0x01);
        cpu.Registers.IX = 0x4000;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x00, cpu.Registers.B);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x46),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedAddFromMemory()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x86, 0x01);
        cpu.Registers.IX = 0x4000;
        cpu.Registers.A = 0x01;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x01, cpu.Registers.A);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x86),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedSubtractFromMemory()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x96, 0x01);
        cpu.Registers.IX = 0x4000;
        cpu.Registers.A = 0x01;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x01, cpu.Registers.A);
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x96),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedAndFromMemory()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0xA6, 0x01);
        cpu.Registers.IX = 0x4000;
        cpu.Registers.A = 0xFF;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x00, cpu.Registers.A);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.HalfCarry));
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xA6),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedOrFromMemory()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0xB6, 0x01);
        cpu.Registers.IX = 0x4000;
        cpu.Registers.A = 0x01;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x01, cpu.Registers.A);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xB6),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedXorFromMemory()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0xAE, 0x01);
        cpu.Registers.IX = 0x4000;
        cpu.Registers.A = 0xFF;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0xFF, cpu.Registers.A);
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Sign));
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xAE),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedCompareFromMemory()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0xBE, 0x01);
        cpu.Registers.IX = 0x4000;
        cpu.Registers.A = 0x01;

        Assert.Equal(19, cpu.Step());
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0xBE),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedAdcFromMemory()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x8E, 0x01);
        cpu.Registers.IX = 0x4000;
        cpu.Registers.A = 0x01;
        cpu.Registers.F = Z80Flags.Carry;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x02, cpu.Registers.A);
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x8E),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersIndexedSbcFromMemory()
    {
        var (cpu, trace) = CreateTracedCpu(0xDD, 0x9E, 0x01);
        cpu.Registers.IX = 0x4000;
        cpu.Registers.A = 0x01;
        cpu.Registers.F = Z80Flags.Carry;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x00, cpu.Registers.A);
        Assert.False(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Carry));
        Assert.True(Z80Flags.IsSet(cpu.Registers.F, Z80Flags.Zero));
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x9E),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0x01),
            (BusCycleKind.MemoryRead, 0x4001, 0x00));
    }

    [Fact]
    public void TraceOrdersNegativeIYMemoryAdd()
    {
        var (cpu, trace) = CreateTracedCpu(0xFD, 0x86, 0xFF);
        cpu.Registers.IY = 0x4000;
        cpu.Registers.A = 0x01;

        Assert.Equal(19, cpu.Step());
        Assert.Equal((byte)0x01, cpu.Registers.A);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xFD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x86),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.MemoryRead, 2, 0xFF),
            (BusCycleKind.MemoryRead, 0x3FFF, 0x00));
    }

    [Fact]
    public void TraceOrdersInputIntoCUsingOriginalFullPort()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x48);
        cpu.Registers.BC = 0x0210;

        Assert.Equal(12, cpu.Step());
        Assert.Equal((byte)0xFF, cpu.Registers.C);
        Assert.Equal((byte)0x02, cpu.Registers.B);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x48),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.IoRead, 0x0210, 0xFF));
    }

    [Fact]
    public void TraceOrdersOutputFromCUsingFullPort()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x49);
        cpu.Registers.BC = 0x0210;

        Assert.Equal(12, cpu.Step());
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x49),
            (BusCycleKind.Refresh, 2, 2),
            (BusCycleKind.IoWrite, 0x0210, 0x10));
    }

    [Fact]
    public void TraceOrdersLoadInterruptRegisterFromAccumulator()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x47);
        cpu.Registers.A = 0x5A;

        Assert.Equal(9, cpu.Step());
        Assert.Equal((byte)0x5A, cpu.Registers.I);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x47),
            (BusCycleKind.Refresh, 2, 2));
    }

    [Fact]
    public void TraceOrdersLoadRefreshRegisterFromAccumulator()
    {
        var (cpu, trace) = CreateTracedCpu(0xED, 0x4F);
        cpu.Registers.A = 0x5A;

        Assert.Equal(9, cpu.Step());
        Assert.Equal((byte)0x5A, cpu.Registers.R);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 0, 0xED),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x4F),
            (BusCycleKind.Refresh, 2, 2));
    }

    [Theory]
    [InlineData(0x45)]
    [InlineData(0x4D)]
    public void TraceOrdersRetnAndRetiStackReads(byte opcode)
    {
        var (cpu, trace) = CreateTracedCpu(0x21, 0x34, 0x12, 0xE5, 0xED, opcode);
        cpu.Registers.SP = 0x4000;
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();

        Assert.Equal(14, cpu.Step());
        Assert.Equal((ushort)0x1234, cpu.Registers.PC);
        AssertCycles(trace,
            (BusCycleKind.OpcodeFetch, 4, 0xED),
            (BusCycleKind.Refresh, 3, 3),
            (BusCycleKind.OpcodeFetch, 5, opcode),
            (BusCycleKind.Refresh, 4, 4),
            (BusCycleKind.MemoryRead, 0x3FFE, 0x34),
            (BusCycleKind.MemoryRead, 0x3FFF, 0x12));
    }

    [Fact]
    public void TraceIncludesIoAndPrefixCycles()
    {
        var (inputCpu, inputTrace) = CreateTracedCpu(0xDB, 0x10);
        inputCpu.Step();
        Assert.Equal(BusCycleKind.IoRead, inputTrace.Cycles[^1].Kind);
        Assert.Equal((ushort)0x10, inputTrace.Cycles[^1].Address);

        var (outputCpu, outputTrace) = CreateTracedCpu(0xD3, 0x11);
        outputCpu.Registers.A = 0x5A;
        outputCpu.Step();
        Assert.Equal((BusCycleKind.IoWrite, (ushort)0x5A11, (byte)0x5A),
            (outputTrace.Cycles[^1].Kind, outputTrace.Cycles[^1].Address, outputTrace.Cycles[^1].Value));

        var (prefixCpu, prefixTrace) = CreateTracedCpu(0xDD, 0x00);
        prefixCpu.Step();
        AssertCycles(prefixTrace,
            (BusCycleKind.OpcodeFetch, 0, 0xDD),
            (BusCycleKind.Refresh, 1, 1),
            (BusCycleKind.OpcodeFetch, 1, 0x00),
            (BusCycleKind.Refresh, 2, 2));
    }

    [Fact]
    public void ImmediateIoUsesAccumulatorAsHighPortByte()
    {
        var (inputCpu, inputTrace) = CreateTracedCpu(0xDB, 0x10);
        inputCpu.Registers.A = 0xA5;
        inputCpu.Step();
        Assert.Equal((ushort)0xA510, inputTrace.Cycles[^1].Address);

        var (outputCpu, outputTrace) = CreateTracedCpu(0xD3, 0x11);
        outputCpu.Registers.A = 0xA5;
        outputCpu.Step();
        Assert.Equal((ushort)0xA511, outputTrace.Cycles[^1].Address);
    }

    [Fact]
    public void TraceOrdersInterruptAcknowledgeRefreshAndStackWrites()
    {
        var bus = new TestBus { InterruptOpcode = 0xFF, Memory = { [0] = 0xED, [1] = 0x56, [2] = 0xFB, [3] = 0x00 } };
        var lines = new TestInterruptLines { IntAsserted = true };
        var trace = new TraceObserver();
        var cpu = new Z80Cpu(bus, lines, trace);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();

        cpu.Step();

        AssertCycles(trace,
            (BusCycleKind.InterruptAcknowledge, 0, 0xFF),
            (BusCycleKind.Refresh, 5, 5),
            (BusCycleKind.MemoryWrite, 0xFFFD, 0x04),
            (BusCycleKind.MemoryWrite, 0xFFFE, 0x00));
    }

    [Fact]
    public void TraceOrdersNmiRefreshBeforeStackWrites()
    {
        var bus = new TestBus();
        var lines = new TestInterruptLines { NmiAsserted = true };
        var trace = new TraceObserver();
        var cpu = new Z80Cpu(bus, lines, trace);
        cpu.Registers.I = 0xA5;
        cpu.Registers.PC = 0x1234;
        cpu.Registers.SP = 0x4000;

        Assert.Equal(11, cpu.Step());

        AssertCycles(trace,
            (BusCycleKind.Refresh, 0xA501, 1),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x34),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x12));
    }

    [Fact]
    public void TraceOrdersIm2VectorReadsAfterStackWrites()
    {
        var bus = new TestBus
        {
            InterruptOpcode = 0x34,
            Memory = { [0] = 0xED, [1] = 0x5E, [2] = 0xFB, [3] = 0x00, [0x1234] = 0x78, [0x1235] = 0x56 }
        };
        var lines = new TestInterruptLines();
        var trace = new TraceObserver();
        var cpu = new Z80Cpu(bus, lines, trace);
        cpu.Registers.I = 0x12;
        cpu.Registers.SP = 0x4000;
        cpu.Step();
        cpu.Step();
        cpu.Step();
        trace.Cycles.Clear();
        lines.IntAsserted = true;

        Assert.Equal(19, cpu.Step());

        AssertCycles(trace,
            (BusCycleKind.InterruptAcknowledge, 0, 0x34),
            (BusCycleKind.Refresh, 0x1205, 5),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x04),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x00),
            (BusCycleKind.MemoryRead, 0x1234, 0x78),
            (BusCycleKind.MemoryRead, 0x1235, 0x56));
    }

    [Fact]
    public void TraceOrdersIm0AcknowledgedRstWithoutOpcodeFetch()
    {
        var bus = new TestBus
        {
            InterruptOpcode = 0xFF,
            Memory = { [0] = 0xED, [1] = 0x46, [2] = 0xFB, [3] = 0x00 }
        };
        var lines = new TestInterruptLines();
        var trace = new TraceObserver();
        var cpu = new Z80Cpu(bus, lines, trace);
        cpu.Registers.SP = 0x4000;
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Registers.PC = 0x1234;
        trace.Cycles.Clear();
        lines.IntAsserted = true;

        Assert.Equal(13, cpu.Step());
        Assert.Equal((ushort)0x0038, cpu.Registers.PC);

        AssertCycles(trace,
            (BusCycleKind.InterruptAcknowledge, 0, 0xFF),
            (BusCycleKind.Refresh, 5, 5),
            (BusCycleKind.MemoryWrite, 0x3FFE, 0x34),
            (BusCycleKind.MemoryWrite, 0x3FFF, 0x12));
    }

    private static (Z80Cpu Cpu, TraceObserver Trace) CreateTracedCpu(params byte[] program)
    {
        var bus = new TestBus();
        Array.Copy(program, bus.Memory, program.Length);
        var trace = new TraceObserver();
        return (new Z80Cpu(bus, new InterruptLines(), trace), trace);
    }

    private static void AssertCycles(TraceObserver trace, params (BusCycleKind Kind, ushort Address, byte Value)[] expected)
    {
        Assert.Equal(expected.Length, trace.Cycles.Count);
        for (var index = 0; index < expected.Length; index++)
            Assert.Equal(expected[index], (trace.Cycles[index].Kind, trace.Cycles[index].Address, trace.Cycles[index].Value));
    }

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];
        public byte InterruptOpcode { get; set; } = 0xFF;
        public int OpcodeFetches { get; private set; }
        public int MemoryReads { get; private set; }
        public int AcknowledgeCount { get; private set; }
        public byte ReadMemory(ushort address)
        {
            MemoryReads++;
            return Memory[address];
        }
        public byte ReadOpcode(ushort address)
        {
            OpcodeFetches++;
            return Memory[address];
        }
        public void WriteMemory(ushort address, byte value) => Memory[address] = value;
        public byte ReadPort(byte port) => 0xFF;
        public void WritePort(byte port, byte value) { }
        public byte AcknowledgeInterrupt()
        {
            AcknowledgeCount++;
            return InterruptOpcode;
        }
    }

    private sealed class TestInterruptLines : IInterruptLines
    {
        public bool IntAsserted { get; set; }
        public bool NmiAsserted { get; set; }
        public bool WaitAsserted { get; set; }
        public void Clear() { }
    }

    private sealed class TraceObserver : IBusCycleObserver
    {
        public List<BusCycle> Cycles { get; } = [];
        public void Observe(BusCycle cycle) => Cycles.Add(cycle);
    }
}
