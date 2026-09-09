using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;

namespace PetEmulator.Vic20.Tests;

/// <summary>
/// The first correctness gate for this port (see docs/vic20-migration-plan.md step 9) - proves
/// the machine boots real BASIC on real ROMs, not that individual chips behave in isolation.
/// Methodology ported from cpu-vibe-001's Vic20BootTests.Boot_ReachesBasicReady_AndScreenHasText:
/// count non-space screen characters instead of searching for a literal "READY." string (the
/// exact screen-code bytes for "READY." would need a VIC-20 PETSCII-to-screen-code table this
/// port doesn't have yet - counting rendered characters is what that source test already proved
/// sufficient for "did BASIC's startup banner actually print").
/// </summary>
public sealed class Vic20BootTests
{
    [Test]
    [CancelAfter(30_000)]
    public void Boot_ReachesBasicReady_AndScreenHasText()
    {
        var machine = CreateMachine();

        machine.RunUntil(_ => HasScreenText(machine), 2_000_000).Should().BeTrue(
            "BASIC's startup banner should print non-space characters to screen RAM within 2M instructions");
    }

    [Test]
    [CancelAfter(30_000)]
    public void Boot_ConfiguresRealVicColumnsAndRows()
    {
        var machine = CreateMachine();

        machine.RunUntil(_ => HasScreenText(machine), 2_000_000).Should().BeTrue();

        machine.Vic.Columns.Should().BeGreaterThan(0, "KERNAL should configure VIC columns during boot");
        machine.Vic.Rows.Should().BeGreaterThan(0, "KERNAL should configure VIC rows during boot");
    }

    private static bool HasScreenText(Vic20Machine machine)
    {
        var cols = machine.Vic.Columns;
        var rows = machine.Vic.Rows;
        if (cols == 0 || rows == 0)
            return false; // KERNAL hasn't configured the VIC yet

        var screenAddr = machine.Vic.ScreenAddr;
        var nonSpace = 0;
        for (var i = 0; i < cols * rows; i++)
        {
            if (machine.Memory.Read((ushort)(screenAddr + i)) != 0x20) // screen-code space
                nonSpace++;
        }

        return nonSpace > 10;
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
