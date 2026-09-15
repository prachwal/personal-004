using NUnit.Framework;

namespace PetEmulator.Cpc464.Tests;

/// <summary>End-to-end save/reset/load round trip for the CPC464 cassette, mirroring
/// Trs80CassetteSaveLoadRoundTripTests: fill a memory region, "save" it to tape by bit-banging the
/// real PPI ports (motor on Port C bit 4, write data on Port C bit 5) the same way a real CSAVE
/// routine would, reset the machine (which does not clear RAM by itself - the test clears the
/// region afterward to prove the reload is real, not stale RAM), then "load" the saved tape back by
/// polling the same read bit (Port B bit 7) and decoding the observed pulse widths. This machine
/// doesn't run real Amstrad CSAVE/CLOAD firmware yet, so this drives the PPI ports directly rather
/// than depending on that ROM code path - it proves the cassette subsystem's own encode/decode
/// round trip is correct, independent of that still-open question.</summary>
public sealed class Cpc464CassetteSaveLoadRoundTripTests
{
    private const ushort ControlPort = 0xF700;
    private const ushort PortCPort = 0xF600;
    private const ushort PortBPort = 0xF500;
    private const byte MotorBit = 0x10;
    private const byte DataBit = 0x20;

    [Test]
    public void MemoryPattern_SurvivesSaveResetLoad()
    {
        var machine = new Cpc464Machine(new byte[Cpc464Bus.RomSize]);
        // Real CPC firmware programs this PPI control word (mode 0, Port A output, Port B input,
        // Port C output) before touching the cassette relay; the reset-default control word leaves
        // Port C as input, so writes to the motor/data bits would otherwise be silently dropped.
        machine.Bus.WritePort(ControlPort, 0x82);

        const ushort address = 0x4000;
        var original = Enumerable.Range(0, 16).Select(i => (byte)(i * 17 + 5)).ToArray();
        for (var i = 0; i < original.Length; i++)
            machine.Memory.Write((ushort)(address + i), original[i]);

        Encode(machine, original);
        Assert.That(machine.Bus.Cassette.TryGetRecordedTape(out var savedTape), Is.True);
        Assert.That(savedTape, Is.EqualTo(original));

        machine.Reset();
        machine.Bus.WritePort(ControlPort, 0x82); // Reset() clears the PPI control word too
        for (var i = 0; i < original.Length; i++)
            machine.Memory.Write((ushort)(address + i), 0); // prove the reload is real, not stale RAM
        Assert.That(machine.Memory.Read(address), Is.EqualTo(0));

        machine.Bus.Cassette.LoadTape(savedTape);
        var loaded = Decode(machine, original.Length);
        for (var i = 0; i < loaded.Length; i++)
            machine.Memory.Write((ushort)(address + i), loaded[i]);

        for (var i = 0; i < original.Length; i++)
            Assert.That(machine.Memory.Read((ushort)(address + i)), Is.EqualTo(original[i]));
    }

    private static void Encode(Cpc464Machine machine, byte[] source)
    {
        machine.Bus.WritePort(PortCPort, MotorBit); // motor on, write data low
        Tick(machine, 2_000); // leader gap before the first bit
        foreach (var value in source)
            for (var bitIndex = 7; bitIndex >= 0; bitIndex--)
            {
                var high = ((value >> bitIndex) & 1) != 0;
                machine.Bus.WritePort(PortCPort, (byte)(MotorBit | DataBit));
                Tick(machine, high ? Cpc464Cassette.OneBitTicks : Cpc464Cassette.ZeroBitTicks);
                machine.Bus.WritePort(PortCPort, MotorBit);
                Tick(machine, Cpc464Cassette.GapTicks);
            }
        machine.Bus.WritePort(PortCPort, 0x00); // motor off - flushes and stops recording
    }

    private static byte[] Decode(Cpc464Machine machine, int byteCount)
    {
        machine.Bus.WritePort(PortCPort, MotorBit); // motor on

        const int PollTicks = 20;
        var risingEdgeAt = -1;
        var position = 0;
        var pulseWidths = new List<int>();
        var budget = byteCount * 8 * (Cpc464Cassette.OneBitTicks + Cpc464Cassette.GapTicks) * 2;
        while (!machine.Bus.Cassette.AtEndOfTape && position < budget)
        {
            Tick(machine, PollTicks);
            position += PollTicks;
            var high = (machine.Bus.ReadPort(PortBPort) & 0x80) != 0;
            if (high && risingEdgeAt < 0) risingEdgeAt = position;
            else if (!high && risingEdgeAt >= 0) { pulseWidths.Add(position - risingEdgeAt); risingEdgeAt = -1; }
        }

        const int Threshold = (Cpc464Cassette.ZeroBitTicks + Cpc464Cassette.OneBitTicks) / 2;
        var bits = pulseWidths.Select(width => width >= Threshold).ToArray();
        Assert.That(bits, Has.Length.EqualTo(byteCount * 8), "decoded bit count did not match the saved byte count");

        var data = new byte[byteCount];
        for (var i = 0; i < data.Length; i++)
        for (var bit = 0; bit < 8; bit++)
            data[i] = (byte)((data[i] << 1) | (bits[i * 8 + bit] ? 1 : 0));
        return data;
    }

    /// <summary>Cpc464Bus.Tick counts raw Z80 T-states (4 per Gate Array/cassette clock), while
    /// Cpc464Cassette's own timing constants are in cassette-clock ticks - this converts once so
    /// every other call site in this file can read as "N cassette ticks".</summary>
    private static void Tick(Cpc464Machine machine, int cassetteTicks) => machine.Bus.Tick(cassetteTicks * 4);
}
