using PetEmulator.Core;

namespace PetEmulator.Chips;

/// <summary>
/// MOS 6702 copy-protection dongle used by the SuperPET. The device has no
/// published data sheet; this model follows the functional description and
/// the VICE reference model.
/// </summary>
public sealed class MOS6702 : IMemoryMappedDevice
{
    private readonly ushort _baseAddress;
    private readonly MOS6702Variant _variant;
    private readonly int[] _leftmostBits;
    private readonly int[] _shiftRegisters = new int[8];
    private byte _output;
    private byte _lastAcceptedInput;
    private bool _waitingForOdd;

    public MOS6702(ushort baseAddress = 0xEFE0, string name = "MOS6702", MOS6702Variant? variant = null)
    {
        if ((uint)baseAddress + Length > 0x1_0000)
            throw new ArgumentOutOfRangeException(nameof(baseAddress), "The MOS 6702 must fit in the 16-bit address space.");

        _baseAddress = baseAddress;
        Name = name;
        _variant = variant ?? MOS6702Variant.Standard;
        _leftmostBits = _variant.ShiftRegisterLengths.Select(length => 1 << (length - 1)).ToArray();
        Reset();
    }

    public string Name { get; }

    /// <summary>The four addresses are aliases of the dongle data register.</summary>
    public uint Length => 4;

    public byte Output => _output;

    public MOS6702Variant Variant => _variant;

    public Action<BusAccess>? Observer { get; set; }

    public void Tick(ulong cycles)
    {
        // The dongle is advanced by accepted writes, not by clock cycles.
    }

    public void Reset()
    {
        Array.Clear(_shiftRegisters);
        for (var bit = 0; bit < _shiftRegisters.Length; bit++)
        {
            // This is the reset state used by the reverse-engineered VICE
            // model; it preserves the documented initial output $D6.
            if (((_variant.InitialValue | 1) & (1 << bit)) != 0)
                _shiftRegisters[bit] = _leftmostBits[bit];
        }

        _output = _variant.InitialValue;
        _lastAcceptedInput = 1;
        _waitingForOdd = false;
    }

    public byte Read(ushort address)
    {
        ValidateAddress(address);
        Observer?.Invoke(new BusAccess(IsWrite: false, address, _output));
        return _output;
    }

    public void Write(ushort address, byte value)
    {
        ValidateAddress(address);
        if ((value & 1) == (_waitingForOdd ? 1 : 0))
        {
            if (_waitingForOdd)
                AcceptOdd(value);

            _waitingForOdd = !_waitingForOdd;
        }

        Observer?.Invoke(new BusAccess(IsWrite: true, address, value));
    }

    private void AcceptOdd(byte input)
    {
        var changed = (byte)(_lastAcceptedInput ^ input);
        var value = _output;
        var mask = 0x80;

        for (var bit = 7; bit >= 0; bit--, mask >>= 1)
        {
            if ((changed & mask) != 0)
                _shiftRegisters[bit] ^= _leftmostBits[bit];

            if ((_shiftRegisters[bit] & 1) != 0)
            {
                value ^= (byte)mask;
                _shiftRegisters[bit] |= _leftmostBits[bit] << 1;
            }

            _shiftRegisters[bit] >>= 1;
        }

        _lastAcceptedInput = input;
        _output = value;
    }

    private void ValidateAddress(ushort address)
    {
        if (address < _baseAddress || address >= _baseAddress + Length)
            throw new ArgumentOutOfRangeException(nameof(address), address, "Address is outside the MOS 6702 range.");
    }
}
