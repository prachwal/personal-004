namespace PetEmulator.Vic20.Cartridge.Abstractions;

public enum Vic20CartridgeResourceKind
{
    Rom,
    Ram,
    Io,
}

[Flags]
public enum Vic20CartridgeResourceAccess
{
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write,
}

/// <summary>One address range consumed by a VIC-20 cartridge or expansion device.</summary>
public sealed record Vic20CartridgeResource(
    string Name,
    ushort StartAddress,
    ushort Length,
    Vic20CartridgeResourceKind Kind,
    Vic20CartridgeResourceAccess Access)
{
    public ushort EndAddress => (ushort)(StartAddress + Length - 1);

    public bool Overlaps(Vic20CartridgeResource other) =>
        StartAddress < other.StartAddress + other.Length
        && other.StartAddress < StartAddress + Length;
}

public static class Vic20CartridgeResourceValidator
{
    public static void ThrowIfConflicting(IEnumerable<Vic20CartridgeResource> resources)
    {
        var list = resources.ToArray();
        for (var i = 0; i < list.Length; i++)
        for (var j = i + 1; j < list.Length; j++)
            if (list[i].Overlaps(list[j]))
                throw new InvalidOperationException(
                    $"Cartridge resources '{list[i].Name}' and '{list[j].Name}' overlap " +
                    $"at ${Math.Max(list[i].StartAddress, list[j].StartAddress):X4}.");
    }
}

/// <summary>Static metadata exposed before a cartridge image is mounted.</summary>
public sealed record Vic20CartridgeDescriptor(
    string Id,
    string Name,
    IReadOnlyList<Vic20CartridgeResource> Resources,
    IReadOnlyList<string> ImageExtensions);

/// <summary>Executable state of one mounted cartridge plugin.</summary>
public interface IVic20CartridgeInstance
{
    Vic20CartridgeDescriptor Descriptor { get; }

    bool TryRead(ushort address, out byte value);

    bool TryWrite(ushort address, byte value);

    void Reset();

    void Tick(ulong cycles);
}

/// <summary>Entry point exported by a cartridge DLL.</summary>
public interface IVic20CartridgePlugin
{
    Vic20CartridgeDescriptor Descriptor { get; }

    IVic20CartridgeInstance Create(ReadOnlyMemory<byte> image);
}
