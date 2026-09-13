using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Cpu8080;
using PetEmulator.Core;

namespace PetEmulator.Cpu8080.Tests;

[TestFixture]
public sealed class Cpu8080ObserverTests
{
    [Test]
    public void Observer_receives_opcode_mnemonic_and_lifecycle_counters()
    {
        var memory = new TestMemory { Bytes = { [0] = 0x00 } };
        var observer = new TestObserver();
        var cpu = new Cpu8080(memory) { ExecutionObserver = observer };

        cpu.StepInstruction();

        observer.Completed.Should().ContainSingle();
        var trace = observer.Completed[0];
        trace.Opcode.Should().Be(OpcodeKey.Base(0x00));
        trace.Mnemonic.Should().Be("NOP");
        trace.Result.Cycles.Should().Be(4);
        trace.Before.InstructionCount.Should().Be(0);
        trace.After.InstructionCount.Should().Be(1);
    }

    [Test]
    public void Observer_breakpoint_stops_before_fetch_and_execution()
    {
        var memory = new TestMemory { Bytes = { [0] = 0x00 } };
        var observer = new TestObserver { BreakBeforeStep = true };
        var cpu = new Cpu8080(memory) { ExecutionObserver = observer };

        cpu.StepInstruction();

        cpu.CpuState.PC.Should().Be(0);
        cpu.CycleCount.Should().Be(0);
        cpu.InstructionCount.Should().Be(0);
        observer.Completed.Should().ContainSingle()
            .Which.Result.BreakpointHit.Should().BeTrue();
    }

    private sealed class TestObserver : ICpuExecutionObserver
    {
        public bool BreakBeforeStep { get; init; }
        public List<CpuStepTrace> Completed { get; } = [];

        public bool ShouldBreak(CpuDebugSnapshot snapshot) => BreakBeforeStep;

        public bool ShouldBreakOnMemoryAccess(BusAccess access) => false;

        public void OnStepCompleted(CpuStepTrace trace) => Completed.Add(trace);

        public void OnStepFailed(CpuDebugSnapshot snapshot, Exception exception)
            => throw new AssertionException($"Unexpected CPU step failure: {exception.Message}");
    }

    private sealed class TestMemory : IMemoryBus
    {
        public byte[] Bytes { get; } = new byte[ushort.MaxValue + 1];

        public byte Read(ushort address) => Bytes[address];

        public void Write(ushort address, byte value) => Bytes[address] = value;
    }
}
