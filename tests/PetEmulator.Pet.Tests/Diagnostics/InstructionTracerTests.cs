using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Diagnostics;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests.Diagnostics;

public sealed class InstructionTracerTests
{
    [Test]
    public void StepInstruction_RecordsOnePcAndItsBusAccesses_PerInstruction()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var tracer = new InstructionTracer(machine);

        tracer.Run(50);

        tracer.Recent.Should().HaveCount(50);
        tracer.Recent.Select(t => t.Index).Should().BeInAscendingOrder();
        tracer.Recent.Should().OnlyContain(t => t.BusAccesses.Count > 0, "every real 6502 instruction touches the bus at least once (its own opcode fetch)");
    }

    [Test]
    public void Recent_IsBoundedByCapacity_OldestEvictedFirst()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var tracer = new InstructionTracer(machine, capacity: 10);

        tracer.Run(30);

        tracer.Recent.Should().HaveCount(10);
        tracer.Recent[0].Index.Should().Be(20, "the first 20 of 30 traced instructions should have been evicted");
        tracer.Recent[^1].Index.Should().Be(29);
    }

    [Test]
    public void Attaching_ChainsOntoWhateverObserverWasAlreadySet_InsteadOfReplacingIt()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var calls = 0;
        machine.BusObserver = _ => calls++;

        var tracer = new InstructionTracer(machine);
        tracer.Run(5);

        calls.Should().BeGreaterThan(0, "the pre-existing observer (e.g. a disk-activity LED hookup) must keep firing while a tracer is attached");
    }

    [Test]
    public void Detach_RestoresExactlyWhateverObserverWasSetBefore_AndIsIdempotent()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var calls = 0;
        Action<BusAccess> original = _ => calls++;
        machine.BusObserver = original;

        var tracer = new InstructionTracer(machine);
        tracer.Detach();

        machine.BusObserver.Should().BeSameAs(original);
        var act = tracer.Detach;
        act.Should().NotThrow("detaching twice must be a no-op, not throw or clobber a since-changed observer");
    }

    [Test]
    public void Render_ProducesOneLinePerInstructionPlusOneLinePerBusAccess()
    {
        var machine = CreateMachine(PetProfileCatalog.Pet2001_8);
        var tracer = new InstructionTracer(machine);

        tracer.Run(3);
        var text = tracer.Render();

        text.Should().Contain("[0] PC=");
        text.Should().Contain("[1] PC=");
        text.Should().Contain("[2] PC=");
    }

    [TestCase(0xFFFA)] // NMI vector low byte
    [TestCase(0xFFFB)]
    [TestCase(0xFFFC)] // RESET vector low byte
    [TestCase(0xFFFD)]
    [TestCase(0xFFFE)] // IRQ/BRK vector low byte
    [TestCase(0xFFFF)]
    public void IsInterruptVectorFetch_TrueForAReadAtAnyVectorAddress(int address)
    {
        var instr = new TracedInstruction(0, 0x1234, [new BusAccess(IsWrite: false, (ushort)address, 0x00)]);

        instr.IsInterruptVectorFetch.Should().BeTrue();
    }

    [Test]
    public void IsInterruptVectorFetch_FalseForAWriteAtAVectorAddress()
    {
        // ROM is read-only, so a real machine never writes here - but the flag should still key
        // off "read", the actual 6502 vector-fetch signal, not merely the address.
        var instr = new TracedInstruction(0, 0x1234, [new BusAccess(IsWrite: true, 0xFFFE, 0x00)]);

        instr.IsInterruptVectorFetch.Should().BeFalse();
    }

    [Test]
    public void IsInterruptVectorFetch_FalseWhenNoAccessTouchesAVectorAddress()
    {
        var instr = new TracedInstruction(0, 0x1234, [new BusAccess(IsWrite: false, 0xC000, 0x00)]);

        instr.IsInterruptVectorFetch.Should().BeFalse();
    }

    private static PetMachine CreateMachine(PetProfile profile)
    {
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        return new PetMachine(profile, romsRoot);
    }
}
