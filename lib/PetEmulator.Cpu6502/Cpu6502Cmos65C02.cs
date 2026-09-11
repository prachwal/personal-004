using PetEmulator.Core;
using PetEmulator.Cpu6502;

namespace PetEmulator.Cpu6502.Variants;

/// <summary>
/// WDC 65C02 CPU implementation.
/// The 65C02 is an improved version of the MOS 6502 with additional instructions and quirks fixes.
/// </summary>
public sealed class Cpu6502Cmos65C02 : Cpu6502
{
    /// <summary>
    /// Initializes a new instance of the WDC 65C02 CPU.
    /// </summary>
    /// <param name="memory">Memory bus interface for the CPU.</param>
    /// <param name="opcodeTable">Optional custom opcode table (uses default 65C02 table if not provided).</param>
    /// <param name="clock">Optional injected IClock instance (uses default internal clock if null).</param>
    public Cpu6502Cmos65C02(IMemoryBus memory, OpcodeTable? opcodeTable = null, IClock? clock = null)
        : base(memory, OpcodeTables.CreateCmos65C02Variant(opcodeTable), clock)
    {
    }
}
