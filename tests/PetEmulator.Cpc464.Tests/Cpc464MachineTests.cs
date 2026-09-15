using NUnit.Framework;
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
}
