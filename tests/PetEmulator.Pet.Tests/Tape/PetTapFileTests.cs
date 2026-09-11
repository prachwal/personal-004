using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet.Tests.Tape;

public sealed class PetTapFileTests
{
    private static byte[] BuildTap(byte version, byte platform, byte[] payload)
    {
        var bytes = new List<byte>();
        bytes.AddRange("C64-TAPE-RAW"u8.ToArray());
        bytes.Add(version);
        bytes.Add(platform);
        bytes.Add(0); // video standard
        bytes.Add(0); // reserved
        bytes.AddRange(BitConverter.GetBytes(payload.Length));
        bytes.AddRange(payload);
        return bytes.ToArray();
    }

    [Test]
    public void Parses_header_fields_and_version0_pulses()
    {
        // 44*8=352, 64*8=512, 84*8=672 - exactly the Short/Medium/Long nominal widths.
        var tap = BuildTap(version: 0, platform: (byte)PetTapPlatform.Pet, [44, 64, 84]);

        var parsed = PetTapFile.Parse(tap);

        parsed.Version.Should().Be(0);
        parsed.Platform.Should().Be(PetTapPlatform.Pet);
        parsed.PulseCycles.Should().Equal(352, 512, 672);
    }

    [Test]
    public void Parses_version1_long_pulse_escape()
    {
        // $00 followed by a 3-byte LE cycle count, per VICE's version-1 encoding.
        var payload = new byte[] { 44, 0x00, 0x88, 0x13, 0x00, 64 }; // 44*8=352, then 0x001388=5000 cycles, then 512
        var tap = BuildTap(version: 1, platform: (byte)PetTapPlatform.Pet, payload);

        var parsed = PetTapFile.Parse(tap);

        parsed.PulseCycles.Should().Equal(352, 5000, 512);
    }

    [Test]
    public void Rejects_a_bad_signature()
    {
        var tap = BuildTap(0, (byte)PetTapPlatform.Pet, [44]);
        tap[0] = (byte)'X';
        var act = () => PetTapFile.Parse(tap);
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void Rejects_version0_long_pulse_escape_as_unrecoverable()
    {
        var tap = BuildTap(0, (byte)PetTapPlatform.Pet, [44, 0x00]);
        var act = () => PetTapFile.Parse(tap);
        act.Should().Throw<NotSupportedException>();
    }

    [Test]
    public void Rejects_a_truncated_data_size_claim()
    {
        var tap = BuildTap(0, (byte)PetTapPlatform.Pet, [44, 64, 84]);
        var truncated = tap.Take(tap.Length - 1).ToArray(); // header claims 3 payload bytes, only 2 present
        var act = () => PetTapFile.Parse(truncated);
        act.Should().Throw<FormatException>();
    }
}
