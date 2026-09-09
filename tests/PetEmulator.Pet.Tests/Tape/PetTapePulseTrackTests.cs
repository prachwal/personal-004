using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet.Tests.Tape;

public sealed class PetTapePulseTrackTests
{
    private static byte[] MakeHeaderContent(PetTapeHeaderType type, ushort start, ushort end, string name)
    {
        var content = new byte[PetTapeHeaderBlock.Length];
        content[0] = (byte)type;
        content[1] = (byte)(start & 0xFF);
        content[2] = (byte)(start >> 8);
        content[3] = (byte)(end & 0xFF);
        content[4] = (byte)(end >> 8);
        Array.Fill(content, (byte)0x20, 5, content.Length - 5); // space-pad, like a real header
        for (var i = 0; i < name.Length; i++) content[5 + i] = (byte)name[i];
        return content;
    }

    [Test]
    public void Reads_a_header_block_after_leader_and_sync()
    {
        var content = MakeHeaderContent(PetTapeHeaderType.NonRelocatableProgram, 0x0401, 0x0500, "TEST");
        var pulses = PetTapeTestEncoder.EncodeStream(content, firstCopy: true);
        var track = new PetTapePulseTrack(pulses);
        var index = 0;

        var decoded = track.ReadStream(ref index, PetTapeHeaderBlock.Length);
        var header = PetTapeHeaderBlock.Parse(decoded);

        header.HeaderType.Should().Be(PetTapeHeaderType.NonRelocatableProgram);
        header.StartAddress.Should().Be(0x0401);
        header.EndAddress.Should().Be(0x0500);
        header.FileName.Should().Be("TEST");
        index.Should().Be(pulses.Count, "the reader should consume exactly the encoded pulses, nothing more or less");
    }

    [Test]
    public void Reads_both_duplicated_copies_of_a_stream_in_sequence()
    {
        var content = MakeHeaderContent(PetTapeHeaderType.RelocatableProgram, 0x0401, 0x0410, "A");
        var pulses = PetTapeTestEncoder.EncodeStream(content, firstCopy: true);
        pulses.AddRange(PetTapeTestEncoder.EncodeStream(content, firstCopy: false)); // gap is just more leader
        var track = new PetTapePulseTrack(pulses);
        var index = 0;

        track.SkipShortRun(ref index);
        var firstCopyIsFirst = track.ReadSyncCountdown(ref index);
        var firstContent = track.ReadStreamContent(ref index, PetTapeHeaderBlock.Length);
        track.SkipShortRun(ref index); // the gap between copies is just another run of Short pulses
        var secondCopyIsFirst = track.ReadSyncCountdown(ref index);
        var secondContent = track.ReadStreamContent(ref index, PetTapeHeaderBlock.Length);

        firstCopyIsFirst.Should().BeTrue();
        secondCopyIsFirst.Should().BeFalse();
        firstContent.Should().Equal(secondContent);
    }

    [Test]
    public void Rejects_a_broken_sync_countdown()
    {
        var content = MakeHeaderContent(PetTapeHeaderType.RelocatableProgram, 0x0401, 0x0410, "X");
        var pulses = PetTapeTestEncoder.EncodeStream(content, firstCopy: true);
        // Corrupt the 3rd sync byte (index 2 within the 9-byte countdown, after the leader).
        var syncByteStart = 20 + 2 * 20; // leader (20 shorts) + 2 full encoded sync bytes (20 pulses each)
        (pulses[syncByteStart], pulses[syncByteStart + 1]) = (pulses[syncByteStart + 1], pulses[syncByteStart]);

        var track = new PetTapePulseTrack(pulses);
        var index = 0;
        var act = () => track.ReadStream(ref index, PetTapeHeaderBlock.Length);
        act.Should().Throw<FormatException>();
    }

    [Test]
    public void Rejects_a_checksum_mismatch()
    {
        // Each content byte carries its own valid parity bit, so corrupting a bit in-place always
        // trips that byte's own parity check before the checksum is ever compared. To exercise
        // the checksum check specifically, build the stream with a deliberately wrong (but still
        // internally valid) checksum byte instead of mutating an already-encoded pulse.
        var content = MakeHeaderContent(PetTapeHeaderType.RelocatableProgram, 0x0401, 0x0410, "X");
        var pulses = new List<int>(Enumerable.Repeat(PetTapeCassetteFormat.ShortPulseCycles, 20));
        pulses.AddRange(PetTapeTestEncoder.EncodeBytes(PetTapeCassetteFormat.SyncCountdown(firstCopy: true)));
        pulses.AddRange(PetTapeTestEncoder.EncodeBytes(content));
        byte correctChecksum = 0;
        foreach (var b in content) correctChecksum ^= b;
        pulses.AddRange(PetTapeTestEncoder.EncodeByte((byte)(correctChecksum ^ 0xFF)));

        var track = new PetTapePulseTrack(pulses);
        var index = 0;
        var act = () => track.ReadStream(ref index, PetTapeHeaderBlock.Length);
        act.Should().Throw<FormatException>().WithMessage("*Checksum mismatch*");
    }
}
