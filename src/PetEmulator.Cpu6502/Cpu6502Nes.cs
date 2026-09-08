using PetEmulator.Core;

namespace Cpu6502.Variants;

/// <summary>
/// Procesor Ricoh 2A03 używany w NES (Nintendo Entertainment System).
/// Jest wariantem MOS 6502 z wyłączonym trybem BCD (Decimal Mode) i poprawionym bugiem JMP indirect.
/// Flaga D (0x08) jest widoczna, ale arytmetyka ADC/SBC zawsze działa w trybie binarnym.
/// </summary>
public sealed class Cpu6502Nes : Cpu6502
{
    /// <summary>
    /// Inicjalizuje nowy egzemplarz procesora Ricoh 2A03 (NES).
    /// </summary>
    /// <param name="memory">Interfejs magistrali pamięci.</param>
    public Cpu6502Nes(IMemoryBus memory, OpcodeTable? opcodeTable = null)
        : base(memory, OpcodeTables.CreateNesVariant(opcodeTable))
    {
    }
}
