using PetEmulator.Chips;
using PetEmulator.Core;
using PetEmulator.CpuZ80.Bus;
using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.Kaypro;

/// <summary>Kaypro II address and I/O bus composed from Core and chip contracts.</summary>
public sealed class KayproBus : IBus, IMemoryBus
{
    public const ushort SystemPort = 0x1C;
    public const ushort VideoBase = 0x3000;
    public const ushort RomTop = 0x07FF;

    private readonly byte[] _ram = new byte[ushort.MaxValue + 1];
    private readonly byte[] _rom = new byte[0x800];
    private readonly FD1793 _fdc = new();
    private byte _systemPort = 0x80;

    public KayproBus()
    {
        Video = new KayproVideo();
        Sio = new KayproSio();
        Pio = new KayproPio();
        InterruptLines = new InterruptLines();
        Video.Reset();
    }

    public KayproVideo Video { get; }
    public KayproSio Sio { get; }
    public KayproPio Pio { get; }
    public FD1793 Fdc => _fdc;
    public InterruptLines InterruptLines { get; }
    public bool RomEnabled => (_systemPort & 0x80) != 0;
    public byte SystemPortValue => _systemPort;

    public byte Read(ushort address)
    {
        if (address is >= VideoBase and < VideoBase + KayproVideo.MemorySize)
            return Video.Read((ushort)(address - VideoBase));
        if (RomEnabled && address <= RomTop)
            return _rom[address];
        return _ram[address];
    }

    public void Write(ushort address, byte value)
    {
        if (address is >= VideoBase and < VideoBase + KayproVideo.MemorySize)
        {
            Video.Write((ushort)(address - VideoBase), value);
            return;
        }

        _ram[address] = value;
    }

    public byte ReadMemory(ushort address) => Read(address);
    public void WriteMemory(ushort address, byte value) => Write(address, value);

    public byte ReadPort(byte port) => ReadPort((ushort)port);

    public byte ReadPort(ushort port)
    {
        port &= 0x00FF;
        return port switch
        {
            >= 0x04 and <= 0x07 => Sio.Read(port),
            >= 0x10 and <= 0x13 => _fdc.Read((byte)(port - 0x10)),
            SystemPort => _systemPort,
            >= 0x08 and <= 0x0B => Pio.Read(port),
            _ => 0xFF,
        };
    }

    public void WritePort(byte port, byte value) => WritePort((ushort)port, value);

    public void WritePort(ushort port, byte value)
    {
        port &= 0x00FF;
        switch (port)
        {
            case >= 0x04 and <= 0x07:
                Sio.Write(port, value);
                break;
            case >= 0x10 and <= 0x13:
                _fdc.Write((byte)(port - 0x10), value);
                break;
            case >= 0x08 and <= 0x0B:
                Pio.Write(port, value);
                break;
            case SystemPort:
                _systemPort = value;
                _fdc.DriveSelect = (byte)(value & 0x03);
                break;
        }
    }

    public void LoadRom(ReadOnlySpan<byte> rom)
    {
        if (rom.Length != _rom.Length)
            throw new ArgumentException("Kaypro II monitor ROM must be 2 KiB.", nameof(rom));
        rom.CopyTo(_rom);
    }

    public void InsertDisk(int drive, IFD1793DiskImage? disk) => _fdc.InsertDisk(drive, disk);

    public void Tick(int tStates)
    {
        _fdc.Tick(tStates);
        InterruptLines.SetNmi(_fdc.IntrqAsserted || _fdc.DrqAsserted);
    }

    public void Reset()
    {
        Array.Clear(_ram);
        _systemPort = 0x80;
        _fdc.Reset();
        Sio.Reset();
        Pio.Reset();
        InterruptLines.Clear();
        Video.Reset();
    }
}
