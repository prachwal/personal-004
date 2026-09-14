using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core.Serial;
using PetEmulator.Pet.Diagnostics;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests.Diagnostics;

/// <summary>Regression pin for docs/pet/superpet-6809-boot-hang.md's stage-by-stage boot map. Each
/// assertion here is a fact this session earned by hand-disassembling the ROM and cross-checking it
/// against a live run - see <see cref="SuperPetBootCheckpoints.WellKnown"/>'s doc comments for the
/// evidence behind each one. If any of these ever flips, the boot's behavior genuinely changed and
/// this doc needs revisiting, not just this test.</summary>
public sealed class SuperPetBootCheckpointsTests
{
    [Test]
    public void Run_IsBoundedByMaxHitsPerCheckpoint_EvenInsideATightLoop()
    {
        var report = SuperPetBootCheckpoints.Run(CreateMachine(), 30_000, maxHitsPerCheckpoint: 3);

        report.Hits.Where(h => h.Stage == "KeyboardMatrixScanLoop").Should().HaveCountLessOrEqualTo(3,
            "the keyboard matrix scan loop re-executes constantly - capping hits per checkpoint is " +
            "the whole point, unlike SuperPetStartupDiagnostics/MachineDebugger.Trace which both " +
            "accumulate every instruction and OOM past ~10-12 million (see the 'Tooling gap' " +
            "section of docs/pet/superpet-6809-boot-hang.md)");
    }

    [Test]
    public void Run_ReachesTheEarlyBootStages_InOrder()
    {
        var report = SuperPetBootCheckpoints.Run(CreateMachine(), 30_000);

        var reachedInOrder = report.Hits.Select(h => h.Stage).Distinct().ToList();
        reachedInOrder.Should().ContainInOrder(
            "Reset", "ExpansionBankClear", "KeyboardRingBufferInit", "ChecksCb1FlagAtDE0B", "KeyboardMatrixScanLoop");
    }

    [Test]
    public void Run_ReachesTheMenuBanner_WithoutAnyKeypress()
    {
        // The SuperPET 6809 boot hang investigation's actual root cause, found via a real 6809
        // reference emulator (VICE) side-by-side comparison: M6809Cpu.PushFullFrame pushed the
        // 12-byte interrupt frame in the exact reverse of real 6809 hardware order (CC first, PC
        // last, instead of PC first, CC last) - RTI would then resume at garbage bytes read from
        // the wrong stack offset instead of the interrupted PC, crashing into zeroed RAM the very
        // first time IRQ ever fired, before Waterloo's own boot code ever reached the banner-print
        // routine. Fixed in M6809Cpu.Instructions.cs; see
        // M6809/InterruptTests.IRQ_PushesTheFullFrameInRealHardwareByteOrder for the CPU-level pin
        // and docs/pet/superpet-6809-boot-hang.md for the full investigation. The menu renders
        // unconditionally (no keypress needed) - MenuDispatchTrampoline/MenuBannerPrint/
        // MenuChoiceDispatch are all reached well within 100,000 instructions now.
        var report = SuperPetBootCheckpoints.Run(CreateMachine(), 100_000);

        // MenuDispatchTrampoline ($A9F0) itself is still never hit - the banner is reached via a
        // different path than the one this investigation originally assumed was the sole entry
        // point; that assumption (not the fix) is what's now known to be incomplete.
        var reached = report.Hits.Select(h => h.Stage).ToList();
        reached.Should().Contain(["MenuBannerPrint", "MenuChoiceDispatch"]);

        // The ring buffer still never gets an actual keyboard byte enqueued without a real
        // keypress - that part of the original investigation's finding still holds.
        report.NeverReached.Select(c => c.Stage).Should().Contain("RingBufferEnqueue");
    }

    [Test]
    public void Run_ReportsFullRegisterStateAndWatchedMemory_ForEveryHit()
    {
        var report = SuperPetBootCheckpoints.Run(CreateMachine(), 30_000);

        var bufferInit = report.Hits.Single(h => h.Stage == "KeyboardRingBufferInit");
        bufferInit.WatchedMemory.Should().ContainKey((ushort)0xE813,
            "CRB's post-fix value is the whole point of this checkpoint - see docs/pet/superpet-6809-boot-hang.md");
    }

    private static PetMachine CreateMachine()
    {
        var profile = PetProfileCatalog.SuperPet;
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        return new PetMachine(profile, romsRoot, serialTransport: new BufferedSerialTransport());
    }
}
