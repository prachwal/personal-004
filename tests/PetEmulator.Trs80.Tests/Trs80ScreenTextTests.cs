using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Trs80.Tests;

/// <summary>Real-firmware boot + keyboard typing for TRS-80 Model I, decoding the result through
/// <see cref="Trs80ScreenCode"/> instead of raw byte inspection - this machine had neither a
/// keyboard text-typer (see the new <see cref="Trs80TextTyper"/>) nor a decoded-screen-text
/// assertion before this test, unlike PET/VIC-20/CPC464 which all already had one or the other.</summary>
public sealed class Trs80ScreenTextTests
{
    private const int Columns = 64, Rows = 16;

    [Test]
    [CancelAfter(30_000)]
    public void RealBoot_ScreenShowsBasicBanner()
    {
        var machine = CreateMachine();

        BootToReady(machine);

        TestContext.Out.WriteLine(ScreenText(machine));
        ScreenText(machine).Should().Contain("READY", "real Level II BASIC should print its startup banner and reach the READY prompt");
    }

    [Test]
    [CancelAfter(30_000)]
    public void TypedLine_ExecutesInBasic_VisibleAsRealDecodedText()
    {
        var machine = CreateMachine();
        BootToReady(machine).Should().BeTrue("must boot first");

        Trs80TextTyper.Type(machine, "PRINT2+2\n");
        machine.Run(200_000);

        var lines = ScreenText(machine).Split('\n').Select(line => line.TrimEnd()).Where(line => line.Length > 0).ToList();
        TestContext.Out.WriteLine(string.Join('\n', lines));
        var inputLineIndex = lines.FindLastIndex(line => line == ">PRINT2+2");
        inputLineIndex.Should().BeGreaterThanOrEqualTo(0, "the typed line should echo exactly as typed - proves '+' really is Shift+Semicolon, not dropped or mistyped");
        // " 4" - BASIC's PRINT reserves a leading column for a numeric result's sign.
        lines[inputLineIndex + 1].Should().Be(" 4", "PRINT2+2 typed through the real keyboard matrix should actually evaluate and print '4' on the very next line - not just have a stray '4' somewhere on screen (the boot banner's own 'Release 1.4' would false-positive a looser check)");
    }

    /// <summary>Waits for real letters (not just any non-zero byte - early boot RAM can hold
    /// arbitrary garbage that isn't ASCII, which a weaker "not blank" check would false-pass on)
    /// to appear on screen, up to a 3M-instruction budget.</summary>
    private static bool BootToReady(Trs80Machine machine)
    {
        for (var budget = 0UL; budget < 3_000_000; budget += 100_000)
        {
            machine.Run(100_000);
            if (ScreenText(machine).Contains("READY")) return true;
        }
        return false;
    }

    private static string ScreenText(Trs80Machine machine)
    {
        var bytes = new byte[Columns * Rows];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = machine.Memory.Read((ushort)(Trs80MemoryMap.VideoRamStart + i));
        return Trs80ScreenCode.ToText(bytes, Columns, Rows);
    }

    private static Trs80Machine CreateMachine() =>
        new(File.ReadAllBytes(Path.Combine(FindRepositoryRoot(), "roms", "trs80", "model1-level2-v1.4.bin")));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetEmulator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
