using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80SioBusAdapterTests
{
    [Test]
    public void MapsCdBaSignalsToDataAndControlPorts()
    {
        var sio = new Z80Sio();
        var bus = new Z80SioBusAdapter(sio);
        var controlBWrite = new Z80SioBusSignals(true, true, false, false, true, true);
        var controlBRead = controlBWrite with { Read = true };

        bus.TryWrite(controlBWrite, 0x09).Should().BeTrue();
        bus.TryWrite(controlBWrite, 0x1A).Should().BeTrue();
        bus.TryWrite(controlBWrite, 0x01).Should().BeTrue();
        bus.TryRead(controlBRead, out var value).Should().BeTrue();
        value.Should().Be(Z80Sio.Rr1AllSent);
    }

    [Test]
    public void RejectsMemoryCyclesAndM1NormalAccessButAcknowledgesInterruptCycle()
    {
        var sio = new Z80Sio();
        var bus = new Z80SioBusAdapter(sio);
        var memoryCycle = new Z80SioBusSignals(true, true, true, true, false, false);
        bus.TryRead(memoryCycle, out _).Should().BeFalse();
        bus.TryWrite(memoryCycle with { Read = false }, 0x00).Should().BeFalse();

        SelectRegister(sio, 0x02, 1);
        sio.WritePort(0x02, 0x01);
        sio.SetCts(Z80Sio.ChannelA, false);
        bus.TryAcknowledgeInterrupt(memoryCycle, out _).Should().BeTrue();
        sio.CompleteInterrupt();
    }

    [Test]
    public void ResetSignalResetsChipAndProfilesExposeUnsupportedRegisters()
    {
        var sio = new Z80Sio(revision: Z80SioRevision.Sio0);
        var bus = new Z80SioBusAdapter(sio);
        sio.SetRxBreak(Z80Sio.ChannelA, true);
        bus.Reset(true);
        sio.GetChannelState(Z80Sio.ChannelA).BreakDetected.Should().BeFalse();

        SelectRegister(sio, 0x02, 3);
        sio.ReadPort(0x02).Should().Be(sio.Profile.UnsupportedRegisterReadValue);
        sio.InterruptOutputEnabled.Should().BeFalse();
        sio.ReadPort(0xFF).Should().Be(sio.Profile.InvalidReadValue);
    }

    [Test]
    public void Wr6AndWr7ProgramSyncCharactersAndBusM1DoesNotReadData()
    {
        var sio = new Z80Sio();
        SelectRegister(sio, 0x02, 6);
        sio.WritePort(0x02, 0x12);
        SelectRegister(sio, 0x02, 7);
        sio.WritePort(0x02, 0x34);
        sio.GetChannelState(Z80Sio.ChannelA).SyncWord.Should().Be(0x1234);
        sio.ConfigureMode(Z80Sio.ChannelA, Z80SioFrameMode.Sync16);
        SelectRegister(sio, 0x02, 3);
        sio.WritePort(0x02, 0xC1); // Rx enable, 8 data bits
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x12);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x34);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0xA5);
        sio.Tick(100_000);
        sio.ReadPort(0).Should().Be(0xA5);

        var bus = new Z80SioBusAdapter(sio);
        bus.TryRead(new Z80SioBusSignals(true, true, true, true, false, false), out _).Should().BeFalse();
    }

    private static void SelectRegister(Z80Sio sio, ushort port, int register) =>
        sio.WritePort(port, (byte)register);
}
