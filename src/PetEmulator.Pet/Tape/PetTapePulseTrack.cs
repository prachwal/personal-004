namespace PetEmulator.Pet.Tape;

/// <summary>
/// A classified, positioned view over a raw cassette pulse-width stream (see
/// <see cref="PetTapeCassetteFormat"/>), supporting the sequence a real tape read actually
/// walks: skip a leader/gap of Short pulses, read the sync countdown, read the stream's content
/// bytes, then read and verify its checksum. Used to pull the header block and then the payload
/// block out of a duplicated data stream.
/// </summary>
public sealed class PetTapePulseTrack
{
    private readonly PetTapePulseKind[] _kinds;

    public PetTapePulseTrack(IReadOnlyList<int> pulseCycles)
    {
        ArgumentNullException.ThrowIfNull(pulseCycles);
        _kinds = pulseCycles.Select(PetTapeCassetteFormat.Classify).ToArray();
    }

    public int Length => _kinds.Length;

    /// <summary>Advances past a run of Short pulses (leader tone or inter-copy gap) starting at
    /// <paramref name="index"/>. A stream's own encoded bytes always start with a Long pulse
    /// (the sync countdown's start marker), so this never over-consumes into real data.</summary>
    public void SkipShortRun(ref int index)
    {
        while (index < _kinds.Length && _kinds[index] == PetTapePulseKind.Short)
            index++;
    }

    /// <summary>Reads the 9-byte sync countdown at <paramref name="index"/>, identifies which
    /// copy it belongs to from the top bit ($89..$81 = first copy, $09..$01 = second), and
    /// verifies the countdown is intact before returning.</summary>
    public bool ReadSyncCountdown(ref int index)
    {
        var first = PetTapeCassetteFormat.SyncCountdown(firstCopy: true);
        var second = PetTapeCassetteFormat.SyncCountdown(firstCopy: false);
        var firstByte = PetTapePulseDecoder.DecodeByte(_kinds, ref index);
        var isFirstCopy = firstByte == first[0];
        if (!isFirstCopy && firstByte != second[0])
            throw new FormatException($"Expected the first sync countdown byte (${first[0]:X2} or ${second[0]:X2}), got ${firstByte:X2}.");

        var expected = isFirstCopy ? first : second;
        for (var i = 1; i < expected.Count; i++)
        {
            var actual = PetTapePulseDecoder.DecodeByte(_kinds, ref index);
            if (actual != expected[i])
                throw new FormatException($"Sync countdown broke at step {i}: expected ${expected[i]:X2}, got ${actual:X2}.");
        }

        return isFirstCopy;
    }

    /// <summary>Reads <paramref name="contentLength"/> content bytes followed by one checksum
    /// byte, verifying the checksum is the XOR of the content (the sync bytes are not part of
    /// the checksum).</summary>
    public IReadOnlyList<byte> ReadStreamContent(ref int index, int contentLength)
    {
        var content = new byte[contentLength];
        byte checksum = 0;
        for (var i = 0; i < contentLength; i++)
        {
            content[i] = PetTapePulseDecoder.DecodeByte(_kinds, ref index);
            checksum ^= content[i];
        }

        var actualChecksum = PetTapePulseDecoder.DecodeByte(_kinds, ref index);
        if (actualChecksum != checksum)
            throw new FormatException($"Checksum mismatch: computed ${checksum:X2} over {contentLength} bytes, tape says ${actualChecksum:X2}.");

        return content;
    }

    /// <summary>Reads a full duplicated stream (sync + content + checksum) starting at
    /// <paramref name="index"/>, skipping any leading Short-pulse leader/gap first.</summary>
    public IReadOnlyList<byte> ReadStream(ref int index, int contentLength)
    {
        SkipShortRun(ref index);
        ReadSyncCountdown(ref index);
        return ReadStreamContent(ref index, contentLength);
    }
}
