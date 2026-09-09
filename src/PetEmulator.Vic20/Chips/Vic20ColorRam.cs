using PetEmulator.Core;

namespace PetEmulator.Vic20.Chips;

/// <summary>The VIC-20's separate 4-bit color RAM ($9400-$97FF on real hardware) - each nibble
/// picks one of 16 colors for the matching screen-RAM character cell. Real hardware wires only
/// the low 4 data-bus lines here; the high nibble reads back as open bus on real silicon, but this
/// repo's <see cref="PetEmulator.Core.IMemoryBus"/> has no open-bus concept below the top-level
/// decoder (see <c>PetEmulator.Pet.PetMemoryBus</c>'s own <c>OpenBus</c> handling for that layer) -
/// masking the write instead (storing only the low nibble) gets the same observable behavior a
/// KERNAL/BASIC read ever sees.</summary>
public sealed class Vic20ColorRam : IMemoryMappedDevice
{
    private readonly ushort _baseAddress;
    private readonly byte[] _data;

    public Vic20ColorRam(string name = "Color RAM", ushort baseAddress = 0, uint size = 0x0400)
    {
        Name = name;
        _baseAddress = baseAddress;
        _data = new byte[size];
    }

    public string Name { get; }

    public uint Length => (uint)_data.Length;

    public byte Read(ushort address) => (byte)(_data[address - _baseAddress] & 0x0F);

    public void Write(ushort address, byte value) => _data[address - _baseAddress] = (byte)(value & 0x0F);

    public void Reset() => Array.Clear(_data);

    public void Tick(ulong cycles) { }
}
