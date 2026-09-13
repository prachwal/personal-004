using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80PioExamplesTests
{
    [Test]
    public void Mode0ExampleDrivesEightExternalOutputs()
    {
        var pio = new Z80PioDevice();
        var port = new TestPort();
        pio.AttachPort(Z80PioDevice.PortA, port);
        pio.WritePort(2, 0x0F);
        pio.WritePort(0, 0xA5);

        port.Output.Should().ContainSingle().Which.Should().Be(0xA5);
    }

    [Test]
    public void Mode1ExampleReceivesAStrobedByte()
    {
        var pio = new Z80PioDevice();
        pio.WritePort(3, 0x4F);
        pio.DriveInput(Z80PioDevice.PortB, 0x5A);

        pio.ReadPort(1).Should().Be(0x5A);
    }

    [Test]
    public void Mode2ExampleChangesDirectionAndTransfersBothWays()
    {
        var pio = new Z80PioDevice();
        var port = new TestPort();
        pio.AttachPort(Z80PioDevice.PortA, port);
        pio.WritePort(2, 0x8F);
        pio.SetBidirectionalDirection(Z80PioDevice.PortA, false);
        pio.WritePort(0, 0x33);
        pio.SetBidirectionalDirection(Z80PioDevice.PortA, true);
        pio.DriveInput(Z80PioDevice.PortA, 0xCC);

        port.Output.Should().ContainSingle().Which.Should().Be(0x33);
        pio.ReadPort(0).Should().Be(0xCC);
    }

    [Test]
    public void Mode3ExampleCombinesInputAndOutputBits()
    {
        var pio = new Z80PioDevice();
        pio.WritePort(2, 0xCF);
        pio.WritePort(2, 0xF0); // high nibble input, low nibble output
        pio.WritePort(0, 0x05);
        pio.DriveInput(Z80PioDevice.PortA, 0xA0);

        pio.ReadPort(0).Should().Be(0xA5);
    }

    [Test]
    public void Im2ExampleUsesTwoPioInstancesAndRestoresChainServiceState()
    {
        var first = CreateInputInterruptPio(0x20);
        var second = CreateInputInterruptPio(0x40);
        var chain = new Z80PioInterruptChain(first, second);
        first.DriveInput(Z80PioDevice.PortA, 0x11);
        second.DriveInput(Z80PioDevice.PortA, 0x22);

        chain.TryAcknowledgeInterrupt(out var vector).Should().BeTrue();
        vector.Should().Be(0x20);
        var state = chain.CaptureState();
        chain.NotifyReti();
        chain.RestoreState(state);
        chain.InterruptRequested.Should().BeFalse();
        chain.NotifyReti();
        chain.TryAcknowledgeInterrupt(out vector).Should().BeTrue();
        vector.Should().Be(0x40);
    }

    [Test]
    public void DebugSnapshotExposesParserLatchesLinesAndPendingInterrupt()
    {
        var pio = new Z80PioDevice();
        pio.WritePort(2, 0x22);
        pio.WritePort(2, 0xCF);
        var snapshot = pio.CaptureDebugSnapshot();

        snapshot.PortA.Mode.Should().Be(Z80PioMode.BitControl);
        snapshot.PortA.InterruptVector.Should().Be(0x22);
        snapshot.PortA.ControlPhase.Should().Be(Z80PioControlPhase.Mode3IoSelect);
        snapshot.PortA.StrobeAsserted.Should().BeFalse();
    }

    private static Z80PioDevice CreateInputInterruptPio(byte vector)
    {
        var pio = new Z80PioDevice();
        pio.WritePort(2, vector);
        pio.WritePort(2, 0x4F);
        pio.WritePort(2, 0x87);
        return pio;
    }

    private sealed class TestPort : IZ80PioPort
    {
        public byte InputValue { get; private set; }
        public bool StrobeAsserted { get; private set; }
        public bool Ready { get; private set; } = true;
        public List<byte> Output { get; } = [];
        public event Action<byte>? InputChanged;
        public event Action<bool>? StrobeChanged;
        public event Action<byte>? OutputChanged;
        public event Action<bool>? ReadyChanged;

        public void DriveInput(byte value) { InputValue = value; InputChanged?.Invoke(value); }
        public void SetStrobe(bool asserted) { StrobeAsserted = asserted; StrobeChanged?.Invoke(asserted); }
        public void WriteOutput(byte value) { Output.Add(value); OutputChanged?.Invoke(value); }
        public void SetReady(bool asserted) { Ready = asserted; ReadyChanged?.Invoke(asserted); }
    }
}
