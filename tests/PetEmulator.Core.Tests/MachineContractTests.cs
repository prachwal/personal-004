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
}
