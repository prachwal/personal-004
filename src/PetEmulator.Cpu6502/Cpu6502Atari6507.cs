using PetEmulator.Core;

namespace Cpu6502.Variants;

/// <summary>
/// Procesor MOS 6507 używany w konsoli Atari 2600.
/// Jest wariantem MOS 6502 z pełną obsługą trybu BCD (Decimal Mode) i bug'iem JMP indirect.
/// Magistrala adresowa 13-bitowa (A0-A12) jest obawą warstwy magistrali pamięci, nie procesora CPU.
/// Maskowanie adresów powyżej 13 bitów odbywa się w implementacji magistrali pamięci.
/// </summary>
public sealed class Cpu6502Atari6507 : Cpu6502
{
    /// <summary>
    /// Inicjalizuje nowy egzemplarz procesora MOS 6507 (Atari 2600).
    /// </summary>
    /// <param name="memory">Interfejs magistrali pamięci.</param>
    /// <param name="opcodeTable">Opcjonalna tablica opcode wariantu.</param>
    public Cpu6502Atari6507(IMemoryBus memory, OpcodeTable? opcodeTable = null)
        : base(memory, OpcodeTables.CreateAtari6507Variant(opcodeTable))
    {
    }
}
