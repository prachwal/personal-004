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
        // The real Gate Array latches a mode change at the next HSync, not immediately on the
        // port write - one CRTC clock reaches that edge from the post-Reset counter state.
        machine.Bus.Tick(4);
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
        machine.Bus.Tick(4);
        machine.Bus.GateArray.RenderFrame();
        Assert.That(machine.Bus.GateArray.Pixels[0], Is.EqualTo(expectedPixel0));
    }
}
