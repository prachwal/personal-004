using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;

namespace PetEmulator.Core.Tests;

[TestFixture]
public sealed class MachineContractTests
{
    [Test]
    public void Contract_keeps_machine_lifecycle_cpu_agnostic()
    {
        IMachine machine = new TestMachine();

        machine.Name.Should().Be("test");
        machine.IsReady.Should().BeTrue();
        machine.Reset();
        machine.StepInstruction();
        machine.Run(2);
        machine.CycleCount.Should().Be(3);
    }

    [Test]
    public void Shared_clock_is_monotonic_and_resettable()
    {
        var clock = new EmulationClock();

        clock.Advance(4);
        clock.Advance(7);

        clock.CycleCount.Should().Be(11);
        clock.Reset();
        clock.CycleCount.Should().Be(0);
    }

    [Test]
    public void Optional_capabilities_remain_separate_from_processor_lifecycle()
    {
        IPortBus ports = new TestPortBus();
        IInterruptLines lines = new TestInterruptLines();
        IWaitLine wait = new TestInterruptLines();
        IFirqProcessor firq = new TestFirqProcessor();

        ports.Read(0x12).Should().Be(0xA5);
        lines.IntAsserted.Should().BeFalse();
        lines.NmiAsserted.Should().BeFalse();
        wait.WaitAsserted.Should().BeFalse();
        firq.SetFIRQ(true);
    }

    private sealed class TestMachine : IMachine
    {
        public string Name => "test";
        public bool IsReady => true;
        public ulong CycleCount { get; private set; }
        public IProcessor Processor { get; } = new TestProcessor();
        public IMemoryBus Memory { get; } = new TestMemoryBus();

        public void Reset() => CycleCount = 0;

        public void StepInstruction() => CycleCount++;

        public void Run(ulong instructionCount)
        {
            for (var i = 0UL; i < instructionCount; i++)
                StepInstruction();
        }
    }

    private sealed class TestProcessor : IProcessor
    {
        public bool Halted => false;
        public ulong CycleCount => 0;
        public ulong InstructionCount => 0;
        public void Reset() { }
        public void StepInstruction() { }
        public void SetIRQ(bool active) { }
        public void SetNMI(bool active) { }
    }

    private sealed class TestMemoryBus : IMemoryBus
    {
        public byte Read(ushort address) => 0;
        public void Write(ushort address, byte value) { }
    }

    private sealed class TestPortBus : IPortBus
    {
        public byte Read(ushort port) => 0xA5;
        public void Write(ushort port, byte value) { }
    }

    private sealed class TestInterruptLines : IInterruptLines, IWaitLine
    {
        public bool IntAsserted => false;
        public bool NmiAsserted => false;
        public bool WaitAsserted => false;
    }

    private sealed class TestFirqProcessor : IFirqProcessor
    {
        public void SetFIRQ(bool active) { }
    }
}
