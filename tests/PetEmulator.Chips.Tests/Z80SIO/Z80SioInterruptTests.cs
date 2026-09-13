using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80SioInterruptTests
{
    [Test]
    public void InterruptsAreMaskedUntilEiEnablesTheSource()
    {
        var sio = new Z80Sio();
        EnableExternalStatusInterrupt(sio, Z80Sio.ChannelA);
        sio.SetCts(Z80Sio.ChannelA, false);

        sio.InterruptRequested.Should().BeTrue();
        sio.InterruptInputEnabled = false;
        sio.InterruptRequested.Should().BeFalse();
        sio.TryAcknowledgeInterrupt(out _).Should().BeFalse();

        sio.InterruptInputEnabled = true;
        sio.TryAcknowledgeInterrupt(out _).Should().BeTrue();
    }

    [Test]
    public void HigherPrioritySourceBlocksLowerSourceUntilReti()
    {
        var sio = new Z80Sio();
        EnableExternalStatusInterrupt(sio, Z80Sio.ChannelA);
        EnableExternalStatusInterrupt(sio, Z80Sio.ChannelB);
        sio.SetCts(Z80Sio.ChannelA, false);
        sio.SetCts(Z80Sio.ChannelB, false);

        sio.TryAcknowledgeInterrupt(out var firstVector).Should().BeTrue();
        firstVector.Should().Be(0x08); // channel A Ext/Status has priority
        sio.InterruptInService.Should().BeTrue();
        sio.InterruptRequested.Should().BeFalse();
        sio.TryAcknowledgeInterrupt(out _).Should().BeFalse();

        sio.NotifyReti();
        sio.InterruptOutputEnabled.Should().BeFalse(); // channel B remains pending
        sio.TryAcknowledgeInterrupt(out var secondVector).Should().BeTrue();
        secondVector.Should().Be(0x00); // channel B Ext/Status, base vector
        sio.CompleteInterrupt();
    }

    [Test]
    public void SimultaneousSourcesUseReceiveThenTransmitThenExternalPriority()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableReceiverInterrupt(sio, Z80Sio.ChannelA);
        EnableTransmitterInterrupt(sio, Z80Sio.ChannelA);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x41);
        sio.Tick(10_000);

        sio.TryAcknowledgeInterrupt(out _).Should().BeTrue();
        sio.TryAcknowledgeInterrupt(out _).Should().BeFalse();
        sio.NotifyReti();
        sio.ReadPort(0x00).Should().Be(0x41);

        sio.TryAcknowledgeInterrupt(out _).Should().BeTrue();
        sio.NotifyReti();
    }

    [Test]
    public void Im2VectorIncludesChannelAndSourceStatus()
    {
        var sio = new Z80Sio(revision: Z80SioRevision.Z8440);
        SelectRegister(sio, 0x02, 2);
        sio.WritePort(0x02, 0xA0);
        EnableExternalStatusInterrupt(sio, Z80Sio.ChannelA);
        EnableExternalStatusInterrupt(sio, Z80Sio.ChannelB);
        sio.SetCts(Z80Sio.ChannelA, false);

        sio.TryAcknowledgeInterrupt(out var vector).Should().BeTrue();
        vector.Should().Be(0xA8);
        sio.NotifyReti();
    }

    [Test]
    public void DaisyChainPassesIeoOnlyWhenSourceIsIdle()
    {
        var first = new Z80Sio();
        var second = new Z80Sio();
        EnableExternalStatusInterrupt(first, Z80Sio.ChannelA);
        EnableExternalStatusInterrupt(second, Z80Sio.ChannelA);
        first.SetCts(Z80Sio.ChannelA, false);
        second.SetCts(Z80Sio.ChannelA, false);

        second.InterruptInputEnabled = first.InterruptOutputEnabled;
        second.TryAcknowledgeInterrupt(out _).Should().BeFalse();
        first.TryAcknowledgeInterrupt(out _).Should().BeTrue();
        first.NotifyReti();
        first.InterruptOutputEnabled.Should().BeTrue();
        second.InterruptInputEnabled = first.InterruptOutputEnabled;
        second.TryAcknowledgeInterrupt(out _).Should().BeTrue();
        second.NotifyReti();
    }

    private static void EnableExternalStatusInterrupt(Z80Sio sio, int channel)
    {
        var port = (ushort)(channel + 2);
        SelectRegister(sio, port, 1);
        sio.WritePort(port, 0x05); // Ext/Status + status affects vector
    }

    private static void EnableReceiverInterrupt(Z80Sio sio, int channel)
    {
        var port = (ushort)(channel + 2);
        SelectRegister(sio, port, 3);
        sio.WritePort(port, 0xC1);
        SelectRegister(sio, port, 1);
        sio.WritePort(port, 0x18); // Rx interrupt on all characters
    }

    private static void EnableTransmitterInterrupt(Z80Sio sio, int channel)
    {
        var port = (ushort)(channel + 2);
        SelectRegister(sio, port, 5);
        sio.WritePort(port, 0x08);
        SelectRegister(sio, port, 1);
        sio.WritePort(port, 0x1A); // Rx + Tx interrupt on all received characters
        sio.WritePort(0, 0x55);
    }

    private static void SelectRegister(Z80Sio sio, ushort port, int register) =>
        sio.WritePort(port, (byte)register);
}
