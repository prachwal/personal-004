using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Vic20.Cartridge.Abstractions;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20ExpansionDeviceTests
{
    [Test]
    public void Machine_AttachesNonCartridgeDevice_AndHonorsOptionalAccessDirections()
    {
        var device = new TestExpansionDevice(
            new Vic20CartridgeResource("read-only register", 0x9800, 1,
                Vic20CartridgeResourceKind.Io, Vic20CartridgeResourceAccess.Read),
            new Vic20CartridgeResource("write-only register", 0x9801, 1,
                Vic20CartridgeResourceKind.Io, Vic20CartridgeResourceAccess.Write));
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));

        machine.AttachExpansionDevice(device);

        machine.ExpansionDevices.Should().ContainSingle().Which.Should().BeSameAs(device);
        machine.Memory.Read(0x9800).Should().Be(0xA5);
        machine.Memory.Read(0x9801).Should().Be(0xFF);
        machine.Memory.Write(0x9800, 0x12);
        machine.Memory.Write(0x9801, 0x34);
        device.ReadOnlyValue.Should().Be(0xA5);
        device.WriteOnlyValue.Should().Be(0x34);
    }

    [Test]
    public void Machine_ResetsAndTicksNonCartridgeDevice()
    {
        var device = new TestExpansionDevice(
            new Vic20CartridgeResource("register", 0x9800, 1,
                Vic20CartridgeResourceKind.Io, Vic20CartridgeResourceAccess.ReadWrite));
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));

        machine.AttachExpansionDevice(device);
        machine.Reset();
        machine.StepInstruction();

        device.ResetCount.Should().Be(1);
        device.TickCount.Should().BeGreaterThan(0);
    }

    [Test]
    public void Machine_RejectsNonCartridgeDeviceWhenItsResourcesConflict()
    {
        var device = new TestExpansionDevice(
            new Vic20CartridgeResource("conflicting ROM", 0xA000, 1,
                Vic20CartridgeResourceKind.Rom, Vic20CartridgeResourceAccess.Read));
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
        machine.AttachExpansionDevice(new TestExpansionDevice(
            new Vic20CartridgeResource("existing ROM", 0xA000, 1,
                Vic20CartridgeResourceKind.Rom, Vic20CartridgeResourceAccess.Read)));

        Action action = () => machine.AttachExpansionDevice(device);

        action.Should().Throw<InvalidOperationException>().WithMessage("*overlap*$A000*");
        machine.ExpansionDevices.Should().ContainSingle();
    }

    private sealed class TestExpansionDevice(params Vic20CartridgeResource[] resources)
        : IVic20ExpansionDevice
    {
        public IReadOnlyList<Vic20CartridgeResource> Resources { get; } = resources;

        public byte ReadOnlyValue { get; private set; } = 0xA5;

        public byte WriteOnlyValue { get; private set; }

        public int ResetCount { get; private set; }

        public ulong TickCount { get; private set; }

        public bool TryRead(ushort address, out byte value)
        {
            if (address == 0x9800 && Resources[0].Access.HasFlag(Vic20CartridgeResourceAccess.Read))
            {
                value = ReadOnlyValue;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryWrite(ushort address, byte value)
        {
            if (address == 0x9801 && Resources.Count > 1
                && Resources[1].Access.HasFlag(Vic20CartridgeResourceAccess.Write))
            {
                WriteOnlyValue = value;
                return true;
            }

            return false;
        }

        public void Reset() => ResetCount++;

        public void Tick(ulong cycles) => TickCount += cycles;
    }
}
