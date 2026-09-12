using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Vic20.Cartridge.Abstractions;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class MultiCartridgeTests
{
    [Test]
    public void GroupsIndependentCartridgesAndRoutesThemThroughTheExistingBusRegistry()
    {
        var first = new Vic20Cartridge([0x12], 0x2000);
        var second = new Vic20Cartridge([0x34], 0x4000);
        var multi = new MultiCartridge([first, second]);
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));

        machine.AttachExpansionDevice(multi);

        multi.Devices.Should().Equal(first, second);
        multi.Resources.Should().HaveCount(2);
        machine.Memory.Read(0x2000).Should().Be(0x12);
        machine.Memory.Read(0x4000).Should().Be(0x34);
        machine.ExpansionDevices.Should().ContainSingle().Which.Should().BeSameAs(multi);
    }

    [Test]
    public void RejectsConflictingResourcesBeforeTheGroupCanBeMounted()
    {
        var first = new Vic20Cartridge([0x12], 0xA000);
        var second = new Vic20Cartridge([0x34], 0xA000);

        var act = () => new MultiCartridge([first, second]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ROM*overlap*$A000*");
    }

    [Test]
    public void ResetTickAndIrqAreForwardedToEveryMember()
    {
        var first = new TestDevice(0x9800, assertsIrq: true);
        var second = new TestDevice(0x9801, assertsIrq: false);
        var multi = new MultiCartridge([first, second]);

        multi.Reset();
        multi.Tick(37);

        multi.Irq.Should().BeTrue();
        first.ResetCount.Should().Be(1);
        second.ResetCount.Should().Be(1);
        first.TickCount.Should().Be(37);
        second.TickCount.Should().Be(37);
    }

    [Test]
    public void RejectsEmptyGroups()
    {
        var act = () => new MultiCartridge([]);

        act.Should().Throw<ArgumentException>();
    }

    private sealed class TestDevice(ushort address, bool assertsIrq) : IVic20ExpansionDevice
    {
        public IReadOnlyList<Vic20CartridgeResource> Resources { get; } =
        [new("register", address, 1, Vic20CartridgeResourceKind.Io, Vic20CartridgeResourceAccess.ReadWrite)];

        public bool Irq { get; } = assertsIrq;
        public int ResetCount { get; private set; }
        public ulong TickCount { get; private set; }

        public bool TryRead(ushort requestedAddress, out byte value)
        {
            value = requestedAddress == Resources[0].StartAddress ? (byte)0xA5 : (byte)0;
            return requestedAddress == Resources[0].StartAddress;
        }

        public bool TryWrite(ushort requestedAddress, byte value) =>
            requestedAddress == Resources[0].StartAddress;

        public void Reset() => ResetCount++;
        public void Tick(ulong cycles) => TickCount += cycles;
    }
}
