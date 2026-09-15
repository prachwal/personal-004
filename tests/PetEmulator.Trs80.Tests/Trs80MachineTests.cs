using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Trs80;
using PetEmulator.Trs80.Display;

namespace PetEmulator.Trs80.Tests;

[TestFixture]
public sealed class Trs80MachineTests
{
    [Test]
    public void KeyboardMatrix_MapsRowsAndReleasesKeys()
    {
        var keyboard = new Trs80KeyboardMatrix();
        keyboard.SetKeyDown(Trs80Key.A, true);
        keyboard.Read(0x3801).Should().Be(0x02);
        keyboard.SetKeyDown(Trs80Key.A, false);
        keyboard.Read(0x3801).Should().Be(0);
    }

    [Test]
    public void MemoryBus_DispatchesRomKeyboardVideoPrinterAndFdc()
    {
        var keyboard = new Trs80KeyboardMatrix();
        var printer = new Trs80Printer();
        var fdc = new Trs80FdcWiring();
        var bus = new Trs80MemoryBus(keyboard, printer, fdc);
        bus.LoadRom(new byte[Trs80MemoryMap.RomEnd]);
        bus.Write(Trs80MemoryMap.VideoRamStart, 0x41);
        bus.Read(Trs80MemoryMap.VideoRamStart).Should().Be(0x41);
        bus.Write(Trs80MemoryMap.PrinterStart, 0x48);
        printer.Output.Should().ContainSingle().Which.Should().Be(0x48);
        bus.Write(Trs80MemoryMap.FdcStart, 1);
        fdc.SelectedDrive.Should().Be(0);
    }

    [Test]
    public void DiskAdapter_ReadsAndWritesJv1Sectors()
    {
        var bytes = Enumerable.Range(0, Jv1DiskImage.SectorSizeBytes * Jv1DiskImage.SectorsPerTrack)
            .Select(value => (byte)value).ToArray();
        var adapter = new Trs80DiskImageAdapter(Jv1DiskImage.Load(bytes));
        var sector = new byte[256];
        adapter.TryReadSector(0, 0, sector).Should().BeTrue();
        sector[1].Should().Be(1);
        adapter.TryWriteSector(0, 1, Enumerable.Repeat((byte)0xA5, 256).ToArray()).Should().BeTrue();
        adapter.TryReadSector(0, 1, sector).Should().BeTrue();
        sector.Should().OnlyContain(value => value == 0xA5);
    }

    [Test]
    public void Machine_RunsBoundedInstructionsWithoutThrowing()
    {
        var rom = new byte[Trs80MemoryMap.RomEnd];
        rom[0] = 0x00; // NOP
        var machine = new Trs80Machine(rom);
        machine.Run(100);
        machine.IsReady.Should().BeTrue();
        machine.CycleCount.Should().BeGreaterThan(0);
    }

    [Test]
    public void CassetteEncoding_UsesModelIWaveformAndClock()
    {
        Trs80CassetteEncoding.ToTStates(128).Should().Be(227);
        Trs80CassetteEncoding.ToTStates(1871).Should().Be(3319);
        Trs80CassetteEncoding.ZeroBit.Should().Equal((128d, true), (1871d, false));
        Trs80CassetteEncoding.OneBit.Should().Equal((128d, true), (128d, false), (876d, true), (988d, false));
    }

    [Test]
    public void CassettePlayer_LatchesRisingEdgesAndReachesEndOfTape()
    {
        var cassette = new Trs80CassettePlayer([0x80]);
        cassette.Write(Trs80MemoryMap.CassettePort, 0x04);
        cassette.Tick(1);
        cassette.Read(Trs80MemoryMap.CassettePort).Should().Be(0x80);
        cassette.Read(Trs80MemoryMap.CassettePort).Should().Be(0);
        cassette.Tick(30000);
        cassette.AtEndOfTape.Should().BeTrue();
    }

    [Test]
    public void CharacterFont_UsesBothMcm667xBanks()
    {
        var rom = new byte[2048];
        rom[1] = 0x01;
        rom[1024 + 1] = 0x1F;
        var font = new Trs80CharacterFont(rom);
        font.GlyphWidth.Should().Be(6);
        font.GlyphHeight.Should().Be(12);
        font.GetGlyphRow(0, 2).Should().Be(0x08);
        font.GetGlyphRow(128, 2).Should().Be(0xF8);
    }

    [Test]
    public void RasterDisplay_RendersSemigraphicBlocksAndVideoTiming()
    {
        var bus = new Trs80MemoryBus(new Trs80KeyboardMatrix(), new Trs80Printer());
        bus.Write(Trs80MemoryMap.VideoRamStart, 0x81);
        var display = new Trs80RasterDisplay(bus, new Trs80CharacterFont(new byte[2048]));
        var frame = new uint[Trs80RasterDisplay.PixelWidth * Trs80RasterDisplay.PixelHeight];
        display.Render(frame);
        frame[0].Should().Be(0xFFFFFFFFu);
        frame[4 * Trs80RasterDisplay.PixelWidth].Should().Be(0xFF000000u);
        display.Tick(113 * 262);
        display.Scanline.Should().Be(0);
    }

    [Test]
    public void MachineReset_PreservesRam()
    {
        var machine = new Trs80Machine(new byte[Trs80MemoryMap.RomEnd]);
        machine.Memory.Write(0x4000, 0x5A);
        machine.Reset();
        machine.Memory.Read(0x4000).Should().Be(0x5A);
    }

    [Test]
    public void InsertDisk_HotSwapsWithoutRebuildingTheMachine()
    {
        // Machine built with no disk at all (Fdc is null - see the constructor's own doc comment
        // on why "no disk at boot" means "no FDC", not "an FDC with nothing inserted"). InsertDisk
        // must attach a controller lazily on first call, then reuse it on a later swap - neither
        // step may touch RAM/CPU state, unlike the old rebuild-a-new-machine approach.
        var machine = new Trs80Machine(new byte[Trs80MemoryMap.RomEnd]);
        machine.Fdc.Should().BeNull();
        machine.Memory.Write(0x4000, 0x5A);

        var bytes = Enumerable.Range(0, Jv1DiskImage.SectorSizeBytes * Jv1DiskImage.SectorsPerTrack)
            .Select(value => (byte)value).ToArray();
        machine.InsertDisk(Jv1DiskImage.Load(bytes));
        var fdcAfterFirstInsert = machine.Fdc;
        fdcAfterFirstInsert.Should().NotBeNull();
        machine.Memory.Read(0x4000).Should().Be(0x5A);

        var swapped = Enumerable.Range(0, Jv1DiskImage.SectorSizeBytes * Jv1DiskImage.SectorsPerTrack)
            .Select(value => (byte)(value + 1)).ToArray();
        machine.InsertDisk(Jv1DiskImage.Load(swapped));
        machine.Fdc.Should().BeSameAs(fdcAfterFirstInsert);
        machine.Memory.Read(0x4000).Should().Be(0x5A);
    }

    [Test]
    public void LoadTape_AttachesCassetteWithoutRebuildingTheMachine()
    {
        var machine = new Trs80Machine(new byte[Trs80MemoryMap.RomEnd]);
        machine.Cassette.Should().BeNull();
        machine.Memory.Write(0x4000, 0x5A);

        machine.LoadTape([0x80]);

        machine.Cassette.Should().NotBeNull();
        machine.Memory.Read(0x4000).Should().Be(0x5A);
    }
}
