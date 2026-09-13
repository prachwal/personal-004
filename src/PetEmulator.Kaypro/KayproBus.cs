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
    private readonly KayproFdcWiring _fdcWiring;
    private byte _systemPort = 0x80;
    private bool _fdcNmiPulseActive;

    public KayproBus()
    {
        Video = new KayproVideo();
        // The monitor transfers one byte per NMI/INI round-trip.  The generic
        // controller default is a physical byte window; Kaypro's board glue
        // must include the Z80 NMI/RET/INI service latency in that window.
        _fdcWiring = new KayproFdcWiring(new FD1793(dataByteTStates: 10_000));
        Sio = new KayproSioWiring();
        Pio = new KayproPioWiring();
        Pio.SystemPortChanged += value =>
        {
            _systemPort = value;
            _fdcWiring.WriteSystemPort(value);
        };
        PioInterruptChain = new Z80PioInterruptChain(Pio.PioG, Pio.PioS);
        InterruptLines = new InterruptLines();
        Video.Reset();
    }

    public KayproVideo Video { get; }
    public KayproSioWiring Sio { get; }
    public KayproPioWiring Pio { get; }
    public Z80PioInterruptChain PioInterruptChain { get; }
    public FD1793 Fdc => _fdcWiring.Controller;
    public KayproFdcWiring FdcWiring => _fdcWiring;
    public InterruptLines InterruptLines { get; }
    public bool RomEnabled => (_systemPort & 0x80) != 0;
    public byte SystemPortValue => _systemPort;
    public ulong FdcNmiPulseCount { get; private set; }
    public bool FdcNmiPending => Fdc.DrqAsserted || Fdc.IntrqAsserted;

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
            0x10 => _fdcWiring.Controller.Read(0),
            >= 0x11 and <= 0x13 => _fdcWiring.Controller.Read((byte)(port - 0x10)),
            >= KayproPioWiring.PioGBasePort and < KayproPioWiring.PioGBasePort + 4 => Pio.Read(port),
            >= KayproPioWiring.PioSBasePort and < KayproPioWiring.PioSBasePort + 4 => Pio.Read(port),
            _ => 0xFF,
        };
    }

    public byte AcknowledgeInterrupt()
    {
        var acknowledged = Sio.TryAcknowledgeInterrupt(out var vector);
        if (!acknowledged)
            acknowledged = PioInterruptChain.TryAcknowledgeInterrupt(out vector);
        InterruptLines.SetInt(Sio.InterruptRequested || PioInterruptChain.InterruptRequested);
        return acknowledged ? vector : (byte)0xFF;
    }

    public void NotifyInterruptReturn()
    {
        Sio.NotifyReti();
        PioInterruptChain.NotifyReti();
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
                _fdcWiring.Controller.Write((byte)(port - 0x10), value);
                break;
            case >= KayproPioWiring.PioGBasePort and < KayproPioWiring.PioGBasePort + 4:
            case >= KayproPioWiring.PioSBasePort and < KayproPioWiring.PioSBasePort + 4:
                Pio.Write(port, value);
                break;
        }
    }

    public void LoadRom(ReadOnlySpan<byte> rom)
    {
        if (rom.Length != _rom.Length)
            throw new ArgumentException("Kaypro II monitor ROM must be 2 KiB.", nameof(rom));
        rom.CopyTo(_rom);
    }

    public void InsertDisk(int drive, IFD1793DiskImage? disk) => _fdcWiring.Controller.InsertDisk(drive, disk);

    public void Tick(int tStates, bool cpuHalted = false)
    {
        Sio.Tick(tStates);
        _fdcWiring.Tick(tStates);
        InterruptLines.SetInt(Sio.InterruptRequested || PioInterruptChain.InterruptRequested);

        if (_fdcNmiPulseActive)
        {
            _fdcNmiPulseActive = false;
            InterruptLines.SetNmi(false);
        }

        // Kaypro wires both FD179x DRQ and INTRQ to the Z80 NMI input.  Use
        // the current hardware levels here instead of replaying a generic
        // InterruptSequence event: a queued event can outlive the condition
        // that caused it and wake the ROM's HALT/INI loop without a byte.
        if (cpuHalted && (Fdc.DrqAsserted || Fdc.IntrqAsserted))
        {
            _fdcNmiPulseActive = true;
            FdcNmiPulseCount++;
            InterruptLines.SetNmi(true);
        }
        else if (!_fdcNmiPulseActive)
        {
            InterruptLines.SetNmi(false);
        }
    }

    public void Reset()
    {
        Array.Clear(_ram);
        _systemPort = 0x80;
        _fdcWiring.Reset();
        _fdcNmiPulseActive = false;
        FdcNmiPulseCount = 0;
        Sio.Reset();
        Pio.Reset();
        InterruptLines.Clear();
        Video.Reset();
    }
}
