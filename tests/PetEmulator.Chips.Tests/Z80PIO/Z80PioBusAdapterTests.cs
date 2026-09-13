using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80PioBusAdapterTests
{
    [Test]
    public void MapsDataAndControlForBothPortsOnlyDuringIoCycle()
    {
        var pio = new Z80PioDevice();
        var adapter = new Z80PioBusAdapter(pio);
        pio.AttachPort(Z80PioDevice.PortA, new ReadyPort());
        var write = new Z80PioBusSignals(true, true, false, false, false, false);
        var controlA = write with { ControlSelect = true };

        adapter.TryWrite(controlA, 0x0F).Should().BeTrue();
        adapter.TryWrite(write, 0x5A).Should().BeTrue();
        adapter.TryRead(write with { Read = true }, out var value).Should().BeTrue();

        value.Should().Be(0x5A);
        adapter.TryWrite(write with { M1 = true }, 0xA5).Should().BeFalse();
        adapter.TryRead(write with { Read = true, M1 = true }, out _).Should().BeFalse();
    }

    [Test]
    public void PortBAndControlSelectUseTheZ80PioFourPortLayout()
    {
        var pio = new Z80PioDevice();
        var adapter = new Z80PioBusAdapter(pio);
        var dataB = new Z80PioBusSignals(true, true, false, false, false, true);
        var controlB = dataB with { ControlSelect = true };

        adapter.TryWrite(controlB, 0x0F).Should().BeTrue();
        adapter.TryWrite(dataB, 0xC3).Should().BeTrue();
        adapter.TryRead(dataB with { Read = true }, out var value).Should().BeTrue();

        value.Should().Be(0xC3);
    }

    [Test]
    public void InterruptAcknowledgeRequiresM1AndReleasesThroughReti()
    {
        var pio = new Z80PioDevice();
        var adapter = new Z80PioBusAdapter(pio);
        pio.AttachPort(Z80PioDevice.PortA, new ReadyPort());
        var control = new Z80PioBusSignals(true, true, false, false, true, false);

        adapter.TryWrite(control, 0x0F).Should().BeTrue();
        adapter.TryWrite(control, 0x87).Should().BeTrue();
        adapter.TryWrite(control with { ControlSelect = false }, 0x11).Should().BeTrue();
        pio.InterruptRequested.Should().BeTrue();

        adapter.TryAcknowledgeInterrupt(control with { M1 = false }, out _).Should().BeFalse();
        adapter.TryAcknowledgeInterrupt(control with { M1 = true }, out _).Should().BeTrue();
        pio.InterruptInService.Should().BeTrue();
        pio.NotifyReti();
        pio.InterruptInService.Should().BeFalse();
    }

    [Test]
    public void ResetLineResetsOnlyWhenAsserted()
    {
        var pio = new Z80PioDevice();
        var adapter = new Z80PioBusAdapter(pio);
        pio.WritePort(2, 0x0F);
        pio.WritePort(0, 0x66);

        adapter.Reset(false);
        pio.ReadPort(0).Should().Be(0x66);
        adapter.Reset(true);
        pio.ReadPort(0).Should().Be(0x00);
    }

    private sealed class ReadyPort : IZ80PioPort
    {
        public byte InputValue => 0;
        public bool StrobeAsserted => false;
        public bool Ready => true;
        public event Action<byte>? InputChanged;
        public event Action<bool>? StrobeChanged;
        public event Action<byte>? OutputChanged;
        public event Action<bool>? ReadyChanged;

        public void DriveInput(byte value) => InputChanged?.Invoke(value);
        public void SetStrobe(bool asserted) => StrobeChanged?.Invoke(asserted);
        public void WriteOutput(byte value) => OutputChanged?.Invoke(value);
        public void SetReady(bool asserted) => ReadyChanged?.Invoke(asserted);
    }
}
