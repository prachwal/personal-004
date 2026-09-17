using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PetEmulator.Pet.Tape;

/// <summary>Platform byte in a .tap file's header, per VICE's TAP format spec.</summary>
public enum PetTapPlatform : byte { C64 = 0, Vic20 = 1, C16 = 2, Pet = 3 }

/// <summary>
/// Parses a VICE-style .tap container into its header fields and a decoded pulse-length (in CPU
/// cycles) sequence, ready for <see cref="PetTapePulseTrack"/>. 20-byte header
/// ("C64-TAPE-RAW" signature is shared across platforms despite the name; platform byte 3 = PET;
/// PET's own clock is exactly 1,000,000 Hz for both PAL and NTSC, which is what makes byte*8 in a
/// PET-platform .tap equal to PET CPU cycles directly - no further conversion needed).
/// </summary>
public sealed record PetTapFile(byte Version, PetTapPlatform Platform, byte VideoStandard, IReadOnlyList<int> PulseCycles)
{
    private const string Signature = "C64-TAPE-RAW";
    public const int HeaderLength = 20;

    public static PetTapFile Parse(IReadOnlyList<byte> bytes, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var log = logger ?? NullLogger.Instance;
        try
        {
            var tap = ParseCore(bytes);
            log.LogDebug("Parsed .tap ({Size} B): version={Version} platform={Platform} video={Video} pulses={Pulses}.",
                bytes.Count, tap.Version, tap.Platform, tap.VideoStandard, tap.PulseCycles.Count);
            return tap;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Parsing .tap ({Size} B) failed.", bytes.Count);
            throw;
        }
    }

    private static PetTapFile ParseCore(IReadOnlyList<byte> bytes)
    {
        if (bytes.Count < HeaderLength)
            throw new FormatException($"A .tap file needs at least a {HeaderLength}-byte header, got {bytes.Count} bytes.");

        var signature = new string(bytes.Take(12).Select(b => (char)b).ToArray());
        if (signature != Signature)
            throw new FormatException($"Expected \"{Signature}\" signature, got \"{signature}\".");

        var version = bytes[12];
        if (version is not (0 or 1))
            throw new NotSupportedException($"Only .tap version 0 and 1 are supported, got version {version}.");
        var platform = (PetTapPlatform)bytes[13];
        var videoStandard = bytes[14];
        var dataSize = bytes[16] | (bytes[17] << 8) | (bytes[18] << 16) | (bytes[19] << 24);
        if (HeaderLength + dataSize > bytes.Count)
            throw new FormatException($"Header declares {dataSize} data bytes but only {bytes.Count - HeaderLength} are present.");

        var pulses = new List<int>();
        var index = HeaderLength;
        var end = HeaderLength + dataSize;
        while (index < end)
        {
            var value = bytes[index++];
            if (value != 0)
            {
                pulses.Add(value * 8);
                continue;
            }

            if (version == 0)
                throw new NotSupportedException(
                    "Version 0 .tap files use a bare $00 byte to mean \"a long, unspecified gap (>2040 cycles)\" " +
                    "with no exact length recorded - there's no cycle count to recover, so this isn't decodable " +
                    "into a real pulse. Re-save the tape as version 1 (which stores the exact cycle count) if possible.");

            if (index + 3 > end)
                throw new FormatException("Version 1 long-pulse marker ($00) at end of data with no 3-byte cycle count following.");
            pulses.Add(bytes[index] | (bytes[index + 1] << 8) | (bytes[index + 2] << 16));
            index += 3;
        }

        return new PetTapFile(version, platform, videoStandard, pulses);
    }
}
