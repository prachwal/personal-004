using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet.Tests.Tape;

/// <summary>
/// No real .tap fixture exists in this repo, and PetTapePulseDecoder is exercised here by
/// round-tripping through a matching encoder (PetTapeTestEncoder) - it verifies the decoder
/// correctly implements the documented bit-pair/byte-framing algorithm on its own terms.
/// </summary>
public sealed class PetTapePulseDecoderTests
{
    [Test]
    public void Decodes_a_single_encoded_byte()
    {
        var pulses = PetTapeTestEncoder.EncodeByte(0x41);
        PetTapePulseDecoder.DecodeBytes(pulses).Should().Equal(0x41);
    }

    [TestCase(new byte[] { 0x00 })]
    [TestCase(new byte[] { 0xFF })]
    [TestCase(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })]
    [TestCase(new byte[] { 0x0D, 0x50, 0x45, 0x54 })] // arbitrary "PET"-ish bytes
    public void Round_trips_arbitrary_byte_sequences(byte[] data)
    {
        var pulses = PetTapeTestEncoder.EncodeBytes(data);
        PetTapePulseDecoder.DecodeBytes(pulses).Should().Equal(data);
    }

    [Test]
    public void Rejects_a_truncated_stream()
    {
        var pulses = PetTapeTestEncoder.EncodeByte(0x41);
        var truncated = pulses.Take(pulses.Count - 3).ToArray();
        var act = () => PetTapePulseDecoder.DecodeBytes(truncated);
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void Rejects_a_corrupted_parity_bit()
    {
        var pulses = PetTapeTestEncoder.EncodeByte(0x41);
        // Last two pulses are the parity bit-pair; swap them to flip the parity result.
        (pulses[^1], pulses[^2]) = (pulses[^2], pulses[^1]);
        var act = () => PetTapePulseDecoder.DecodeBytes(pulses);
        act.Should().Throw<FormatException>().WithMessage("*Parity mismatch*");
    }
}
