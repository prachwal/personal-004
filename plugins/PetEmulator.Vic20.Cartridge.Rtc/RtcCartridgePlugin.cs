using PetEmulator.Chips;
using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20.Cartridge.Rtc;

public sealed class RtcCartridgePlugin : IVic20CartridgePlugin
{
    public Vic20CartridgeDescriptor Descriptor { get; } = new(
        "vic20-mc146818-rtc",
        "VIC-20 MC146818 RTC",
        [
            new("RTC ROM", 0xA000, 0x2000, Vic20CartridgeResourceKind.Rom,
                Vic20CartridgeResourceAccess.Read),
            new("MC146818 index/data", 0x9C00, 2, Vic20CartridgeResourceKind.Io,
                Vic20CartridgeResourceAccess.ReadWrite),
        ],
        [".bin"]);

    public IVic20CartridgeInstance Create(ReadOnlyMemory<byte> image)
    {
        if (image.Length == 0 || image.Length > 0x2000)
            throw new ArgumentOutOfRangeException(nameof(image), "RTC cartridge image must contain 1..8192 bytes.");

        return new RtcCartridgeInstance(Descriptor, image.ToArray());
    }
}

internal sealed class RtcCartridgeInstance : IVic20CartridgeInstance
{
    private readonly byte[] _rom;
    private readonly MC146818 _rtc = new(baseAddress: 0x9C00);

    public RtcCartridgeInstance(Vic20CartridgeDescriptor descriptor, byte[] rom)
    {
        Descriptor = descriptor;
        _rom = rom;
    }

    public Vic20CartridgeDescriptor Descriptor { get; }

    public IReadOnlyList<Vic20CartridgeResource> Resources => Descriptor.Resources;

    public bool Irq => _rtc.Irq;

    public bool TryRead(ushort address, out byte value)
    {
        if (address >= 0xA000 && address < 0xA000 + _rom.Length)
        {
            value = _rom[address - 0xA000];
            return true;
        }

        if (address is 0x9C00 or 0x9C01)
        {
            value = _rtc.Read(address);
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryWrite(ushort address, byte value)
    {
        if (address is 0x9C00 or 0x9C01)
        {
            _rtc.Write(address, value);
            return true;
        }

        return address >= 0xA000 && address < 0xA000 + _rom.Length;
    }

    public void Reset() => _rtc.Reset();

    public void Tick(ulong cycles) => _rtc.Tick(cycles);
}
