using PetEmulator.Core;

namespace PetEmulator.Chips;

/// <summary>
/// Small MC146818/DS12887-compatible real-time clock.
///
/// The implementation models the register interface used by a cartridge: register-select at
/// offset 0 and register-data at offset 1, BCD/24-hour mode, update-ended interrupts and the
/// read-to-clear status register C. It advances on emulated CPU cycles, while the supplied clock
/// only provides the initial calendar value after reset.
/// </summary>
public sealed class MC146818 : IMemoryMappedDevice
{
    public const byte Seconds = 0x00;
    public const byte Minutes = 0x02;
    public const byte Hours = 0x04;
    public const byte DayOfWeek = 0x06;
    public const byte DayOfMonth = 0x07;
    public const byte Month = 0x08;
    public const byte Year = 0x09;
    public const byte RegisterA = 0x0A;
    public const byte RegisterB = 0x0B;
    public const byte RegisterC = 0x0C;
    public const byte RegisterD = 0x0D;

    public const byte UpdateEndedFlag = 0x10;
    public const byte PeriodicFlag = 0x40;
    public const byte InterruptRequestFlag = 0x80;
    public const byte UpdateEndedInterruptEnable = 0x10;
    public const byte PeriodicInterruptEnable = 0x40;
    public const byte DataModeBinary = 0x04;
    public const byte SetTime = 0x80;

    private readonly ushort _baseAddress;
    private readonly Func<DateTime> _clock;
    private readonly ulong _cyclesPerSecond;
    private DateTime _currentTime;
    private ulong _cycleRemainder;
    private ulong _periodicCycleRemainder;
    private byte _selectedRegister;
    private byte _registerA;
    private byte _registerB;
    private byte _registerC;

    public MC146818(
        string name = "MC146818 RTC",
        ushort baseAddress = 0,
        Func<DateTime>? clock = null,
        ulong cyclesPerSecond = 1_022_727)
    {
        if (cyclesPerSecond == 0)
            throw new ArgumentOutOfRangeException(nameof(cyclesPerSecond));

        Name = name;
        _baseAddress = baseAddress;
        _clock = clock ?? (() => DateTime.Now);
        _cyclesPerSecond = cyclesPerSecond;
        Reset();
    }

    public string Name { get; }

    public uint Length => 2;

    public Action<BusAccess>? Observer { get; set; }

    public bool Irq => (_registerC & InterruptRequestFlag) != 0;

    public DateTime CurrentTime => _currentTime;

    public byte Read(ushort address)
    {
        var offset = (byte)((address - _baseAddress) & 0x01);
        var value = offset == 0 ? _selectedRegister : ReadSelectedRegister();
        Observer?.Invoke(new BusAccess(IsWrite: false, address, value));
        return value;
    }

    public void Write(ushort address, byte value)
    {
        var offset = (byte)((address - _baseAddress) & 0x01);
        if (offset == 0)
            _selectedRegister = (byte)(value & 0x3F);
        else
            WriteSelectedRegister(value);

        Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
    }

    public void Reset()
    {
        _currentTime = _clock();
        _cycleRemainder = 0;
        _periodicCycleRemainder = 0;
        _selectedRegister = 0;
        _registerA = 0x20; // divider running, no periodic rate selected
        _registerB = 0x02; // 24-hour BCD mode
        _registerC = 0;
    }

    public void Tick(ulong cycles)
    {
        if ((_registerB & SetTime) != 0)
            return;

        _cycleRemainder += cycles;
        while (_cycleRemainder >= _cyclesPerSecond)
        {
            _cycleRemainder -= _cyclesPerSecond;
            _currentTime = _currentTime.AddSeconds(1);
            _registerC |= UpdateEndedFlag;
            if ((_registerB & UpdateEndedInterruptEnable) != 0)
                _registerC |= InterruptRequestFlag;
        }

        if ((_registerB & PeriodicInterruptEnable) == 0)
            return;

        var periodicPeriod = Math.Max(1UL, _cyclesPerSecond / 16);
        _periodicCycleRemainder += cycles;
        while (_periodicCycleRemainder >= periodicPeriod)
        {
            _periodicCycleRemainder -= periodicPeriod;
            _registerC |= PeriodicFlag | InterruptRequestFlag;
        }
    }

    private byte ReadSelectedRegister() => _selectedRegister switch
    {
        Seconds => Encode(_currentTime.Second),
        Minutes => Encode(_currentTime.Minute),
        Hours => Encode(_currentTime.Hour),
        DayOfWeek => Encode((int)_currentTime.DayOfWeek + 1),
        DayOfMonth => Encode(_currentTime.Day),
        Month => Encode(_currentTime.Month),
        Year => Encode(_currentTime.Year % 100),
        RegisterA => _registerA,
        RegisterB => _registerB,
        RegisterC => ReadAndClearStatus(),
        RegisterD => 0x80, // valid CMOS RAM / time
        _ => 0,
    };

    private byte ReadAndClearStatus()
    {
        var value = (byte)(_registerC | (Irq ? InterruptRequestFlag : 0));
        _registerC = 0;
        return value;
    }

    private void WriteSelectedRegister(byte value)
    {
        switch (_selectedRegister)
        {
            case RegisterA:
                _registerA = (byte)(value & 0x7F);
                break;
            case RegisterB:
                _registerB = value;
                break;
            case RegisterC:
            case RegisterD:
                break;
            case Seconds:
                if ((_registerB & SetTime) != 0)
                    _currentTime = Replace(_currentTime, seconds: Decode(value));
                break;
            case Minutes:
                if ((_registerB & SetTime) != 0)
                    _currentTime = Replace(_currentTime, minutes: Decode(value));
                break;
            case Hours:
                if ((_registerB & SetTime) != 0)
                    _currentTime = Replace(_currentTime, hours: Decode(value));
                break;
        }
    }

    private byte Encode(int value) => (_registerB & DataModeBinary) != 0 ? (byte)value : ToBcd(value);

    private byte Decode(byte value) => (_registerB & DataModeBinary) != 0
        ? value
        : (byte)((value >> 4) * 10 + (value & 0x0F));

    private static DateTime Replace(DateTime value, int? hours = null, int? minutes = null, int? seconds = null) =>
        new(value.Year, value.Month, value.Day, hours ?? value.Hour, minutes ?? value.Minute,
            seconds ?? value.Second, value.Kind);

    private static byte ToBcd(int value) => (byte)((value / 10 << 4) | (value % 10));
}
