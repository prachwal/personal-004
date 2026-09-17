using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PetEmulator.Cpc464;

/// <summary>Parses a CDT/TZX image into alternating cassette pulse widths in microseconds.</summary>
public sealed record Cpc464CdtImage(IReadOnlyList<int> PulseTicks)
{
    public const int StandardHeaderPilotPulseCount = 8063;
    public const int StandardDataPilotPulseCount = 3223;
    public const int StopPauseTicks = 5_000_000;

    public static Cpc464CdtImage Parse(byte[] bytes, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var log = logger ?? NullLogger.Instance;
        try
        {
            var image = ParseCore(bytes);
            log.LogInformation("Parsed CDT ({Size} B): {Pulses} pulses.", bytes.Length, image.PulseTicks.Count);
            return image;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Parsing CDT ({Size} B) failed.", bytes.Length);
            throw;
        }
    }

    private static Cpc464CdtImage ParseCore(byte[] bytes)
    {
        if (bytes.Length < 10 || !bytes.AsSpan(0, 8).SequenceEqual("ZXTape!\x1A"u8))
            throw new FormatException("A CDT image must start with the ZXTape! signature and contain two version bytes.");

        var pulses = new List<int>();
        var offset = 10;
        while (offset < bytes.Length)
        {
            var blockOffset = offset;
            var blockId = ReadByte(bytes, ref offset);
            switch (blockId)
            {
                case 0x10:
                {
                    var pause = ReadUInt16(bytes, ref offset);
                    var data = ReadBytes(bytes, ref offset, ReadUInt16(bytes, ref offset));
                    AddStandardBlock(pulses, data, pause);
                    break;
                }
                case 0x11:
                {
                    var pilot = ReadUInt16(bytes, ref offset);
                    var sync1 = ReadUInt16(bytes, ref offset);
                    var sync2 = ReadUInt16(bytes, ref offset);
                    var zero = ReadUInt16(bytes, ref offset);
                    var one = ReadUInt16(bytes, ref offset);
                    var pilotCount = ReadUInt16(bytes, ref offset);
                    var usedBits = ReadByte(bytes, ref offset);
                    var pause = ReadUInt16(bytes, ref offset);
                    var data = ReadBytes(bytes, ref offset, ReadUInt24(bytes, ref offset));
                    AddRepeated(pulses, pilot, pilotCount);
                    AddPulse(pulses, sync1);
                    AddPulse(pulses, sync2);
                    AddDataBlock(pulses, data, usedBits, zero, one);
                    AddPause(pulses, pause);
                    break;
                }
                case 0x12:
                    AddRepeated(pulses, ReadUInt16(bytes, ref offset), ReadUInt16(bytes, ref offset));
                    break;
                case 0x13:
                {
                    var count = ReadByte(bytes, ref offset);
                    for (var i = 0; i < count; i++)
                        AddPulse(pulses, ReadUInt16(bytes, ref offset));
                    break;
                }
                case 0x14:
                {
                    var zero = ReadUInt16(bytes, ref offset);
                    var one = ReadUInt16(bytes, ref offset);
                    var usedBits = ReadByte(bytes, ref offset);
                    var pause = ReadUInt16(bytes, ref offset);
                    var data = ReadBytes(bytes, ref offset, ReadUInt24(bytes, ref offset));
                    AddDataBlock(pulses, data, usedBits, zero, one);
                    AddPause(pulses, pause);
                    break;
                }
                case 0x20:
                    AddPause(pulses, ReadUInt16(bytes, ref offset));
                    break;
                case 0x21:
                case 0x30:
                    Skip(bytes, ref offset, ReadByte(bytes, ref offset));
                    break;
                case 0x22:
                    break;
                default:
                    throw new NotSupportedException($"Unsupported CDT block 0x{blockId:X2} at offset 0x{blockOffset:X}.");
            }
        }

        return new Cpc464CdtImage(pulses.ToArray());
    }

    private static void AddStandardBlock(List<int> pulses, byte[] data, int pauseMilliseconds)
    {
        AddRepeated(pulses, 2168, data.Length > 0 && data[0] == 0 ? StandardHeaderPilotPulseCount : StandardDataPilotPulseCount);
        AddPulse(pulses, 667);
        AddPulse(pulses, 735);
        AddDataBlock(pulses, data, 8, 855, 1710);
        AddPause(pulses, pauseMilliseconds);
    }

    private static void AddDataBlock(List<int> pulses, byte[] data, int usedBitsInLastByte, int zeroTStates, int oneTStates)
    {
        var lastByteBits = usedBitsInLastByte == 0 ? 8 : usedBitsInLastByte;
        if (lastByteBits is < 1 or > 8)
            throw new FormatException($"A CDT data block cannot use {usedBitsInLastByte} bits in its last byte.");

        for (var byteIndex = 0; byteIndex < data.Length; byteIndex++)
        {
            var bitCount = byteIndex == data.Length - 1 ? lastByteBits : 8;
            for (var bit = 7; bit >= 8 - bitCount; bit--)
                AddRepeated(pulses, ((data[byteIndex] >> bit) & 1) == 0 ? zeroTStates : oneTStates, 2);
        }
    }

    private static void AddPause(List<int> pulses, int milliseconds) =>
        pulses.Add(milliseconds == 0 ? StopPauseTicks : checked(milliseconds * 1000));

    private static void AddRepeated(List<int> pulses, int tStates, int count)
    {
        var ticks = ToCassetteTicks(tStates);
        for (var i = 0; i < count; i++)
            pulses.Add(ticks);
    }

    private static void AddPulse(List<int> pulses, int tStates) => pulses.Add(ToCassetteTicks(tStates));

    private static int ToCassetteTicks(int tStates) => Math.Max(1, tStates / 4);

    private static byte ReadByte(byte[] bytes, ref int offset)
    {
        EnsureOffset(bytes, offset, 1);
        return bytes[offset++];
    }

    private static int ReadUInt16(byte[] bytes, ref int offset)
    {
        EnsureOffset(bytes, offset, 2);
        var value = bytes[offset] | bytes[offset + 1] << 8;
        offset += 2;
        return value;
    }

    private static int ReadUInt24(byte[] bytes, ref int offset)
    {
        EnsureOffset(bytes, offset, 3);
        var value = bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16;
        offset += 3;
        return value;
    }

    private static byte[] ReadBytes(byte[] bytes, ref int offset, int count)
    {
        EnsureOffset(bytes, offset, count);
        var value = bytes.AsSpan(offset, count).ToArray();
        offset += count;
        return value;
    }

    private static void Skip(byte[] bytes, ref int offset, int count)
    {
        EnsureOffset(bytes, offset, count);
        offset += count;
    }

    private static void EnsureOffset(byte[] bytes, int offset, int count)
    {
        if (count < 0 || offset < 0 || offset > bytes.Length - count)
            throw new FormatException($"CDT block is truncated at offset 0x{offset:X}.");
    }
}
