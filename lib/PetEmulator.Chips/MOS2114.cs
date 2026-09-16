using PetEmulator.Core;

namespace PetEmulator.Chips;

/// <summary>The VIC-20's separate 4-bit color RAM ($9400-$97FF on real hardware) - each nibble
/// picks one of 16 colors for the matching screen-RAM character cell. Real hardware wires only
/// the low 4 data-bus lines here; the high nibble reads back as open bus on real silicon, but this
/// repo's <see cref="PetEmulator.Core.IMemoryBus"/> has no open-bus concept below the top-level
/// decoder (see <c>PetEmulator.Pet.PetMemoryBus</c>'s own <c>OpenBus</c> handling for that layer) -
/// masking the write instead (storing only the low nibble) gets the same observable behavior a
/// KERNAL/BASIC read ever sees.</summary>
public sealed class MOS2114 : IMemoryMappedDevice
{
    private readonly ushort _baseAddress;
    private readonly byte[] _data;

    public MOS2114(string name = "Color RAM", ushort baseAddress = 0, uint size = 0x0400)
    {
        if (size == 0 || size > int.MaxValue || (ulong)baseAddress + size > 0x1_0000)
            throw new ArgumentOutOfRangeException(nameof(size), "The memory-mapped RAM must fit in the 16-bit address space.");

        Name = name;
        _baseAddress = baseAddress;
        _data = new byte[(int)size];
    }

    public string Name { get; }

    public uint Length => (uint)_data.Length;

    public byte Read(ushort address) => (byte)(_data[GetOffset(address)] & 0x0F);

    public void Write(ushort address, byte value) => _data[GetOffset(address)] = (byte)(value & 0x0F);

    public void Reset() => Array.Clear(_data);

    public MOS2114Snapshot CaptureState() => new() { Data = _data.ToArray() };

    public void RestoreState(MOS2114Snapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Data.Length != _data.Length)
            throw new ArgumentException($"Expected {_data.Length} bytes of color RAM.", nameof(state));
        state.Data.CopyTo(_data, 0);
    }

    public void Tick(ulong cycles) { }

    private int GetOffset(ushort address)
    {
        var offset = address - _baseAddress;
        if (address < _baseAddress || offset >= _data.Length)
            throw new ArgumentOutOfRangeException(nameof(address), address, "Address is outside the mapped RAM range.");

        return offset;
    }
}
