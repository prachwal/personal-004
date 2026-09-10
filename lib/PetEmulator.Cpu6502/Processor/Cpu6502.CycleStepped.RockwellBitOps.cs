namespace PetEmulator.Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    /// <summary>
    /// RMB0-7 / SMB0-7 (Rockwell/WDC 65C02S) — reset/set bit N of a zero-page byte.
    /// RMB format: 0x07, 0x17, 0x27, 0x37, 0x47, 0x57, 0x67, 0x77 (bit 0-7 reset)
    /// SMB format: 0x87, 0x97, 0xA7, 0xB7, 0xC7, 0xD7, 0xE7, 0xF7 (bit 0-7 set)
    /// </summary>
    internal void ExecuteRockwellBitCycle(byte opcode, byte cycle)
    {
        if (cycle == 0)
        {
            var (addr, value, _) = Zp();
            int bit = (opcode >> 4) & 0x07;
            byte mask = (byte)(1 << bit);
            byte newValue = (opcode & 0x80) != 0 ? (byte)(value | mask) : (byte)(value & ~mask);
            _memory.Write(addr, value);      // dummy write of the original value (RMW convention)
            _memory.Write(addr, newValue);   // real write of the modified value
        }
        if (cycle >= GetEffectiveInstructionCycles(opcode) - 1)
            _sync = true;
    }

    /// <summary>
    /// BBR0-7 / BBS0-7 (Rockwell/WDC 65C02S) — branch if bit N of a zero-page byte is reset/set.
    /// BBR format: 0x0F, 0x1F, 0x2F, 0x3F, 0x4F, 0x5F, 0x6F, 0x7F (branch if bit 0-7 reset)
    /// BBS format: 0x8F, 0x9F, 0xAF, 0xBF, 0xCF, 0xDF, 0xEF, 0xFF (branch if bit 0-7 set)
    /// </summary>
    internal void ExecuteRockwellBranchCycle(byte opcode, byte cycle)
    {
        if (cycle == 0)
        {
            var (_, value, _) = Zp();
            int bit = (opcode >> 4) & 0x07;
            bool bitSet = ((value >> bit) & 1) != 0;
            _branchTaken = (opcode & 0x80) != 0 ? bitSet : !bitSet;
            byte offset = _memory.Read(_pc++);
            _tempAddr = (ushort)(_pc + (sbyte)offset);
            if (_branchTaken)
                _pc = _tempAddr;
        }
        byte total = (byte)(5 + (_branchTaken ? 1 : 0));
        if (cycle >= total - 1)
            _sync = true;
    }

    /// <summary>
    /// WAI/STP (WDC 65C02S) — wait for interrupt / stop the clock.
    /// WAI (0xCB): Pause execution until an unmasked interrupt is received.
    /// STP (0xDB): Stop the processor until a reset.
    /// </summary>
    internal void ExecuteWaiStpCycle(byte opcode, byte cycle)
    {
        if (cycle == 0)
        {
            if (opcode == 0xCB) _waitingForInterrupt = true; // WAI
            else _halted = true; // STP
        }
        if (cycle >= GetEffectiveInstructionCycles(opcode) - 1)
            _sync = true;
    }
}
