using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet.CbmDos;
using PetEmulator.Vic20.Keyboard;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests.Serial;

/// <summary>Layer 2 IEC coverage: real BASIC/KERNAL commands typed through the VIC-20 keyboard,
/// rather than direct serial-bus or register operations.</summary>
public sealed class Vic20DiskEndToEndTests
{
    [Test]
    [CancelAfter(60_000)]
    public void LoadDirectoryThenList_ShowsTheMountedDisksName()
    {
        const string DiskName = "IECEND";
        var diskPath = CreateDisk(DiskName);
        try
        {
            var machine = BootMachine(diskPath);
            var before = CountScreenText(machine, DiskName);

            Vic20TextTyper.Type(machine, "LOAD\"$\",8\n", holdInstructions: 12_000, gapInstructions: 12_000);
            machine.Run(2_000_000);
            machine.PollDiskActivity().Should().BeTrue("IEC byte traffic should latch disk activity");
            machine.PollDiskActivity().Should().BeFalse("disk activity should be consumed by one poll");
            Vic20TextTyper.Type(machine, "LIST\n", holdInstructions: 12_000, gapInstructions: 12_000);
            machine.Run(500_000);

            CountScreenText(machine, DiskName).Should().BeGreaterThan(before,
                "LIST should display the directory loaded from the mounted IEC disk");
        }
        finally
        {
            File.Delete(diskPath);
        }
    }

    [Test]
    [CancelAfter(60_000)]
    public void SaveNewLoadRun_RoundTripsAProgramThroughTheMountedDisk()
    {
        var diskPath = CreateDisk("IECSAVE");
        try
        {
            var machine = BootMachine(diskPath);
            var beforeRun = CountScreenCode(machine, 0x34); // '4'; count, not presence, avoids boot-banner false positives.

            Vic20TextTyper.Type(machine, "10 PRINT2+2\n", holdInstructions: 12_000, gapInstructions: 12_000);
            Vic20TextTyper.Type(machine, "SAVE\"TESTFILE\",8\n", holdInstructions: 12_000, gapInstructions: 12_000);
            machine.Run(2_000_000);

            Vic20TextTyper.Type(machine, "NEW\n", holdInstructions: 12_000, gapInstructions: 12_000);
            machine.Run(200_000);
            Vic20TextTyper.Type(machine, "LOAD\"TESTFILE\",8\n", holdInstructions: 12_000, gapInstructions: 12_000);
            machine.Run(2_000_000);
            Vic20TextTyper.Type(machine, "RUN\n", holdInstructions: 12_000, gapInstructions: 12_000);
            machine.Run(500_000);

            CountScreenCode(machine, 0x34).Should().BeGreaterThan(beforeRun,
                "RUN can only print 4 after the program was saved, cleared by NEW, and loaded from disk");
        }
        finally
        {
            File.Delete(diskPath);
        }
    }

    private static Vic20Machine BootMachine(string diskPath)
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
        machine.MountDisk(diskPath);
        machine.RunUntil(_ => machine.Vic.Columns > 0 && machine.Vic.Rows > 0, 2_000_000)
            .Should().BeTrue("the real VIC-20 KERNAL must boot before accepting BASIC commands");
        machine.Run(200_000);
        return machine;
    }

    private static string CreateDisk(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), $"vic20-disk-e2e-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, D64Image.CreateFormatted(name, "00"));
        return path;
    }

    private static int CountScreenText(Vic20Machine machine, string text)
    {
        var codes = text.Select(ch => (byte)(ch - 'A' + 1)).ToArray();
        var count = 0;
        for (var start = 0; start + codes.Length <= machine.Vic.Columns * machine.Vic.Rows; start++)
        {
            var match = true;
            for (var i = 0; i < codes.Length; i++)
                if (machine.Memory.Read((ushort)(machine.Vic.ScreenAddr + start + i)) != codes[i]) { match = false; break; }
            if (match) count++;
        }
        return count;
    }

    private static int CountScreenCode(Vic20Machine machine, byte code)
    {
        var count = 0;
        for (var i = 0; i < machine.Vic.Columns * machine.Vic.Rows; i++)
            if (machine.Memory.Read((ushort)(machine.Vic.ScreenAddr + i)) == code) count++;
        return count;
    }
}
