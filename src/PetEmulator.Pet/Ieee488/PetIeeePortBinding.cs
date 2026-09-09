namespace PetEmulator.Pet.Ieee488;

/// <summary>
/// Trivial DIO pin translation over a <see cref="PetIeeeBus"/> (the bus's DIO byte is active-low
/// on the real PIA pins, hence the XOR $FF both ways).
/// </summary>
public sealed class PetIeeePortABinding(PetIeeeBus bus)
{
    public byte ReadPins() => (byte)(bus.GetCurrentDio() ^ 0xFF);
}

public sealed class PetIeeePortBBinding(PetIeeeBus bus)
{
    public byte ReadPins() => (byte)(bus.GetCurrentDio() ^ 0xFF);

    /// <summary>
    /// Matches the original verbatim: always forwards regardless of <paramref name="ddrMask"/>.
    /// </summary>
    public void WritePins(byte value, byte ddrMask) => bus.OnDioWrite((byte)(value ^ 0xFF));
}
