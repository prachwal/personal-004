using PetEmulator.Core;

namespace PetEmulator.DebugApi.Tests;

/// <summary>Minimal <see cref="IMachine"/> test double, in the same spirit as
/// <c>MachineContractTests.TestMachine</c> in <c>PetEmulator.Core.Tests</c>.</summary>
internal sealed class FakeMachine : IMachine
{
    public string Name => "fake";
    public bool IsReady => true;
    public ulong CycleCount => Processor.CycleCount;
    public IProcessor Processor { get; } = new FakeProcessor();
    public IMemoryBus Memory { get; } = new FakeMemoryBus();

    public void Reset() => Processor.Reset();

    public void StepInstruction() => Processor.StepInstruction();

    public void Run(ulong instructionCount)
    {
        for (ulong i = 0; i < instructionCount && !Processor.Halted; i++)
            Processor.StepInstruction();
    }
}

internal sealed class FakeProcessor : IProcessor
{
    public bool Halted { get; private set; }
    public ulong CycleCount { get; private set; }
    public ulong InstructionCount { get; private set; }

    public void Reset()
    {
        Halted = false;
        CycleCount = 0;
        InstructionCount = 0;
    }

    /// <summary>Deliberately non-atomic: reads both counters, yields the thread, then writes them
    /// back. Without a lock serializing callers, two concurrent invocations racing through this
    /// gap lose an update. This is what makes the concurrency test in
    /// <c>DebugApiHostTests</c> an actual regression test for the ported UI-thread-blocking bug's
    /// fix (a <see cref="Lock"/> around every machine access in <c>DebugApiHost</c>) rather than
    /// something that would pass by accident either way.</summary>
    public void StepInstruction()
    {
        if (Halted)
            return;

        ulong nextInstructionCount = InstructionCount + 1;
        ulong nextCycleCount = CycleCount + 2;
        Thread.Yield();
        InstructionCount = nextInstructionCount;
        CycleCount = nextCycleCount;
    }

    public void SetIRQ(bool active) { }

    public void SetNMI(bool active) { }
}

internal sealed class FakeMemoryBus : IMemoryBus
{
    private readonly byte[] _bytes = new byte[65536];

    public byte Read(ushort address) => _bytes[address];

    public void Write(ushort address, byte value) => _bytes[address] = value;
}
