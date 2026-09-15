using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Trs80.Tests;

/// <summary>
/// End-to-end save/reset/load round trip: fill a memory region, "save" it to tape by bit-banging
/// the real cassette port the same way a real CSAVE ROM routine would, reset the machine (which
/// deliberately does not clear RAM - see Trs80Machine.Reset's own doc comment - so this clears the
/// region itself to prove the reload is real, not stale RAM), then "load" the saved tape back by
/// playing it through the same port and decoding the observed EAR pulses, exactly like a real
/// CLOAD loop. This repo's real Level II ROM boot-to-READY isn't verified yet (see
/// docs/trs80/model1-status.md), so this drives the port directly rather than depending on the
/// ROM's actual CSAVE/CLOAD code paths - it proves the cassette subsystem's own encode/decode
/// round trip is correct, independent of that still-open ROM-boot question.
///
/// This is exactly the test that caught a real, previously-untested bug in
/// Trs80CassettePlayer.TryGetRecordedCas: it treated every 0-&gt;1 pulse edge as a separate bit
/// boundary, but a "1" bit's waveform (Trs80CassetteEncoding.OneBit) has two rising edges per
/// cell, not one - so every "1" bit was recorded as two bits. Fixed by grouping edges: a short gap
/// to the next edge means both belong to one "1" bit; a long gap means a lone "0" bit.
/// </summary>
public sealed class Trs80CassetteSaveLoadRoundTripTests
{
    private const byte CassettePort = Trs80MemoryMap.CassettePort;
    private const byte MotorOn = 0x04;

    [Test]
    public void MemoryPattern_SurvivesSaveResetLoad()
    {
        var machine = new Trs80Machine(new byte[Trs80MemoryMap.RomEnd]);
        const ushort address = 0x4000;
        var original = Enumerable.Range(0, 16).Select(i => (byte)(i * 17 + 5)).ToArray();
        for (var i = 0; i < original.Length; i++)
            machine.Memory.Write((ushort)(address + i), original[i]);

        machine.LoadTape([]); // attaches a cassette with nothing in the drive, ready to record
        Encode(machine, original);
        machine.Cassette!.TryGetRecordedCas(out var savedTape).Should().BeTrue();
        savedTape.Should().Equal(original);

        machine.Reset();
        for (var i = 0; i < original.Length; i++)
            machine.Memory.Write((ushort)(address + i), 0); // prove the reload is real, not stale RAM
        machine.Memory.Read(address).Should().Be(0);

        machine.LoadTape(savedTape);
        var loaded = Decode(machine, original.Length);
        for (var i = 0; i < loaded.Length; i++)
            machine.Memory.Write((ushort)(address + i), loaded[i]);

        for (var i = 0; i < original.Length; i++)
            machine.Memory.Read((ushort)(address + i)).Should().Be(original[i]);
    }

    private static void Encode(Trs80Machine machine, byte[] source)
    {
        machine.Bus.WritePort(CassettePort, MotorOn);
        machine.Bus.Tick(Trs80CassetteEncoding.ToTStates(500)); // leader gap before the first bit
        foreach (var value in source)
            for (var bitIndex = 7; bitIndex >= 0; bitIndex--)
            {
                var segments = ((value >> bitIndex) & 1) != 0 ? Trs80CassetteEncoding.OneBit : Trs80CassetteEncoding.ZeroBit;
                foreach (var (durationMicros, high) in segments)
                {
                    machine.Bus.WritePort(CassettePort, (byte)(MotorOn | (high ? 0x01 : 0x00)));
                    machine.Bus.Tick(Trs80CassetteEncoding.ToTStates(durationMicros));
                }
            }

        machine.Bus.WritePort(CassettePort, 0x00);
    }

    private static byte[] Decode(Trs80Machine machine, int byteCount)
    {
        machine.Bus.WritePort(CassettePort, MotorOn);

        var edgePositions = new List<int>();
        var position = 0;
        const int pollStep = 20;
        var budget = byteCount * 8 * 4_000;
        while (!machine.Cassette!.AtEndOfTape && position < budget)
        {
            machine.Bus.Tick(pollStep);
            position += pollStep;
            if ((machine.Bus.ReadPort(CassettePort) & 0x80) != 0)
                edgePositions.Add(position);
        }

        var shortGapThreshold = Trs80CassetteEncoding.ToTStates(500);
        var bits = new List<bool>();
        var edgeIndex = 0;
        while (edgeIndex < edgePositions.Count)
        {
            var hasNext = edgeIndex + 1 < edgePositions.Count;
            var isOneBit = hasNext && edgePositions[edgeIndex + 1] - edgePositions[edgeIndex] <= shortGapThreshold;
            bits.Add(isOneBit);
            edgeIndex += isOneBit ? 2 : 1;
        }

        var data = new byte[bits.Count / 8];
        for (var i = 0; i < data.Length; i++)
            for (var bit = 0; bit < 8; bit++)
                data[i] = (byte)((data[i] << 1) | (bits[i * 8 + bit] ? 1 : 0));
        return data;
    }
}
