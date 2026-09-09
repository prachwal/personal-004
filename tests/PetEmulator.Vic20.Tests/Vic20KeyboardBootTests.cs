using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Vic20.Tests;

/// <summary>Layer 2 (see docs/vic20-migration-plan.md step 12): real BASIC direct-mode command
/// typed through the real keyboard matrix on a fully booted machine - not a register poke.</summary>
public sealed class Vic20KeyboardBootTests
{
    [Test]
    [CancelAfter(30_000)]
    public void TypingPrintInDirectMode_PrintsTheDigitToScreen()
    {
        var machine = new Vic20Machine(RomsRoot());
        machine.RunUntil(_ => HasScreenText(machine), 2_000_000).Should().BeTrue("must boot first");

        Vic20TextTyper.Type(machine, "PRINT5\n", holdInstructions: 8_000, gapInstructions: 8_000);
        machine.Run(200_000);

        ScreenContainsDigitFive(machine).Should().BeTrue("PRINT5 typed in direct mode should evaluate and print '5'");
    }

    private static bool HasScreenText(Vic20Machine machine)
    {
        var cols = machine.Vic.Columns;
        var rows = machine.Vic.Rows;
        if (cols == 0 || rows == 0)
            return false;
        var screenAddr = machine.Vic.ScreenAddr;
        var nonSpace = 0;
        for (var i = 0; i < cols * rows; i++)
            if (machine.Memory.Read((ushort)(screenAddr + i)) != 0x20)
                nonSpace++;
        return nonSpace > 10;
    }

    private static bool ScreenContainsDigitFive(Vic20Machine machine)
    {
        var cols = machine.Vic.Columns;
        var rows = machine.Vic.Rows;
        var screenAddr = machine.Vic.ScreenAddr;
        for (var i = 0; i < cols * rows; i++)
            if (machine.Memory.Read((ushort)(screenAddr + i)) == 0x35) // screen-code '5' (unshifted 0x20-0x3F mirrors ASCII)
                return true;
        return false;
    }

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
