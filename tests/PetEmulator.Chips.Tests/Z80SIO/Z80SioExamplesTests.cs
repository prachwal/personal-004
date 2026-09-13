using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80SioExamplesTests
{
    [Test]
    public void PollingExampleMovesCharactersOnBothChannels()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        EnableReceiver(sio, Z80Sio.ChannelB);
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x41);
        sio.EnqueueRxByte(Z80Sio.ChannelB, 0x42);

        sio.Tick(100);

        sio.ReadPort(0x00).Should().Be(0x41);
        sio.ReadPort(0x01).Should().Be(0x42);
    }

    [Test]
    public void AsyncEightNOneWithX16AndCtsRtsExampleTransmitsOnlyWhenClear()
    {
        var sio = new Z80Sio(clockFrequencyHz: 1_000_000) { BaudRate = 1_000 };
        EnableReceiver(sio, Z80Sio.ChannelA);
        SelectRegister(sio, 0x02, 4);
        sio.WritePort(0x02, 0x44); // async x16, 1 stop bit, 8 data bits
        SelectRegister(sio, 0x02, 5);
        sio.WritePort(0x02, 0x8A); // Tx enable, RTS and DTR
        var transmitted = new List<byte>();
        sio.Transmitted += (_, value) => transmitted.Add(value);

        sio.SetCts(Z80Sio.ChannelA, false);
        sio.WritePort(0x00, 0x55);
        sio.Tick(100_000);
        transmitted.Should().BeEmpty();

        sio.SetCts(Z80Sio.ChannelA, true);
        sio.WritePort(0x00, 0x55);
        sio.Tick(100_000);
        transmitted.Should().ContainSingle().Which.Should().Be(0x55);
    }

    [Test]
    public void SdlcExampleTransmitsAndDecodesAFrame()
    {
        var payload = new byte[] { 0x01, 0x7E, 0x00 };
        var bits = Z80SioSdlcCodec.EncodeFrame(payload);

        Z80SioSdlcCodec.DecodeFrame(bits).Should().BeEquivalentTo(
            new Z80SioSdlcDecodeResult(true, payload, true, false, true));
    }

    [Test]
    public void Im2ExampleAcknowledgesVectorThenReadsReceivedCharacter()
    {
        var sio = new Z80Sio { BaudRate = 1_000_000 };
        sio.WritePort(0x02, 0x02);
        sio.WritePort(0x02, 0xA0); // IM2 base vector
        EnableReceiver(sio, Z80Sio.ChannelA);
        sio.WritePort(0x02, 0x01);
        sio.WritePort(0x02, 0x18); // Rx interrupt on all characters
        sio.EnqueueRxByte(Z80Sio.ChannelA, 0x5A);
        sio.Tick(100);

        sio.TryAcknowledgeInterrupt(out var vector).Should().BeTrue();
        vector.Should().Be(0xA0);
        sio.ReadPort(0x00).Should().Be(0x5A);
    }

    private static void EnableReceiver(Z80Sio sio, int channel)
    {
        var control = (ushort)(channel == Z80Sio.ChannelA ? 0x02 : 0x03);
        sio.WritePort(control, 0x03);
        sio.WritePort(control, 0xC1);
    }

    private static void SelectRegister(Z80Sio sio, ushort control, byte register)
    {
        sio.WritePort(control, register);
    }
}
