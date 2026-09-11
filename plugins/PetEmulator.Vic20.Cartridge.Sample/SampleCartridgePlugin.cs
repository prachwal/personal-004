using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20.Cartridge.Sample;

public sealed class SampleCartridgePlugin : IVic20CartridgePlugin
{
    public Vic20CartridgeDescriptor Descriptor { get; } = new(
        "sample-8k-io",
        "Sample 8K ROM + I/O2",
        [
            new("ROM", 0xA000, 0x2000, Vic20CartridgeResourceKind.Rom,
                Vic20CartridgeResourceAccess.Read),
            new("I/O2 register", 0x9800, 1, Vic20CartridgeResourceKind.Io,
                Vic20CartridgeResourceAccess.ReadWrite),
        ],
        [".bin"]);

    public IVic20CartridgeInstance Create(ReadOnlyMemory<byte> image)
    {
        if (image.Length == 0 || image.Length > 0x2000)
            throw new ArgumentOutOfRangeException(nameof(image), "Sample cartridge image must contain 1..8192 bytes.");

        return new SampleCartridgeInstance(Descriptor, image.ToArray());
    }
}

internal sealed class SampleCartridgeInstance : IVic20CartridgeInstance
{
    private readonly byte[] _image;
    private byte _register;

    public SampleCartridgeInstance(Vic20CartridgeDescriptor descriptor, byte[] image)
    {
        Descriptor = descriptor;
        _image = image;
    }

    public Vic20CartridgeDescriptor Descriptor { get; }

    public bool TryRead(ushort address, out byte value)
    {
        if (address == 0x9800)
        {
            value = _register;
            return true;
        }

        if (address >= 0xA000 && address < 0xA000 + _image.Length)
        {
            value = _image[address - 0xA000];
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryWrite(ushort address, byte value)
    {
        if (address != 0x9800)
            return address >= 0xA000 && address < 0xA000 + _image.Length;

        _register = value;
        return true;
    }

    public void Reset() => _register = 0;

    public void Tick(ulong cycles) { }
}
