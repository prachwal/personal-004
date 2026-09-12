using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Vic20.Keyboard;

namespace PetEmulator.Vic20.Tests;

/// <summary>Layer 2 (see docs/vic20/migration-plan.md step 12): real BASIC direct-mode command
/// typed through the real keyboard matrix on a fully booted machine - not a register poke.</summary>
public sealed class Vic20KeyboardBootTests
{
    [Test]
    [CancelAfter(30_000)]
    public void TypingPrintInDirectMode_ActuallyExecutesTheLine_NotJustEchoesTheTypedText()
    {
        // "PRINT2+2" -> checking for a literal '2' on screen would be a false positive: '2' is
        // typed input, echoed to screen regardless of whether Enter ever actually ran anything
        // (this repo's own first version of this test made exactly that mistake, checking for
        // the literal digit in "PRINT5" - it stayed green through a real bug where Enter was
        // wired to CRSR-DOWN and never executed a single typed line - see
        // docs/vic20/rendering-fixes.md's Enter investigation). '4' cannot appear from echoing
        // the typed characters alone - only real execution proves it.
        var machine = new Vic20Machine(RomsRoot());
        machine.RunUntil(_ => HasScreenText(machine), 2_000_000).Should().BeTrue("must boot first");

        Vic20TextTyper.Type(machine, "PRINT2+2\n", holdInstructions: 8_000, gapInstructions: 8_000);
        machine.Run(200_000);

        ScreenContainsDigitFour(machine).Should().BeTrue("PRINT2+2 typed in direct mode should actually evaluate and print '4'");
    }

    [Test]
    [CancelAfter(30_000)]
    public void LiveQuoteMapping_EchoesVIC20QuoteScreenCode()
    {
        var machine = new Vic20Machine(RomsRoot());
        machine.RunUntil(_ => HasScreenText(machine), 2_000_000).Should().BeTrue("must boot first");
        var map = new Vic20KeyboardMap();

        foreach (var action in map.Translate("Quote", HostKeyEventKind.Press))
            machine.Keyboard.Press(action.Row, action.Column);
        machine.Run(8_000);
        foreach (var action in map.Translate("Quote", HostKeyEventKind.Release))
            machine.Keyboard.Release(action.Row, action.Column);
        machine.Run(8_000);

        ScreenContainsCode(machine, 0x22).Should().BeTrue("Shift+2 must echo the VIC-20 quote screen code");
    }

    private static bool HasScreenText(Vic20Machine machine)
    {
        var cols = machine.Vic.Columns;
        var rows = machine.Vic.Rows;
        if (cols == 0 || rows == 0)
            return false;
        var screenAddr = machine.Vic.ScreenAddr;
        var letters = 0;
        for (var i = 0; i < cols * rows; i++)
        {
            // Screen-code letters are 1-26 - "!= space" alone is a false positive on zeroed RAM
            // (0x00), which a provisional pre-relocation screen address can show - see
            // Vic20BootTests.HasScreenText's identical fix and doc comment.
            var code = machine.Memory.Read((ushort)(screenAddr + i));
            if (code is >= 1 and <= 26)
                letters++;
        }
        return letters > 10;
    }

    private static bool ScreenContainsDigitFour(Vic20Machine machine)
    {
        var cols = machine.Vic.Columns;
        var rows = machine.Vic.Rows;
        var screenAddr = machine.Vic.ScreenAddr;
        for (var i = 0; i < cols * rows; i++)
            if (machine.Memory.Read((ushort)(screenAddr + i)) == 0x34) // screen-code '4' (unshifted 0x20-0x3F mirrors ASCII)
                return true;
        return false;
    }

    private static bool ScreenContainsCode(Vic20Machine machine, byte expected)
    {
        var cols = machine.Vic.Columns;
        var rows = machine.Vic.Rows;
        var screenAddr = machine.Vic.ScreenAddr;
        for (var i = 0; i < cols * rows; i++)
            if (machine.Memory.Read((ushort)(screenAddr + i)) == expected)
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
