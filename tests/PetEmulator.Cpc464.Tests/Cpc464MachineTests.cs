using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Cpc;
using PetEmulator.Cpc464;

namespace PetEmulator.Cpc464.Tests;

[TestFixture]
public sealed class Cpc464MachineTests
{
    [Test]
    public void LowerAndUpperFirmwareWindowsAreVisibleAtReset()
    {
        var rom = new byte[0x8000]; rom[0] = 0xA5; rom[0x4000] = 0x5A;
        var machine = new Cpc464Machine(rom);
        Assert.That(machine.Memory.Read(0), Is.EqualTo(0xA5));
        Assert.That(machine.Memory.Read(0xC000), Is.EqualTo(0x5A));
    }

    [Test]
    public void GateArrayCommandCanDisableFirmwareAndSelectMode()
    {
        var machine = new Cpc464Machine(new byte[0x8000]);
        machine.Bus.WritePort(0x7F00, 0x80 | 0x02 | 0x04 | 0x08);
        Assert.That(machine.Bus.GateArray.LowerRomEnabled, Is.False);
        Assert.That(machine.Bus.GateArray.UpperRomEnabled, Is.False);
        // The real Gate Array latches a mode change at the next HSync, not immediately on the
        // port write - one CRTC clock reaches that edge from the post-Reset counter state.
        machine.Tick(4);
        Assert.That(machine.Bus.GateArray.Mode, Is.EqualTo(CpcDisplayMode.Mode2));
    }

    [Test]
    public void WritesBehindFirmwareOverlayReachPhysicalRam()
    {
        var machine = new Cpc464Machine(new byte[0x8000]);
        machine.Memory.Write(0, 0x7C);
        machine.Bus.WritePort(0x7F00, 0x80 | 0x04);
        Assert.That(machine.Memory.Read(0), Is.EqualTo(0x7C));
        Assert.That(machine.Bus.GateArray.LowerRomEnabled, Is.False);
        Assert.That(machine.Memory.Read(0), Is.EqualTo(0x7C));
    }

    [Test]
    public void SnapshotRestoresCpc464MachineState()
    {
        var machine = new Cpc464Machine(new byte[0x8000]);
        machine.Memory.Write(0x4000, 0xA5);
        machine.Bus.Keyboard.SetKey(3, 4, true);
        machine.Bus.Cassette.LoadTape([1, 2, 3]);
        machine.StepInstruction();

        var snapshot = machine.CaptureState();
        var restored = new Cpc464Machine(new byte[0x8000]);
        restored.RestoreState(snapshot);
        Assert.That(machine, Is.AssignableTo<IMachineStateStore<Cpc464Snapshot>>());
        Assert.That(snapshot, Is.AssignableTo<IMachineSnapshot>());

        Assert.That(snapshot, Is.TypeOf<Cpc464Snapshot>());
        Assert.That(snapshot, Is.AssignableTo<CpcMachineSnapshot>());
        Assert.That(restored.Memory.Read(0x4000), Is.EqualTo(0xA5));
        Assert.That(restored.Bus.Keyboard.ReadRow(3), Is.EqualTo(0xEF));
        Assert.That(restored.CycleCount, Is.EqualTo(machine.CycleCount));
        Assert.That(restored.Cpu.Registers.PC, Is.EqualTo(machine.Cpu.Registers.PC));
    }

    [Test]
    public void PpiPortsRouteKeyboardAndCassetteSignals()
    {
        var machine = new Cpc464Machine(new byte[Cpc464Bus.RomSize]);
        machine.Bus.WritePort(0xF700, 0x82); // Port A/C output, Port B input
        machine.Bus.Keyboard.SetKey(3, 4, true);

        machine.Bus.WritePort(0xF400, 14); // PSG register 14 selects the keyboard while Port A outputs
        machine.Bus.WritePort(0xF600, 0x43); // keyboard row 3, PSG read mode
        machine.Bus.WritePort(0xF700, 0x92); // switch Port A to input for the PSG read
        Assert.That(machine.Bus.ReadPort(0xF400), Is.EqualTo(0xEF));

        machine.Bus.WritePort(0xF700, 0x82);
        machine.Bus.WritePort(0xF600, 0x10); // cassette motor on
        Assert.That(machine.Bus.Cassette.MotorOn, Is.True);
        machine.Bus.WritePort(0xF600, 0x00);
        Assert.That(machine.Bus.Cassette.MotorOn, Is.False);
    }

    [TestCase(0, 15)] // Mode0: bits 7,3,5,1 of 0xFF -> 4 ink bits all set
    [TestCase(1, 3)]  // Mode1: bits 7,3
    [TestCase(2, 1)]  // Mode2: bit 7 only
    public void RenderFrameDecodesTheFirstPixelPerActiveMode(int modeBits, int expectedPixel0)
    {
        // Same video byte (0xFF) decoded under each Gate Array mode must follow that mode's own
        // bit order (previously RenderFrame always used Mode 1's regardless of the active mode).
        // A fresh machine per mode avoids the CRTC's un-programmed H-total (R0=0), under which
        // HSync never falls again after its first tick and a later mode write can't re-latch.
        var machine = new Cpc464Machine(new byte[0x8000]);
        machine.Memory.Write(0, 0xFF);
        machine.Bus.WritePort(0x7F00, (byte)(0x80 | modeBits));
        machine.Tick(4);
        machine.Bus.GateArray.RenderFrame();
        Assert.That(machine.Bus.GateArray.Pixels[0], Is.EqualTo(expectedPixel0));
    }
}
