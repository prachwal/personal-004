using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Debugger;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20MachineTests
{
    [Test]
    public void Keyboard_RoutesThroughVia2PortBOutPortAIn_NotVia1()
    {
        // Layer 1 (register-level bus integration, no KERNAL involved) for the real wiring bug
        // docs/vic20-rendering-fixes.md's keyboard investigation found: row-select is VIA2 port B
        // ($9120), column readback is VIA2 port A ($9121) - confirmed against the real KERNAL
        // disassembly (docs/vic20-disassembly/kernal.asm). An earlier version of this wiring used
        // VIA1 port A/port B instead - every real keypress silently vanished.
        var machine = CreateMachine();
        machine.Keyboard.Press(2, 0);

        machine.Via2.Write(0x9120, unchecked((byte)~(1 << 2))); // select row 2 only

        machine.Via2.Read(0x9121).Should().Be(unchecked((byte)~1), "row 2 col 0 is pressed");
    }

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
