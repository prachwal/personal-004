using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80PioGroup0Tests
{
    [Test]
    public void DefaultDeviceIsTheZ8420BaselineWithFourRelativePorts()
    {
        var pio = new Z80PioDevice();

        pio.Profile.Revision.Should().Be(Z80PioRevision.Z8420);
        pio.Ports.Should().Equal(0, 1, 2, 3);
        pio.CaptureState().PortA.Mode.Should().Be(Z80PioMode.Input);
        pio.CaptureState().PortB.Mode.Should().Be(Z80PioMode.Input);
    }

    [Test]
    public void TwoInstancesDoNotShareRegistersOrInterruptState()
    {
        var first = new Z80PioDevice(basePort: 0x08);
        var second = new Z80PioDevice(basePort: 0x1C);

        first.WritePort(0x0A, 0x40);
        first.WritePort(0x08, 0xA5);

        second.ReadPort(0x1C).Should().Be(0);
        second.CaptureState().InterruptVector.Should().Be(0);
        first.Ports.Should().Equal(0x08, 0x09, 0x0A, 0x0B);
        second.Ports.Should().Equal(0x1C, 0x1D, 0x1E, 0x1F);
    }

    [Test]
    public void ResetIsDeterministicAfterPartialControlProgramming()
    {
        var pio = new Z80PioDevice();
        pio.WritePort(0x02, 0xFF); // Mode 3, next write is I/O select
        pio.WritePort(0x02, 0x55);
        pio.WritePort(0x02, 0x97); // interrupt control, mask follows

        pio.Reset();
        var state = pio.CaptureState();

        state.PortA.Mode.Should().Be(Z80PioMode.Input);
        state.PortA.DirectionMask.Should().Be(0xFF);
        state.PortA.ControlPhase.Should().Be(Z80PioControlPhase.Ready);
        state.PortA.InterruptEnabled.Should().BeFalse();
        state.PortA.InterruptMask.Should().Be(0);
    }

    [Test]
    public void UnsupportedAndInvalidReadsUseTheProfileContract()
    {
        var pio = new Z80PioDevice(basePort: 0x40);

        pio.ReadPort(0x44).Should().Be(0xFF);
        pio.WritePort(0x42, 0x04); // unsupported RR/control read selector
        pio.ReadPort(0x42).Should().Be(0x00);
    }
}
