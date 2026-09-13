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

    private sealed class TestObserver : ICpuExecutionObserver
    {
        public List<CpuStepTrace> Completed { get; } = [];

        public bool ShouldBreak(CpuDebugSnapshot snapshot) => false;

        public bool ShouldBreakOnMemoryAccess(BusAccess access) => false;

        public void OnStepCompleted(CpuStepTrace trace) => Completed.Add(trace);

        public void OnStepFailed(CpuDebugSnapshot snapshot, Exception exception) => throw exception;
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
