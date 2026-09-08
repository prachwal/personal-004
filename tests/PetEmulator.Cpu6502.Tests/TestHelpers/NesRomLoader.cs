using System;
using System.IO;

namespace Cpu6502.Tests.TestHelpers;

/// <summary>
/// Ładowacz plików ROM w formacie iNES.
/// Format: Nagłówek (16 bajtów) + PRG ROM (16KB na bank) + CHR ROM (8KB na bank)
/// </summary>
public static class NesRomLoader
{
    private const int HeaderSize = 16;

    /// <summary>
    /// Ładuje plik NES ROM i zwraca dane PRG ROM.
    /// </summary>
    /// <param name="romPath">Ścieżka do pliku ROM.</param>
    /// <returns>Tablica bajtów zawierająca dane PRG ROM.</returns>
    public static byte[] LoadPrgRom(string romPath)
    {
        var romData = File.ReadAllBytes(romPath);
        
        if (romData.Length < HeaderSize)
            throw new InvalidDataException("Invalid NES ROM: Too short to contain header.");

        // Sprawdź magiczne bajty (NES^Z)
        if (romData[0] != 'N' || romData[1] != 'E' || romData[2] != 'S' || romData[3] != 0x1A)
            throw new InvalidDataException("Invalid NES ROM: Missing NES header.");

        // Liczba banków PRG ROM (16KB na bank)
        int prgRomBanks = romData[4];
        int prgRomSize = prgRomBanks * 16384; // 16KB = 16384 bytes

        // Wyekstrahuj PRG ROM (zaczyna się od offset 16)
        var prgRom = new byte[prgRomSize];
        Array.Copy(romData, HeaderSize, prgRom, 0, prgRomSize);

        return prgRom;
    }
}
