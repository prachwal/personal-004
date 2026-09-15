using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.TestSupport;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Vic20.Tests;

/// <summary>Proves <see cref="CbmScreenCode"/> decodes real VIC-20 screen RAM as readable text -
/// upgrading the exact gap Vic20BootTests.Boot_ReachesBasicReady_AndScreenHasText's own doc comment
/// names ("the exact screen-code bytes for 'READY.' would need a VIC-20 PETSCII-to-screen-code table
/// this port doesn't have yet"). Shares the table with PET (PetScreenTextTests) - same screen-code
/// scheme, same character-ROM lineage.</summary>
public sealed class Vic20ScreenTextTests
{
    [Test]
    [CancelAfter(30_000)]
    public void RealBoot_ScreenTextContainsReadyPrompt()
    {
        var machine = CreateMachine();

        machine.RunUntil(_ => ScreenText(machine).Contains("READY."), 2_000_000)
            .Should().BeTrue("BASIC should boot to a 'READY.' prompt");
    }

    [Test]
    [CancelAfter(30_000)]
    public void TypedLine_ExecutesInBasic_VisibleAsRealDecodedText()
    {
        var machine = CreateMachine();
        machine.RunUntil(_ => ScreenText(machine).Contains("READY."), 2_000_000).Should().BeTrue("must boot first");

        Vic20TextTyper.Type(machine, "PRINT2+2\n", holdInstructions: 8_000, gapInstructions: 8_000);
        machine.Run(200_000);

        ScreenText(machine).Should().Contain("4", "PRINT2+2 typed through the real keyboard matrix should actually evaluate and print '4'");
    }

    private static string ScreenText(Vic20Machine machine)
    {
        var columns = machine.Vic.Columns;
        var rows = machine.Vic.Rows;
        if (columns == 0 || rows == 0) return "";
        var screenAddr = machine.Vic.ScreenAddr;
        var bytes = new byte[columns * rows];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = machine.Memory.Read((ushort)(screenAddr + i));
        return CbmScreenCode.ToText(bytes, columns, rows);
    }

    private static Vic20Machine CreateMachine() => new(RomsRoot());

    private static string RomsRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms", "vic20");
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate roms/vic20/ walking up from the test binary directory.");
    }
}
