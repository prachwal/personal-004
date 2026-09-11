using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Cpu6502;
using PetEmulator.Chips;
using PetEmulator.Debugger;
using PetEmulator.Pet.CbmDos;
using PetEmulator.Vic20.Tests.Roms;
using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20MachineTests
{
    [Test]
    public void DisplayConfigSelectsTheMatchingVicTimingProfile()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"), Vic20DisplayConfig.Pal);

        machine.DisplayConfig.Should().Be(Vic20DisplayConfig.Pal);
        machine.Vic.Standard.Should().Be(MOS6560Standard.Pal);
        machine.Vic.TimingCyclesPerLine.Should().Be(MOS6560.PalCyclesPerLine);
        machine.Vic.TimingTotalScanlines.Should().Be(MOS6560.PalTotalScanlines);
    }
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
    [CancelAfter(10_000)]
    public void Via1TimerIrq_IsPropagatedToThe6502IrqVector()
    {
        var machine = CreateMachine();
        machine.RunUntil(_ => machine.Vic.Columns > 0 && machine.Vic.Rows > 0, 2_000_000)
            .Should().BeTrue("the KERNAL must finish initialization before the timer IRQ is injected");

        var reads = new List<ushort>();
        machine.BusObserver = access =>
        {
            if (!access.IsWrite)
                reads.Add(access.Address);
        };

        machine.Memory.Write(0x911D, 0x7F); // clear any boot-time VIA1 interrupt flags
        ((PetEmulator.Cpu6502.Cpu6502)machine.Processor).Registers.P &= unchecked((byte)~0x04); // CLI for the test CPU
        machine.Memory.Write(0x911E, 0xC0); // enable VIA1 Timer 1 interrupt
        machine.Memory.Write(0x9114, 0x01);
        machine.Memory.Write(0x9115, 0x00); // start T1 with a two-cycle period
        machine.Run(20);

        reads.Should().Contain(0xFFFE, "the 6502 must fetch the low byte of the IRQ vector");
        reads.Should().Contain(0xFFFF, "the 6502 must fetch the high byte of the IRQ vector");
    }

    [Test]
    public void Joystick_DirectionsAndFireAreActiveLowOnTheRealVias()
    {
        var machine = CreateMachine();
        machine.Via1.Write(0x9113, 0x00); // PA2-PA5 as inputs
        machine.Via2.Write(0x9122, 0x00); // PB7 as input

        machine.Joystick.Set(Vic20JoystickInput.Up, true);
        machine.Joystick.Set(Vic20JoystickInput.Left, true);
        machine.Joystick.Set(Vic20JoystickInput.Right, true);
        machine.Joystick.Set(Vic20JoystickInput.Fire, true);

        var via1PortA = machine.Via1.Read(0x9111);
        var via2PortB = machine.Via2.Read(0x9120);

        (via1PortA & 0x34).Should().Be(0, "UP, LEFT and FIRE are pulled low on VIA1 PA2/PA4/PA5");
        (via1PortA & 0x08).Should().Be(0x08, "DOWN remains released");
        (via2PortB & 0x80).Should().Be(0, "RIGHT is pulled low on VIA2 PB7");

        machine.Joystick.Set(Vic20JoystickInput.Up, false);
        (machine.Via1.Read(0x9111) & 0x04).Should().Be(0x04, "releasing UP restores the pull-up");
    }

    [Test]
    public void UserPort_ExposesVIA1PortBInputDirectionAndOutput()
    {
        var machine = CreateMachine();
        var outputChanges = 0;
        var directionChanges = 0;
        machine.UserPort.OutputChanged += () => outputChanges++;
        machine.UserPort.DirectionChanged += () => directionChanges++;
        machine.UserPort.Input = 0xA5;
        machine.Via1.Write(0x9112, 0x00); // all User Port pins as inputs

        machine.Via1.Read(0x9110).Should().Be(0xA5);

        machine.Via1.Write(0x9112, 0xF0);
        machine.Via1.Write(0x9110, 0x5A);

        machine.UserPort.Direction.Should().Be(0xF0);
        machine.UserPort.Output.Should().Be(0x50);
        outputChanges.Should().Be(1);
        directionChanges.Should().Be(1);
    }

    [Test]
    [CancelAfter(30_000)]
    public void Enter_ActuallyExecutesTheTypedLine_NotJustCrsrDown()
    {
        // The real bug this regression-tests: (3,7) - CRSR-DOWN - and (1,7) - real Enter - both
        // advance the KERNAL's screen line-pointer by exactly one row when pressed alone, so a
        // pointer-movement check can't tell them apart (see
        // Vic20KeyboardMatrixTests.Vic20HostKeyMap_Enter_IsRowOneColSeven and
        // docs/vic20-rendering-fixes.md). Only watching for the typed command's real effect can -
        // CRSR-DOWN never executes "PRINT2+2" no matter how long it's given to run; Enter does.
        var machine = CreateMachine();
        machine.RunUntil(_ => HasLetters(machine), 2_000_000).Should().BeTrue("must boot first");

        void Press(int row, int col)
        {
            machine.Keyboard.Press(row, col);
            machine.Run(8_000);
            machine.Keyboard.Release(row, col);
            machine.Run(8_000);
        }

        Press(1, 5); Press(1, 2); Press(1, 4); Press(3, 4); Press(6, 2); // P R I N T
        Press(7, 0); Press(0, 5); Press(7, 0); // 2 + 2
        Press(1, 7); // Enter

        machine.Run(100_000);

        ScreenContainsDigitFour(machine).Should().BeTrue("Enter should have executed PRINT2+2, printing '4'");
    }

    private static bool HasLetters(Vic20Machine machine)
    {
        var cols = machine.Vic.Columns;
        var rows = machine.Vic.Rows;
        if (cols == 0 || rows == 0)
            return false;
        var screenAddr = machine.Vic.ScreenAddr;
        var letters = 0;
        for (var i = 0; i < cols * rows; i++)
            if (machine.Memory.Read((ushort)(screenAddr + i)) is >= 1 and <= 26)
                letters++;
        return letters > 10;
    }

    private static bool ScreenContainsDigitFour(Vic20Machine machine)
    {
        var cols = machine.Vic.Columns;
        var rows = machine.Vic.Rows;
        var screenAddr = machine.Vic.ScreenAddr;
        for (var i = 0; i < cols * rows; i++)
            if (machine.Memory.Read((ushort)(screenAddr + i)) == 0x34)
                return true;
        return false;
    }

    [Test]
    public void Name_And_IsReady_AreSet()
    {
        var machine = CreateMachine();

        machine.Name.Should().Contain("VIC-20");
        machine.IsReady.Should().BeTrue();
    }

    [Test]
    public void MountDisk_AddsADriveDeviceStatus()
    {
        var machine = CreateMachine();
        var diskPath = TemporaryDiskPath();
        try
        {
            File.WriteAllBytes(diskPath, D64Image.CreateFormatted("VIC20", "00"));

            machine.MountDisk(diskPath);

            machine.HasDisk().Should().BeTrue();
            machine.Devices.Should().ContainSingle(d => d.Id == "ieee488:8")
                .Which.StatusText.Should().Be(Path.GetFileName(diskPath));
        }
        finally
        {
            File.Delete(diskPath);
        }
    }

    [Test]
    public void MountNewDisk_CreatesAndMountsADrive()
    {
        var machine = CreateMachine();
        var diskPath = TemporaryDiskPath();
        try
        {
            machine.MountNewDisk(diskPath, "VIC20", "00", 9);

            File.Exists(diskPath).Should().BeTrue();
            machine.Devices.Should().ContainSingle(d => d.Id == "ieee488:9")
                .Which.StatusText.Should().Be(Path.GetFileName(diskPath));
        }
        finally
        {
            File.Delete(diskPath);
        }
    }

    [Test]
    public void Reset_AfterConstruction_StillWorks()
    {
        var machine = CreateMachine();

        var act = machine.Reset;

        act.Should().NotThrow();
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

    private static string TemporaryDiskPath() => Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.d64");
}
