using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PetEmulator.Vic20;

public sealed record Vic20CrtImage(
    string Name,
    ushort HardwareType,
    byte Exrom,
    byte Game,
    IReadOnlyList<Vic20CrtChip> Chips);

public sealed record Vic20CrtChip(
    ushort ChipType,
    ushort Bank,
    ushort LoadAddress,
    IReadOnlyList<byte> Data);

/// <summary>Reads the common CRT container used by VIC-20 cartridge images.</summary>
public static class Vic20CrtParser
{
    private const int HeaderLength = 0x40;
    private const int ChipHeaderLength = 0x10;
    private const string Signature = "C64 CARTRIDGE   ";

    public static Vic20CrtImage Parse(string path, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var log = logger ?? NullLogger.Instance;
        log.LogInformation("Parsing CRT '{Path}'.", path);
        try
        {
            var image = Parse(File.ReadAllBytes(path));
            log.LogInformation("CRT parsed '{Path}': name='{Name}' hw={Hardware} chips={Chips}.",
                path, image.Name, image.HardwareType, image.Chips.Count);
            return image;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Parsing CRT '{Path}' failed.", path);
            throw;
        }
    }

    public static Vic20CrtImage Parse(ReadOnlySpan<byte> image)
    {
        if (image.Length < HeaderLength)
            throw new InvalidDataException("CRT image is shorter than its 64-byte header.");
        if (!Encoding.ASCII.GetString(image[..Signature.Length]).Equals(Signature, StringComparison.Ordinal))
            throw new InvalidDataException("CRT image has an invalid signature.");

        uint declaredHeaderLength = ReadUInt32(image, 0x10, "header length");
        if (declaredHeaderLength < HeaderLength || declaredHeaderLength > image.Length)
            throw new InvalidDataException("CRT header length is outside the image.");

        string name = Encoding.ASCII.GetString(image.Slice(0x20, 32)).TrimEnd('\0', ' ');
        var chips = new List<Vic20CrtChip>();
        int offset = checked((int)declaredHeaderLength);
        while (offset < image.Length)
        {
            if (image.Length - offset < ChipHeaderLength)
                throw new InvalidDataException("CRT image contains a truncated CHIP header.");
            if (!image.Slice(offset, 4).SequenceEqual("CHIP"u8))
                throw new InvalidDataException($"CRT image has an invalid CHIP signature at offset 0x{offset:X}.");

            uint packetLength = ReadUInt32(image, offset + 4, "CHIP packet length");
            if (packetLength < ChipHeaderLength || packetLength > image.Length - offset)
                throw new InvalidDataException("CRT CHIP packet length is outside the image.");
            ushort imageSize = ReadUInt16(image, offset + 0x0E, "CHIP image size");
            if (imageSize != packetLength - ChipHeaderLength)
                throw new InvalidDataException("CRT CHIP image size does not match the packet length.");

            int dataOffset = offset + ChipHeaderLength;
            chips.Add(new Vic20CrtChip(
                ReadUInt16(image, offset + 8, "CHIP type"),
                ReadUInt16(image, offset + 0xA, "CHIP bank"),
                ReadUInt16(image, offset + 0xC, "CHIP load address"),
                image.Slice(dataOffset, imageSize).ToArray()));
            offset += checked((int)packetLength);
        }

        if (chips.Count == 0)
            throw new InvalidDataException("CRT image does not contain a CHIP packet.");
        return new Vic20CrtImage(name, ReadUInt16(image, 0x16, "hardware type"), image[0x18], image[0x19], chips);
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> image, int offset, string field)
    {
        if (offset < 0 || offset + sizeof(ushort) > image.Length)
            throw new InvalidDataException($"CRT image is missing {field}.");
        return BinaryPrimitives.ReadUInt16BigEndian(image[offset..]);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> image, int offset, string field)
    {
        if (offset < 0 || offset + sizeof(uint) > image.Length)
            throw new InvalidDataException($"CRT image is missing {field}.");
        return BinaryPrimitives.ReadUInt32BigEndian(image[offset..]);
    }
}
