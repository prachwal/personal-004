namespace Cpu6502.Tests;

/// <summary>
/// Prosta implementacja magistrali pamięci dla testów.
/// Reprezentuje 64KB pamięci RAM.
/// </summary>
public class FlatMemory : IMemoryBus
{
    private readonly byte[] ram = new byte[65536];

    /// <summary>
    /// Odczytuje bajt z podanego adresu.
    /// </summary>
    /// <param name="address">Adres 16-bitowy.</param>
    /// <returns>Wartość z pamięci.</returns>
    public byte Read(ushort address) => ram[address];

    /// <summary>
    /// Zapisuje bajt pod podany adres.
    /// </summary>
    /// <param name="address">Adres 16-bitowy.</param>
    /// <param name="value">Wartość do zapisania.</param>
    public void Write(ushort address, byte value) => ram[address] = value;

    /// <summary>
    /// Ładuje dane ROM do pamięci od podanego adresu.
    /// </summary>
    /// <param name="startAddress">Adres początkowy.</param>
    /// <param name="data">Dane do załadowania.</param>
    public void LoadRom(ushort startAddress, byte[] data)
    {
        Array.Copy(data, 0, ram, startAddress, data.Length);
    }
}
