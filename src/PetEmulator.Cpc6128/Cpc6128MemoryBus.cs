using PetEmulator.CpuZ80.Bus;
using PetEmulator.Core;

namespace PetEmulator.Cpc6128;

/// <summary>CPC6128 CPU bus with eight physical 16 KiB RAM banks and ROM overlays.</summary>
public sealed class Cpc6128MemoryBus : IBus, IMemoryBus
{
    public const int AddressSpaceSize = 0x10000;
    public const int PhysicalRamSize = 0x20000;
    public const int RomSize = 0x8000;

    private readonly byte[] _ram = new byte[PhysicalRamSize];
    private readonly byte[] _rom;
    private readonly Func<bool> _lowerRomEnabled;
    private readonly Func<bool> _upperRomEnabled;
    private readonly Func<byte> _ramConfiguration;
    private readonly Dictionary<byte, byte[]> _upperRoms = [];
    private readonly int[] _banks = new int[4];
    private Cpc6128Ports? _ports;
    private byte _mappedConfiguration = 0xFF;
    private byte _upperRomNumber;

    public Cpc6128MemoryBus(ReadOnlySpan<byte> rom, Func<bool> lowerRomEnabled,
        Func<bool> upperRomEnabled, Func<byte> ramConfiguration)
    {
        if (rom.Length != RomSize)
            throw new ArgumentException("CPC6128 ROM must be exactly 32 KB.", nameof(rom));
        _rom = rom.ToArray();
        _lowerRomEnabled = lowerRomEnabled;
        _upperRomEnabled = upperRomEnabled;
        _ramConfiguration = ramConfiguration;
        SetBankMapping(0);
    }

    public byte UpperRomNumber => _upperRomNumber;

    public void AttachPorts(Cpc6128Ports ports) => _ports = ports;

    public byte Read(ushort address) => ReadMemory(address);
    public void Write(ushort address, byte value) => WriteMemory(address, value);
    public byte ReadMemory(ushort address)
    {
        SetBankMapping(_ramConfiguration());
        if (address < 0x4000 && _lowerRomEnabled()) return _rom[address];
        if (address >= 0xC000 && _upperRomEnabled()) return SelectedUpperRom()[address - 0xC000];
        return _ram[PhysicalAddress(address)];
    }

    public byte ReadRam(ushort address)
    {
        SetBankMapping(_ramConfiguration());
        return _ram[PhysicalAddress(address)];
    }

    /// <summary>Video always sees base 64 KB, independently of the CPU bank selection.</summary>
    public byte ReadVideoRam(ushort address) => _ram[address];

    public void WriteMemory(ushort address, byte value)
    {
        SetBankMapping(_ramConfiguration());
        var physical = PhysicalAddress(address);
        _ram[physical] = value;
    }

    public byte ReadPort(byte port) => _ports?.Read(port) ?? 0xFF;
    public byte ReadPort(ushort port) => _ports?.Read(port) ?? 0xFF;
    public void WritePort(byte port, byte value) => _ports?.Write(port, value);
    public void WritePort(ushort port, byte value) => _ports?.Write(port, value);
    public byte AcknowledgeInterrupt() => _ports?.AcknowledgeInterrupt() ?? 0xFF;

    public void Reset()
    {
        Array.Clear(_ram);
        _upperRomNumber = 0;
        SetBankMapping(0);
    }

    public void SelectUpperRom(byte number) => _upperRomNumber = number;

    public void LoadUpperRom(byte number, ReadOnlySpan<byte> rom)
    {
        if (rom.Length != 0x4000)
            throw new ArgumentException("Upper ROM must be exactly 16 KB.", nameof(rom));
        _upperRoms[number] = rom.ToArray();
    }

    public byte[] CapturePhysicalRam() => _ram.ToArray();

    public void RestorePhysicalRam(ReadOnlySpan<byte> data)
    {
        if (data.Length != PhysicalRamSize)
            throw new ArgumentException($"Expected {PhysicalRamSize} bytes of physical RAM.", nameof(data));
        data.CopyTo(_ram);
    }

    private ReadOnlySpan<byte> SelectedUpperRom() =>
        _upperRomNumber == 0 || !_upperRoms.TryGetValue(_upperRomNumber, out var rom)
            ? _rom.AsSpan(0x4000, 0x4000) : rom;

    private Span<byte> PhysicalRam(int window) => _ram.AsSpan(_banks[window] * 0x4000, 0x4000);
    private int PhysicalAddress(ushort address) => _banks[address >> 14] * 0x4000 + (address & 0x3FFF);

    private bool SetBankMapping(byte configuration)
    {
        configuration &= 7;
        if (configuration == _mappedConfiguration) return false;
        int[] mapping = configuration switch
        {
            0 => [0, 1, 2, 3],
            1 => [0, 1, 2, 7],
            2 => [4, 5, 6, 7],
            3 => [0, 3, 2, 7],
            4 => [0, 4, 2, 3],
            5 => [0, 5, 2, 3],
            6 => [0, 6, 2, 3],
            _ => [0, 7, 2, 3],
        };
        mapping.CopyTo(_banks, 0);
        _mappedConfiguration = configuration;
        return true;
    }

}
