using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PetEmulator.Vic20;

public sealed record Vic20PrgImage(ushort LoadAddress, IReadOnlyList<byte> Data);

/// <summary>Reads a VIC-20 PRG file: a little-endian load address followed by program bytes.</summary>
public static class Vic20PrgParser
{
    public static Vic20PrgImage Parse(string path, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var log = logger ?? NullLogger.Instance;
        log.LogInformation("Parsing PRG '{Path}'.", path);
        try
        {
            var image = Parse(File.ReadAllBytes(path));
            log.LogInformation("PRG parsed '{Path}': load=${Load:X4} ({Size} B).", path, image.LoadAddress, image.Data.Count);
            return image;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Parsing PRG '{Path}' failed.", path);
            throw;
        }
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
