using System;
using Cpu6502;

namespace Cpu6502.Tests.TestHelpers;

/// <summary>
/// Implementacja IMemoryBus dla testów, obsługująca pamięć RAM i ROM.
/// </summary>
public class TestMemoryBus : IMemoryBus
{
    private readonly byte[] _memory = new byte[65536];

    /// <summary>
    /// Tworzy nową instancję TestMemoryBus.
    /// </summary>
    public TestMemoryBus()
    {
        // Inicjalizacja pamięci zerami
        Array.Fill(_memory, (byte)0);
    }

    /// <summary>
    /// Ładuje dane do pamięci od podanego adresu.
    /// </summary>
    /// <param name="address">Adres docelowy (16-bitowy).</param>
    /// <param name="data">Tablica bajtów do załadowania.</param>
    public void LoadData(ushort address, byte[] data)
    {
        int offset = 0;
        for (ushort i = address; i < address + data.Length && i < 65536; i++)
        {
            _memory[i] = data[offset++];
        }
    }

    /// <summary>
    /// Ładuje dane do pamięci od podanego adresu (32-bitowy offset).
    /// </summary>
    /// <param name="address">Adres docelowy.</param>
    /// <param name="data">Tablica bajtów do załadowania.</param>
    public void LoadData(int address, byte[] data)
    {
        LoadData((ushort)address, data);
    }

    /// <summary>
    /// Odczytuje bajt z podanego adresu.
    /// </summary>
    /// <param name="address">Adres 16-bitowy (0x0000-0xFFFF).</param>
    /// <returns>Wartość odczytana z pamięci.</returns>
    public byte Read(ushort address)
    {
        return _memory[address];
    }

    /// <summary>
    /// Zapisuje bajt pod podany adres.
    /// </summary>
    /// <param name="address">Adres 16-bitowy (0x0000-0xFFFF).</param>
    /// <param name="value">Wartość do zapisania.</param>
    public void Write(ushort address, byte value)
    {
        _memory[address] = value;
    }
}
