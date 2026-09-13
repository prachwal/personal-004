namespace PetEmulator.Chips;

public static class Z80SioSyncCodec
{
    public static bool IsSynchronized(ReadOnlySpan<byte> input, Z80SioFrameMode mode, ushort syncWord)
    {
        return mode switch
        {
            Z80SioFrameMode.Sync8 => input.IndexOf((byte)syncWord) >= 0,
            Z80SioFrameMode.Sync16 => ContainsSync16(input, syncWord),
            Z80SioFrameMode.ExternalSync => false,
            _ => true,
        };
    }

    private static bool ContainsSync16(ReadOnlySpan<byte> input, ushort syncWord)
    {
        for (var index = 0; index + 1 < input.Length; index++)
            if ((ushort)((input[index] << 8) | input[index + 1]) == syncWord)
                return true;
        return false;
    }
}
