using NUnit.Framework;

namespace PetEmulator.Cpc464.Tests;

public sealed class Cpc464CdtPlaybackTests
{
    private const ushort ControlPort = 0xF700;
    private const ushort PortCPort = 0xF600;
    private const ushort PortBPort = 0xF500;

    [Test]
    public void StandardSpeedBlockPlaysBackThroughThePpiCassetteInput()
    {
        byte[] expected = [0xA5, 0x3C, 0x7E];
        var image = Cpc464CdtImage.Parse(CreateStandardBlock(expected));
        var machine = new Cpc464Machine(new byte[Cpc464Bus.RomSize]);
        machine.Bus.WritePort(ControlPort, 0x82);
        machine.Bus.Cassette.LoadPulses(image.PulseTicks);
        machine.Bus.WritePort(PortCPort, 0x10);

        var pulseWidths = PollPulseWidths(machine, image.PulseTicks.Sum(ticks => (long)ticks));
        var dataPulses = pulseWidths
            .Skip(Cpc464CdtImage.StandardDataPilotPulseCount + 2)
            .Take(expected.Length * 8 * 2)
            .ToArray();

        Assert.That(dataPulses, Has.Length.EqualTo(expected.Length * 16));
        Assert.That(Decode(dataPulses), Is.EqualTo(expected));
    }

    private static byte[] CreateStandardBlock(byte[] data)
    {
        var bytes = new List<byte>("ZXTape!"u8.ToArray()) { 0x1A, 1, 20, 0x10, 1, 0 };
        bytes.Add((byte)data.Length);
        bytes.Add((byte)(data.Length >> 8));
        bytes.AddRange(data);
        return bytes.ToArray();
    }

    private static List<int> PollPulseWidths(Cpc464Machine machine, long tapeTicks)
    {
        const int pollTicks = 10;
        var previous = (machine.Bus.ReadPort(PortBPort) & 0x80) != 0;
        var elapsed = 0;
        var widths = new List<int>();
        for (long position = 0; position <= tapeTicks + pollTicks && !machine.Bus.Cassette.AtEndOfTape; position += pollTicks)
        {
            machine.Tick(pollTicks * 4);
            elapsed += pollTicks;
            var current = (machine.Bus.ReadPort(PortBPort) & 0x80) != 0;
            if (current == previous) continue;
            widths.Add(elapsed);
            elapsed = 0;
            previous = current;
        }
        return widths;
    }

    private static byte[] Decode(IReadOnlyList<int> pulseWidths)
    {
        const int threshold = (855 / 4 + 1710 / 4) / 2;
        var data = new byte[pulseWidths.Count / 16];
        for (var byteIndex = 0; byteIndex < data.Length; byteIndex++)
        for (var bit = 0; bit < 8; bit++)
        {
            var pulse = (pulseWidths[(byteIndex * 8 + bit) * 2] + pulseWidths[(byteIndex * 8 + bit) * 2 + 1]) / 2;
            data[byteIndex] = (byte)((data[byteIndex] << 1) | (pulse >= threshold ? 1 : 0));
        }
        return data;
    }
}
