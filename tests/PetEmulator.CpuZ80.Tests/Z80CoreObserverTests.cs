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

    private sealed class TestBus : IBus
    {
        public byte[] Memory { get; } = new byte[ushort.MaxValue + 1];

        public byte ReadMemory(ushort address) => Memory[address];

        public void WriteMemory(ushort address, byte value) => Memory[address] = value;

        public byte ReadPort(byte port) => 0xFF;

        public void WritePort(byte port, byte value) { }
    }
}
