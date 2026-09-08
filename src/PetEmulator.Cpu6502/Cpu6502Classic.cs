using PetEmulator.Core;

namespace Cpu6502.Variants;

/// <summary>
/// Standardowy procesor MOS 6502 (NMOS) — klasyczny wariant.
/// Obsługuje tryb BCD (Decimal Mode) dla instrukcji ADC i SBC.
/// </summary>
public sealed class Cpu6502Classic : Cpu6502
{
    /// <summary>
    /// Inicjalizuje nowy egzemplarz klasycznego procesora MOS 6502.
    /// </summary>
    /// <param name="memory">Interfejs magistrali pamięci.</param>
    /// <param name="opcodeTable">Opcjonalna tablica opcode wariantu.</param>
    /// <param name="clock">Opcjonalna instancja IClock (uses default internal clock if null).</param>
    public Cpu6502Classic(IMemoryBus memory, OpcodeTable? opcodeTable = null, IClock? clock = null)
        : base(memory, OpcodeTables.CreateNmosVariant(opcodeTable), clock)
    {
    }
}
