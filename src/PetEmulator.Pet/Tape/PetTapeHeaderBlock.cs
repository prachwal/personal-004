namespace PetEmulator.Pet.Tape;

public enum PetTapeHeaderType : byte
{
    RelocatableProgram = 0x01, // BASIC program: loads wherever current start-of-memory points
    NonRelocatableProgram = 0x03, // machine code / SAVE with secondary address 1: loads at StartAddress
    SequentialFileData = 0x04,
    EndOfTape = 0x05
}

/// <summary>
/// A decoded 192-byte tape header block: type, load/end address, and PETSCII filename. Layout:
/// byte 0 = header type, bytes 1-2 = start address (LE), bytes 3-4 = end address (LE), bytes
/// 5..191 = filename (space-padded; trailing spaces are not part of the name).
/// </summary>
public sealed record PetTapeHeaderBlock(PetTapeHeaderType HeaderType, ushort StartAddress, ushort EndAddress, string FileName)
{
    public const int Length = 192;

    public static PetTapeHeaderBlock Parse(IReadOnlyList<byte> content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Count != Length)
            throw new ArgumentException($"A tape header block is always {Length} bytes, got {content.Count}.", nameof(content));

        var headerType = (PetTapeHeaderType)content[0];
        var startAddress = (ushort)(content[1] | (content[2] << 8));
        var endAddress = (ushort)(content[3] | (content[4] << 8));
        // PETSCII filename runs from byte 5 to the end of the block; trailing spaces (0x20) are padding, not part of the name.
        var trimmedLength = content.Count;
        while (trimmedLength > 5 && content[trimmedLength - 1] == 0x20)
            trimmedLength--;
        var fileName = new string(content.Skip(5).Take(trimmedLength - 5).Select(b => (char)b).ToArray());

        return new PetTapeHeaderBlock(headerType, startAddress, endAddress, fileName);
    }
}
