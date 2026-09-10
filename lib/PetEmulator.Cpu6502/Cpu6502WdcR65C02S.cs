using PetEmulator.Core;
using PetEmulator.Cpu6502;

namespace PetEmulator.Cpu6502.Variants;

/// <summary>
/// Rockwell R65C02 / WDC W65C02S CPU implementation.
/// The R65C02S is based on the WDC 65C02 with additional bit manipulation instructions
/// (RMB/SMB/BBR/BBS) and control instructions (WAI/STP).
/// </summary>
public sealed class Cpu6502WdcR65C02S : Cpu6502
{
    /// <summary>
    /// Initializes a new instance of the Rockwell R65C02 / WDC W65C02S CPU.
    /// </summary>
    /// <param name="memory">Memory bus interface for the CPU.</param>
    /// <param name="opcodeTable">Optional custom opcode table (uses default R65C02S table if not provided).</param>
    /// <param name="clock">Optional injected IClock instance (uses default internal clock if null).</param>
    public Cpu6502WdcR65C02S(IMemoryBus memory, OpcodeTable? opcodeTable = null, IClock? clock = null)
        : base(memory, OpcodeTables.CreateR65C02SVariant(opcodeTable), clock)
    {
    }
}
