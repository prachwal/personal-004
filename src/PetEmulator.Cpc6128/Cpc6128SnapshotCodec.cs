using System.Text.Json;

namespace PetEmulator.Cpc6128;

public static class Cpc6128SnapshotCodec
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public static byte[] Encode(Cpc6128Snapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.SerializeToUtf8Bytes(snapshot, Options);
    }

    public static Cpc6128Snapshot Decode(ReadOnlySpan<byte> data)
        => JsonSerializer.Deserialize<Cpc6128Snapshot>(data, Options)
            ?? throw new InvalidDataException("CPC6128 snapshot is empty.");
}
