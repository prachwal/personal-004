using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20;

/// <summary>A register-mapped peripheral exposed by a VIC-20 cartridge in I/O2/I/O3.</summary>
public interface IVic20CartridgeIoDevice
{
    Vic20CartridgeResource Resource { get; }

    ushort StartAddress { get; }

    ushort Length { get; }

    byte Read(ushort address);

    void Write(ushort address, byte value);
}

/// <summary>A VIC-20 cartridge with a raw binary ROM and optional I/O2/I/O3 devices.</summary>
public sealed class Vic20Cartridge : IVic20ExpansionDevice
{
    private readonly byte[] _rom;
    private readonly IVic20CartridgeInstance? _pluginInstance;
    private readonly Vic20BankedCrt? _bankedCrt;

    private Vic20Cartridge(IVic20CartridgeInstance pluginInstance)
    {
        _pluginInstance = pluginInstance ?? throw new ArgumentNullException(nameof(pluginInstance));
        _rom = [];
        IoDevices = [];
        Resources = pluginInstance.Descriptor.Resources;
        Vic20CartridgeResourceValidator.ThrowIfConflicting(Resources);
    }

    private Vic20Cartridge(Vic20BankedCrt bankedCrt)
    {
        _bankedCrt = bankedCrt ?? throw new ArgumentNullException(nameof(bankedCrt));
        _rom = [];
        RomStartAddress = bankedCrt.RomStartAddress;
        IoDevices = [bankedCrt];
        Resources = bankedCrt.Resources;
    }

    public Vic20Cartridge(
        IReadOnlyList<byte> rom,
        ushort romStartAddress = Vic20MemoryMap.CartridgeStart,
        IEnumerable<IVic20CartridgeIoDevice>? ioDevices = null)
    {
        ArgumentNullException.ThrowIfNull(rom);
        if (rom.Count == 0 || rom.Count > Vic20MemoryMap.CartridgeSize)
            throw new ArgumentOutOfRangeException(nameof(rom), "Cartridge ROM must contain 1..8192 bytes.");
        if (!FitsCartridgeWindow(romStartAddress, rom.Count))
            throw new ArgumentOutOfRangeException(nameof(romStartAddress), "Cartridge ROM must fit in a VIC-20 cartridge window ($2000, $4000, $6000 or $A000).");

        _rom = rom.ToArray();
        RomStartAddress = romStartAddress;
        IoDevices = (ioDevices ?? []).ToArray();
        Resources =
        [
            new Vic20CartridgeResource("ROM", RomStartAddress, RomLength,
                Vic20CartridgeResourceKind.Rom, Vic20CartridgeResourceAccess.Read),
            .. IoDevices.Select(device => device.Resource),
        ];
        Vic20CartridgeResourceValidator.ThrowIfConflicting(Resources);

        foreach (var device in IoDevices)
        {
            ArgumentNullException.ThrowIfNull(device);
            if (device.Length == 0
                || device.StartAddress < Vic20MemoryMap.Io2Start
                || device.StartAddress + device.Length > Vic20MemoryMap.Io3End + 1)
                throw new ArgumentException("Cartridge I/O devices must fit in $9800-$9FFF.", nameof(ioDevices));
        }
    }

    public ushort RomStartAddress { get; }

    public ushort RomLength => _bankedCrt?.RomLength ?? (ushort)_rom.Length;

    public IReadOnlyList<IVic20CartridgeIoDevice> IoDevices { get; }

    public IReadOnlyList<Vic20CartridgeResource> Resources { get; }

    public static Vic20Cartridge Load(string path, ushort romStartAddress = Vic20MemoryMap.CartridgeStart, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var log = logger ?? NullLogger.Instance;
        log.LogInformation("Loading cartridge '{Path}'.", path);
        try
        {
            var cartridge = LoadCore(path, romStartAddress, log);
            log.LogInformation("Cartridge loaded '{Path}' ({Resources} resources).", path, cartridge.Resources.Count);
            return cartridge;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Loading cartridge '{Path}' failed.", path);
            throw;
        }
    }

    private static Vic20Cartridge LoadCore(string path, ushort romStartAddress, ILogger log)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path.EndsWith(".prg", StringComparison.OrdinalIgnoreCase))
        {
            Vic20PrgImage prgImage = Vic20PrgParser.Parse(path, log);
            return new(prgImage.Data, prgImage.LoadAddress);
        }
        if (!path.EndsWith(".crt", StringComparison.OrdinalIgnoreCase))
            return new(File.ReadAllBytes(path), romStartAddress);

        Vic20CrtImage image = Vic20CrtParser.Parse(path, log);
        if (image.Chips.Count > 1)
            return new(new Vic20BankedCrt(image.Chips));
        if (image.Chips.Count == 0 || image.Chips[0].Bank != 0)
            throw new InvalidDataException("This VIC-20 CRT must contain a CHIP packet in bank 0.");
        Vic20CrtChip chip = image.Chips[0];
        return new(chip.Data, chip.LoadAddress);
    }

    public static Vic20Cartridge FromPlugin(IVic20CartridgeInstance instance) =>
        new(instance);

    public bool TryRead(ushort address, out byte value)
    {
        if (_pluginInstance is not null)
            return _pluginInstance.TryRead(address, out value);
        if (_bankedCrt is not null)
            return _bankedCrt.TryReadRom(address, out value) || _bankedCrt.TryRead(address, out value);

        var device = FindDevice(address);
        if (device is not null)
        {
            value = device.Read(address);
            return true;
        }

        if (InRange(address, RomStartAddress, RomLength))
        {
            value = _rom[address - RomStartAddress];
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryWrite(ushort address, byte value)
    {
        if (_pluginInstance is not null)
            return _pluginInstance.TryWrite(address, value);
        if (_bankedCrt is not null)
            return _bankedCrt.TryWrite(address, value) || _bankedCrt.ConsumesRomWrite(address);

        var device = FindDevice(address);
        if (device is not null)
        {
            device.Write(address, value);
            return true;
        }

        // A cartridge ROM consumes writes electrically, but does not change its contents.
        return InRange(address, RomStartAddress, RomLength);
    }

    public void Reset()
    {
        _pluginInstance?.Reset();
        _bankedCrt?.Reset();
    }

    public void Tick(ulong cycles)
    {
        _pluginInstance?.Tick(cycles);
        _bankedCrt?.Tick(cycles);
    }

    private IVic20CartridgeIoDevice? FindDevice(ushort address) =>
        IoDevices.FirstOrDefault(device => InRange(address, device.StartAddress, device.Length));

    private static bool InRange(ushort address, ushort start, uint length) =>
        address >= start && address < start + length;

    private static bool FitsCartridgeWindow(ushort start, int length) =>
        FitsWindow(start, length, Vic20MemoryMap.Block1Start, Vic20MemoryMap.Block1Size)
        || FitsWindow(start, length, Vic20MemoryMap.Block2Start, Vic20MemoryMap.Block2Size)
        || FitsWindow(start, length, Vic20MemoryMap.Block3Start, Vic20MemoryMap.Block3Size)
        || FitsWindow(start, length, Vic20MemoryMap.CartridgeStart, Vic20MemoryMap.CartridgeSize);

    private static bool FitsWindow(ushort start, int length, ushort windowStart, int windowLength) =>
        start >= windowStart && start + length <= windowStart + windowLength;
}

/// <summary>Simple writable register block useful for cartridge peripherals and tests.</summary>
public sealed class Vic20RegisterIoDevice : IVic20CartridgeIoDevice
{
    private readonly byte[] _registers;

    public Vic20RegisterIoDevice(ushort startAddress, ushort length)
    {
        if (length == 0 || startAddress < Vic20MemoryMap.Io2Start
            || startAddress + length > Vic20MemoryMap.Io3End + 1)
            throw new ArgumentOutOfRangeException(nameof(length), "Register device must fit in $9800-$9FFF.");

        StartAddress = startAddress;
        Length = length;
        _registers = new byte[length];
        Resource = new Vic20CartridgeResource("I/O device", startAddress, length,
            Vic20CartridgeResourceKind.Io, Vic20CartridgeResourceAccess.ReadWrite);
    }

    public ushort StartAddress { get; }

    public ushort Length { get; }

    public Vic20CartridgeResource Resource { get; }

    public byte Read(ushort address) => _registers[address - StartAddress];

    public void Write(ushort address, byte value) => _registers[address - StartAddress] = value;
}
