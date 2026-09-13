using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80PioInterruptTests
{
    [Test]
    public void PortAHasPriorityOverPortBAndRetiReleasesTheNextSource()
    {
        var pio = CreateInterruptingPio(0x20, 0x40);
        var chain = new Z80PioInterruptChain(pio);
        pio.DriveInput(Z80PioDevice.PortA, 0x11);
        pio.DriveInput(Z80PioDevice.PortB, 0x22);

        chain.TryAcknowledgeInterrupt(out var first).Should().BeTrue();
        first.Should().Be(0x20);
        chain.InterruptRequested.Should().BeFalse();

        chain.NotifyReti();
        chain.TryAcknowledgeInterrupt(out var second).Should().BeTrue();
        second.Should().Be(0x40);
    }

    [Test]
    public void DaisyChainPassesIeiOnlyToTheFirstPendingDevice()
    {
        var first = CreateInterruptingPio(0x20);
        var second = CreateInterruptingPio(0x40);
        var chain = new Z80PioInterruptChain(first, second);
        first.DriveInput(Z80PioDevice.PortA, 0x11);
        second.DriveInput(Z80PioDevice.PortA, 0x22);

        chain.TryAcknowledgeInterrupt(out var vector).Should().BeTrue();
        vector.Should().Be(0x20);
        second.InterruptInputEnabled.Should().BeFalse();
        chain.TryAcknowledgeInterrupt(out _).Should().BeFalse();

        chain.NotifyReti();
        chain.TryAcknowledgeInterrupt(out vector).Should().BeTrue();
        vector.Should().Be(0x40);
    }

    [Test]
    public void VectorIsAlignedForIm2WordFetch()
    {
        var pio = new Z80PioDevice();
        pio.WritePort(2, 0x22);
        pio.WritePort(2, 0x4F);
        pio.WritePort(2, 0x87);
        pio.DriveInput(Z80PioDevice.PortA, 0xAA);

        pio.TryAcknowledgeInterrupt(out var vector).Should().BeTrue();
        vector.Should().Be(0x22);
    }

    [Test]
    public void ResetCancelsInServiceAndPendingInterrupts()
    {
        var pio = CreateInterruptingPio(0x20);
        var chain = new Z80PioInterruptChain(pio);
        pio.DriveInput(Z80PioDevice.PortA, 0x11);
        chain.TryAcknowledgeInterrupt(out _).Should().BeTrue();

        pio.Reset();
        chain.NotifyReti();
        chain.InterruptRequested.Should().BeFalse();
        chain.TryAcknowledgeInterrupt(out _).Should().BeFalse();
    }

    private static Z80PioDevice CreateInterruptingPio(params byte[] vectors)
    {
        var pio = new Z80PioDevice();
        for (var port = 0; port < vectors.Length; port++)
        {
            var control = (ushort)(port == Z80PioDevice.PortA ? 2 : 3);
            var data = (ushort)port;
            pio.WritePort(control, vectors[port]);
            pio.WritePort(control, 0x4F); // Mode 1 input
            pio.WritePort(control, 0x87); // interrupt enable
            pio.WritePort(data, 0x00);
        }
        return pio;
    }
}
