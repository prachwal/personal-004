using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20.Cartridge.Ram;

public sealed class RamExpansionCartridgePlugin : IVic20CartridgePlugin
{
    public Vic20CartridgeDescriptor Descriptor { get; } = new(
        PluginId,
        PluginName,
        Resources,
        [".bin"]);

    public IVic20CartridgeInstance Create(ReadOnlyMemory<byte> image)
    {
        if (image.Length != 0 && image.Length != TotalLength)
            throw new ArgumentOutOfRangeException(nameof(image),
                $"{PluginName} image must be empty or contain exactly {TotalLength} bytes.");

        return new RamExpansionInstance(Descriptor,
            image.Length == 0 ? new byte[TotalLength] : image.ToArray());
    }

#if VIC20_RAM_3K
    private const string PluginId = "vic20-ram-3k";
    private const string PluginName = "VIC-20 +3K RAM";
    private static readonly RangeDefinition[] Ranges = [new("+3K RAM", 0x0400, 0x0C00)];
#elif VIC20_RAM_8K
    private const string PluginId = "vic20-ram-8k";
    private const string PluginName = "VIC-20 +8K RAM";
    private static readonly RangeDefinition[] Ranges = [new("+8K RAM block 1", 0x2000, 0x2000)];
#elif VIC20_RAM_16K
    private const string PluginId = "vic20-ram-16k";
    private const string PluginName = "VIC-20 +16K RAM";
    private static readonly RangeDefinition[] Ranges =
    [
        new("+8K RAM block 1", 0x2000, 0x2000),
        new("+8K RAM block 2", 0x4000, 0x2000),
    ];
#elif VIC20_RAM_24K
    private const string PluginId = "vic20-ram-24k";
    private const string PluginName = "VIC-20 +24K RAM";
    private static readonly RangeDefinition[] Ranges =
    [
        new("+8K RAM block 1", 0x2000, 0x2000),
        new("+8K RAM block 2", 0x4000, 0x2000),
        new("+8K RAM block 3", 0x6000, 0x2000),
    ];
#elif VIC20_RAM_35K
    private const string PluginId = "vic20-ram-35k";
    private const string PluginName = "VIC-20 +35K RAM";
    private static readonly RangeDefinition[] Ranges =
    [
        new("+3K RAM", 0x0400, 0x0C00),
        new("+8K RAM block 1", 0x2000, 0x2000),
        new("+8K RAM block 2", 0x4000, 0x2000),
        new("+8K RAM block 3", 0x6000, 0x2000),
        new("+8K RAM block 5", 0xA000, 0x2000),
    ];
#else
#error Define one VIC20_RAM_* build constant for this plugin.
#endif

    private static int TotalLength => Ranges.Sum(range => range.Length);

    private static IReadOnlyList<Vic20CartridgeResource> Resources =>
        Ranges.Select(range => new Vic20CartridgeResource(
            range.Name,
            range.Start,
            range.Length,
            Vic20CartridgeResourceKind.Ram,
            Vic20CartridgeResourceAccess.ReadWrite)).ToArray();

    private sealed record RangeDefinition(string Name, ushort Start, ushort Length);
}

internal sealed class RamExpansionInstance : IVic20CartridgeInstance
{
    private readonly byte[] _memory;
    private readonly IReadOnlyList<Vic20CartridgeResource> _resources;

    public RamExpansionInstance(Vic20CartridgeDescriptor descriptor, byte[] memory)
    {
        Descriptor = descriptor;
        _memory = memory;
        _resources = descriptor.Resources;
    }

    public Vic20CartridgeDescriptor Descriptor { get; }

    public bool TryRead(ushort address, out byte value)
    {
        if (TryGetOffset(address, out var offset))
        {
            value = _memory[offset];
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryWrite(ushort address, byte value)
    {
        if (!TryGetOffset(address, out var offset))
            return false;

        _memory[offset] = value;
        return true;
    }

    public void Reset() => Array.Clear(_memory);

    public void Tick(ulong cycles) { }

    private bool TryGetOffset(ushort address, out int offset)
    {
        var baseOffset = 0;
        foreach (var resource in _resources)
        {
            if (address >= resource.StartAddress && address <= resource.EndAddress)
            {
                offset = baseOffset + address - resource.StartAddress;
                return true;
            }

            baseOffset += resource.Length;
        }

        offset = 0;
        return false;
    }
}
