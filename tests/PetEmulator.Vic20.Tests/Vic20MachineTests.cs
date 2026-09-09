using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Debugger;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20MachineTests
{
    [Test]
    public void Name_And_IsReady_AreSet()
    {
        var machine = CreateMachine();

        machine.Name.Should().Contain("VIC-20");
        machine.IsReady.Should().BeTrue();
    }

    [Test]
    [CancelAfter(10_000)]
    public void Run_AdvancesWithoutThrowing()
    {
        var machine = CreateMachine();

        var act = () => machine.Run(50_000);

        act.Should().NotThrow();
        machine.Processor.InstructionCount.Should().BeGreaterThan(0);
    }

    [Test]
    public void BusObserver_IsOptOutWithZeroBehaviorChangeWhenUnset()
    {
        var machine = CreateMachine();

        var act = () => machine.Run(500);

        act.Should().NotThrow();
        machine.BusObserver.Should().BeNull();
    }

    [Test]
    [CancelAfter(10_000)]
    public void MachineDebugger_WorksAgainstVic20MachineWithNoVic20SpecificCode()
    {
        // MachineDebugger (PetEmulator.Debugger) was built purely against IMachine/IProcessor/
        // IMemoryBus - see docs/pet-debug-tools.md. This proves that promise: it works here with
        // zero VIC-20-specific code, the entire point of building it CPU/machine-agnostic.
        var machine = CreateMachine();
        var debugger = new MachineDebugger(machine);

        var output = debugger.Execute("trace 3");

        output.Should().Contain("cycles=").And.Contain("PC=", "Cpu6502Classic implements IDebuggableProcessor");
    }

    [Test]
    [CancelAfter(30_000)]
    public void RunUntil_FromCore_WorksAgainstVic20MachineUnchanged()
    {
        var machine = CreateMachine();

        var reached = machine.RunUntil(_ => machine.Processor.InstructionCount >= 100, 10_000);

        reached.Should().BeTrue();
    }

    private static Vic20Machine CreateMachine() => new(RomLocator.Directory("kernal.bin"));
}
