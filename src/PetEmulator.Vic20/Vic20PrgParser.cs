using System.Buffers.Binary;

namespace PetEmulator.Vic20;

public sealed record Vic20PrgImage(ushort LoadAddress, IReadOnlyList<byte> Data);

/// <summary>Reads a VIC-20 PRG file: a little-endian load address followed by program bytes.</summary>
public static class Vic20PrgParser
{
    public static Vic20PrgImage Parse(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Parse(File.ReadAllBytes(path));
    }

    public static Vic20PrgImage Parse(ReadOnlySpan<byte> image)
    {
        if (image.Length < sizeof(ushort) + 1)
            throw new InvalidDataException("VIC-20 PRG must contain a load address and at least one byte.");

        return new Vic20PrgImage(
            BinaryPrimitives.ReadUInt16LittleEndian(image),
            image[sizeof(ushort)..].ToArray());
    }
}
