using PetEmulator.Core;
using PetEmulator.Cpu6502;

namespace PetEmulator.Cpu6502.Variants;

/// <summary>
/// Procesor MOS 6510 używany w komputerze Commodore 64.
/// Jest wariantem MOS 6502 z pełną obsługą trybu BCD (Decimal Mode) i bug'iem JMP indirect.
/// Port I/O ($00/$01) i mapowanie pamięci specyficzne dla Commodore'a są obawą warstwy magistrali pamięci, nie procesora CPU.
/// </summary>
public sealed class Cpu6502Commodore6510 : Cpu6502
{
    /// <summary>
    /// Inicjalizuje nowy egzemplarz procesora MOS 6510 (Commodore 64).
    /// </summary>
    /// <param name="memory">Interfejs magistrali pamięci.</param>
    /// <param name="opcodeTable">Opcjonalna tablica opcode wariantu.</param>
    /// <param name="clock">Opcjonalna instancja IClock (uses default internal clock if null).</param>
    public Cpu6502Commodore6510(IMemoryBus memory, OpcodeTable? opcodeTable = null, IClock? clock = null)
        : base(memory, OpcodeTables.CreateCommodore6510Variant(opcodeTable), clock)
    {
    }
}
