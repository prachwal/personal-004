using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Chips;
using PetEmulator.Vic20.Cartridge.Abstractions;
using PetEmulator.Vic20.Cartridge.Rtc;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20RtcCartridgeTests
{
    [Test]
    public void Plugin_DeclaresRomAndMc146818Io3Resources()
    {
        var descriptor = new RtcCartridgePlugin().Descriptor;

        descriptor.Resources.Should().Contain(resource => resource.StartAddress == 0xA000
            && resource.Length == 0x2000
            && resource.Kind == Vic20CartridgeResourceKind.Rom);
        descriptor.Resources.Should().Contain(resource => resource.StartAddress == 0x9C00
            && resource.Length == 2
            && resource.Kind == Vic20CartridgeResourceKind.Io);
    }

    [Test]
    public void Machine_MapsRtcRegistersAndExpansionIrq()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
        var instance = new RtcCartridgePlugin().Create(new byte[] { 0xEA });
        machine.AttachExpansionDevice(instance);

        machine.Memory.Write(0x9C00, MC146818.RegisterB);
        machine.Memory.Write(0x9C01, 0x12);
        instance.Tick(1_022_727);

        machine.Memory.Write(0x9C00, MC146818.RegisterC);
        machine.Memory.Read(0x9C01).Should().Be(0x90);
        instance.Irq.Should().BeFalse();
    }

    [Test]
    [CancelAfter(30_000)]
    public void RtcProgram_UsesNativeCartridgeAutostartAndWritesTimeToTopRightOfScreen()
    {
        var romsRoot = RomLocator.Directory("kernal.bin");
        var pluginPath = typeof(RtcCartridgePlugin).Assembly.Location;
        var imagePath = Path.Combine(romsRoot, "cartridges", "vic20-mc146818-rtc.bin");
        var machine = new Vic20Machine(romsRoot);

        machine.MountCartridgePlugin(pluginPath, imagePath);
        machine.Memory.Read(0xA000).Should().Be(0x09);
        machine.Memory.Read(0xA001).Should().Be(0xA0);
        machine.Memory.Read(0xA004).Should().Be(0x41);
        machine.Memory.Read(0xA008).Should().Be(0xCD);
        machine.RunUntil(_ => machine.Processor is IDebuggableProcessor debug
            && (ushort)debug.GetRegisters()["PC"] >= 0xA009
            && (ushort)debug.GetRegisters()["PC"] < 0xC000, 500_000)
            .Should().BeTrue();
        machine.Run(5_000_000);
        var pc = (ushort)((IDebuggableProcessor)machine.Processor).GetRegisters()["PC"];
        pc.Should().NotBeInRange(0xA000, 0xBFFF, "the cartridge must hand control to BASIC");
        var irqVector = (ushort)(machine.Memory.Read(0x0314) | (machine.Memory.Read(0x0315) << 8));
        irqVector.Should().BeInRange(0xBF40, 0xBFFF, "the BASIC SYS stage must install the cartridge IRQ handler");
        machine.Vic.Columns.Should().Be(22, "native cartridge startup must initialize the VIC display");
        machine.Vic.Rows.Should().Be(23, "native cartridge startup must initialize the VIC display");
        machine.Vic.ScreenAddr.Should().Be(0x1E00, "native cartridge startup must initialize screen RAM");
        Enumerable.Range(0, machine.Vic.Columns * machine.Vic.Rows)
            .Select(index => machine.Memory.Read((ushort)(machine.Vic.ScreenAddr + index)))
            .Count(code => code is >= 1 and <= 26)
            .Should().BeGreaterThan(4, "BASIC must remain usable after the RTC cartridge autostart");
        var clockCodes = Enumerable.Range(0x1E11, 5)
            .Select(address => machine.Memory.Read((ushort)address))
            .ToArray();
        clockCodes.Should().Contain(0x3A, "the IRQ routine writes the HH:MM separator");
        clockCodes.Count(code => code is >= 0x30 and <= 0x39).Should().Be(4);
    }
}
