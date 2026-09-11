namespace PetEmulator.Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// 65C02 zero page indirect addressing mode (zp) operations.
/// </summary>
public partial class Cpu6502
{
    #region ORA, AND, EOR, ADC, STA, LDA, CMP, SBC with (zp) addressing (65C02)

    /// <summary>
    /// OraZpIndirect - Bitwise OR, Zero Page Indirect (65C02).
    /// Opcode: 0x12, Tryb: (Zero Page), Cykle: 5
    /// </summary>
    private void OraZpIndirect()
    {
        ushort addr = AddrZpIndirect();
        byte val = _memory.Read(addr);
        ExecuteOra(val);
    }

    /// <summary>
    /// AndZpIndirect - Bitwise AND, Zero Page Indirect (65C02).
    /// Opcode: 0x32, Tryb: (Zero Page), Cykle: 5
    /// </summary>
    private void AndZpIndirect()
    {
        ushort addr = AddrZpIndirect();
        byte val = _memory.Read(addr);
        ExecuteAnd(val);
    }

    /// <summary>
    /// EorZpIndirect - Bitwise EOR (XOR), Zero Page Indirect (65C02).
    /// Opcode: 0x52, Tryb: (Zero Page), Cykle: 5
    /// </summary>
    private void EorZpIndirect()
    {
        ushort addr = AddrZpIndirect();
        byte val = _memory.Read(addr);
        ExecuteEor(val);
    }

    /// <summary>
    /// AdcZpIndirect - Add with Carry, Zero Page Indirect (65C02).
    /// Opcode: 0x72, Tryb: (Zero Page), Cykle: 5
    /// </summary>
    private void AdcZpIndirect()
    {
        ushort addr = AddrZpIndirect();
        byte val = _memory.Read(addr);
        ExecuteAdc(val);
    }

    /// <summary>
    /// StaZpIndirect - Store Accumulator, Zero Page Indirect (65C02).
    /// Opcode: 0x92, Tryb: (Zero Page), Cykle: 5
    /// </summary>
    private void StaZpIndirect()
    {
        ushort addr = AddrZpIndirect();
        _memory.Write(addr, _a);
    }

    /// <summary>
    /// LdaZpIndirect - Load Accumulator, Zero Page Indirect (65C02).
    /// Opcode: 0xB2, Tryb: (Zero Page), Cykle: 5
    /// </summary>
    private void LdaZpIndirect()
    {
        ushort addr = AddrZpIndirect();
        _a = _memory.Read(addr);
        SetNZ(_a);
    }

    /// <summary>
    /// CmpZpIndirect - Compare Accumulator, Zero Page Indirect (65C02).
    /// Opcode: 0xD2, Tryb: (Zero Page), Cykle: 5
    /// </summary>
    private void CmpZpIndirect()
    {
        ushort addr = AddrZpIndirect();
        byte val = _memory.Read(addr);
        ExecuteCmp(_a, val);
    }

    /// <summary>
    /// SbcZpIndirect - Subtract with Carry, Zero Page Indirect (65C02).
    /// Opcode: 0xF2, Tryb: (Zero Page), Cykle: 5
    /// </summary>
    private void SbcZpIndirect()
    {
        ushort addr = AddrZpIndirect();
        byte val = _memory.Read(addr);
        ExecuteSbc(val);
    }

    #endregion
}
