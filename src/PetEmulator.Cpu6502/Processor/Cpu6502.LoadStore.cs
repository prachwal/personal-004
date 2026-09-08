namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Implementacja instrukcji Load (LDA, LDX, LDY)

    // LDA - Load Accumulator
    private void LdaImm() { _a = _memory.Read(AddrImmediate()); SetNZ(_a); }
    private void LdaZp() { _a = _memory.Read(AddrZp()); SetNZ(_a); }
    private void LdaZpX() { _a = _memory.Read(AddrZpX()); SetNZ(_a); }
    private void LdaAbs() { _a = _memory.Read(AddrAbs()); SetNZ(_a); }
    private void LdaAbsX() { _a = _memory.Read(AddrAbsX(out _pageCrossed)); SetNZ(_a); }
    private void LdaAbsY() { _a = _memory.Read(AddrAbsY(out _pageCrossed)); SetNZ(_a); }
    private void LdaIndX() { _a = _memory.Read(AddrIndX()); SetNZ(_a); }
    private void LdaIndY() { _a = _memory.Read(AddrIndY(out _pageCrossed)); SetNZ(_a); }

    // LDX - Load X Register
    private void LdxImm() { var (_, val, _) = Imm(); _x = val; SetNZ(_x); }
    private void LdxZp() { var (_, val, _) = Zp(); _x = val; SetNZ(_x); }
    private void LdxZpY() { var (_, val, _) = ZpY(); _x = val; SetNZ(_x); }
    private void LdxAbs() { var (_, val, _) = Abs(); _x = val; SetNZ(_x); }
    private void LdxAbsY() { var (_, val, _) = AbsY(); _x = val; SetNZ(_x); }

    // LDY - Load Y Register
    private void LdyImm() { var (_, val, _) = Imm(); _y = val; SetNZ(_y); }
    private void LdyZp() { var (_, val, _) = Zp(); _y = val; SetNZ(_y); }
    private void LdyZpX() { var (_, val, _) = ZpX(); _y = val; SetNZ(_y); }
    private void LdyAbs() { var (_, val, _) = Abs(); _y = val; SetNZ(_y); }
    private void LdyAbsX() { var (_, val, _) = AbsX(); _y = val; SetNZ(_y); }

    #endregion

    #region Implementacja instrukcji Store (STA, STX, STY)

    // STA - Store Accumulator
    private void StaZp() { var (addr, _, _) = Zp(); _memory.Write(addr, _a); }
    private void StaZpX() { var (addr, _, _) = ZpX(); _memory.Write(addr, _a); }
    private void StaAbs() { var (addr, _, _) = Abs(); _memory.Write(addr, _a); }
    private void StaAbsX() { var (addr, _, _) = AbsX(); _memory.Write(addr, _a); }
    private void StaAbsY() { var (addr, _, _) = AbsY(); _memory.Write(addr, _a); }
    private void StaIndX() { var (addr, _, _) = IndX(); _memory.Write(addr, _a); }
    private void StaIndY() { var (addr, _, _) = IndY(); _memory.Write(addr, _a); }

    // STX - Store X Register
    private void StxZp() { var (addr, _, _) = Zp(); _memory.Write(addr, _x); }
    private void StxZpY() { var (addr, _, _) = ZpY(); _memory.Write(addr, _x); }
    private void StxAbs() { var (addr, _, _) = Abs(); _memory.Write(addr, _x); }

    // STY - Store Y Register
    private void StyZp() { var (addr, _, _) = Zp(); _memory.Write(addr, _y); }
    private void StyZpX() { var (addr, _, _) = ZpX(); _memory.Write(addr, _y); }
    private void StyAbs() { var (addr, _, _) = Abs(); _memory.Write(addr, _y); }

    #endregion
}
