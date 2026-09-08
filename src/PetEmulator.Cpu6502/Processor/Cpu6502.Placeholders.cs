namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Placeholder methods (dla brakujących opcode'ów)

    /// <summary>
    /// BRK - Software Interrupt.
    /// Zaimplementowane w Cpu6502.Interrupts.cs
    /// </summary>
    // private void Brk() => throw new NotImplementedException("BRK not implemented");

    /// <summary>
    /// ORA (ind,X) - OR with Accumulator, Indirect X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraIndXPlaceholder() => throw new NotImplementedException("ORA (ind,X) not implemented");

    /// <summary>
    /// ORA zp - OR with Accumulator, Zero Page.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraZpPlaceholder() => throw new NotImplementedException("ORA zp not implemented");

    /// <summary>
    /// ORA zp,X - OR with Accumulator, Zero Page X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraZpXPlaceholder() => throw new NotImplementedException("ORA zp,X not implemented");

    /// <summary>
    /// ORA abs - OR with Accumulator, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraAbsPlaceholder() => throw new NotImplementedException("ORA abs not implemented");

    /// <summary>
    /// ORA abs,X - OR with Accumulator, Absolute X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraAbsXPlaceholder() => throw new NotImplementedException("ORA abs,X not implemented");

    /// <summary>
    /// ORA abs,Y - OR with Accumulator, Absolute Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraAbsYPlaceholder() => throw new NotImplementedException("ORA abs,Y not implemented");

    /// <summary>
    /// ORA (ind),Y - OR with Accumulator, Indirect Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraIndYPlaceholder() => throw new NotImplementedException("ORA (ind),Y not implemented");

    /// <summary>
    /// ASL A - Arithmetic Shift Left, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AslAccPlaceholder() => throw new NotImplementedException("ASL A not implemented");

    /// <summary>
    /// ORA #imm - OR with Accumulator, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraImmPlaceholder() => throw new NotImplementedException("ORA #imm not implemented");

    /// <summary>
    /// BPL rel - Branch if Plus (N=0).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BplRelPlaceholder() => throw new NotImplementedException("BPL rel not implemented");

    /// <summary>
    /// JSR abs - Jump to Subroutine, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void JsrAbsPlaceholder() => throw new NotImplementedException("JSR abs not implemented");

    /// <summary>
    /// AND (ind,X) - AND with Accumulator, Indirect X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndIndXPlaceholder() => throw new NotImplementedException("AND (ind,X) not implemented");

    /// <summary>
    /// AND zp - AND with Accumulator, Zero Page.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndZpPlaceholder() => throw new NotImplementedException("AND zp not implemented");

    /// <summary>
    /// AND zp,X - AND with Accumulator, Zero Page X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndZpXPlaceholder() => throw new NotImplementedException("AND zp,X not implemented");

    /// <summary>
    /// AND abs - AND with Accumulator, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndAbsPlaceholder() => throw new NotImplementedException("AND abs not implemented");

    /// <summary>
    /// AND abs,X - AND with Accumulator, Absolute X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndAbsXPlaceholder() => throw new NotImplementedException("AND abs,X not implemented");

    /// <summary>
    /// AND abs,Y - AND with Accumulator, Absolute Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndAbsYPlaceholder() => throw new NotImplementedException("AND abs,Y not implemented");

    /// <summary>
    /// AND (ind),Y - AND with Accumulator, Indirect Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndIndYPlaceholder() => throw new NotImplementedException("AND (ind),Y not implemented");

    /// <summary>
    /// ROL A - Rotate Left, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void RolAccPlaceholder() => throw new NotImplementedException("ROL A not implemented");

    /// <summary>
    /// AND #imm - AND with Accumulator, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndImmPlaceholder() => throw new NotImplementedException("AND #imm not implemented");

    /// <summary>
    /// BMI rel - Branch if Minus (N=1).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BmiRelPlaceholder() => throw new NotImplementedException("BMI rel not implemented");

    /// <summary>
    /// RTI - Return from Interrupt.
    /// Zaimplementowane w Cpu6502.Interrupts.cs
    /// </summary>
    // private void Rti() => throw new NotImplementedException("RTI not implemented");

    /// <summary>
    /// EOR (ind,X) - Exclusive OR with Accumulator, Indirect X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorIndXPlaceholder() => throw new NotImplementedException("EOR (ind,X) not implemented");

    /// <summary>
    /// EOR zp - Exclusive OR with Accumulator, Zero Page.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorZpPlaceholder() => throw new NotImplementedException("EOR zp not implemented");

    /// <summary>
    /// EOR zp,X - Exclusive OR with Accumulator, Zero Page X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorZpXPlaceholder() => throw new NotImplementedException("EOR zp,X not implemented");

    /// <summary>
    /// EOR abs - Exclusive OR with Accumulator, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorAbsPlaceholder() => throw new NotImplementedException("EOR abs not implemented");

    /// <summary>
    /// EOR abs,X - Exclusive OR with Accumulator, Absolute X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorAbsXPlaceholder() => throw new NotImplementedException("EOR abs,X not implemented");

    /// <summary>
    /// EOR abs,Y - Exclusive OR with Accumulator, Absolute Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorAbsYPlaceholder() => throw new NotImplementedException("EOR abs,Y not implemented");

    /// <summary>
    /// EOR (ind),Y - Exclusive OR with Accumulator, Indirect Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorIndYPlaceholder() => throw new NotImplementedException("EOR (ind),Y not implemented");

    /// <summary>
    /// LSR A - Logical Shift Right, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void LsrAccPlaceholder() => throw new NotImplementedException("LSR A not implemented");

    /// <summary>
    /// EOR #imm - Exclusive OR with Accumulator, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorImmPlaceholder() => throw new NotImplementedException("EOR #imm not implemented");

    /// <summary>
    /// BVC rel - Branch if Overflow Clear (V=0).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BvcRelPlaceholder() => throw new NotImplementedException("BVC rel not implemented");

    /// <summary>
    /// RTS - Return from Subroutine.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void RtsPlaceholder() => throw new NotImplementedException("RTS not implemented");

    /// <summary>
    /// ROR A - Rotate Right, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void RorAccPlaceholder() => throw new NotImplementedException("ROR A not implemented");

    /// <summary>
    /// NOP - No Operation.
    /// Wykonuje pustą operację (zlicza cykle).
    /// </summary>
    private void NopPlaceholder() { /* no operation */ }

    /// <summary>
    /// BVS rel - Branch if Overflow Set (V=1).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BvsRelPlaceholder() => throw new NotImplementedException("BVS rel not implemented");

    /// <summary>
    /// BCC rel - Branch if Carry Clear (C=0).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BccRelPlaceholder() => throw new NotImplementedException("BCC rel not implemented");

    /// <summary>
    /// BCS rel - Branch if Carry Set (C=1).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BcsRelPlaceholder() => throw new NotImplementedException("BCS rel not implemented");

    /// <summary>
    /// CPY #imm - Compare Y Register, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CpyImmPlaceholder() => throw new NotImplementedException("CPY #imm not implemented");

    /// <summary>
    /// CMP (ind,X) - Compare with Accumulator, Indirect X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpIndXPlaceholder() => throw new NotImplementedException("CMP (ind,X) not implemented");

    /// <summary>
    /// CMP zp - Compare with Accumulator, Zero Page.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpZpPlaceholder() => throw new NotImplementedException("CMP zp not implemented");

    /// <summary>
    /// CMP zp,X - Compare with Accumulator, Zero Page X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpZpXPlaceholder() => throw new NotImplementedException("CMP zp,X not implemented");

    /// <summary>
    /// CMP abs - Compare with Accumulator, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpAbsPlaceholder() => throw new NotImplementedException("CMP abs not implemented");

    /// <summary>
    /// CMP abs,X - Compare with Accumulator, Absolute X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpAbsXPlaceholder() => throw new NotImplementedException("CMP abs,X not implemented");

    /// <summary>
    /// CMP abs,Y - Compare with Accumulator, Absolute Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpAbsYPlaceholder() => throw new NotImplementedException("CMP abs,Y not implemented");

    /// <summary>
    /// CMP (ind),Y - Compare with Accumulator, Indirect Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpIndYPlaceholder() => throw new NotImplementedException("CMP (ind),Y not implemented");

    /// <summary>
    /// DEC A - Decrement, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void DecAccPlaceholder() => throw new NotImplementedException("DEC A not implemented");

    /// <summary>
    /// CMP #imm - Compare with Accumulator, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpImmPlaceholder() => throw new NotImplementedException("CMP #imm not implemented");

    /// <summary>
    /// BNE rel - Branch if Not Equal (Z=0).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BneRelPlaceholder() => throw new NotImplementedException("BNE rel not implemented");

    /// <summary>
    /// CPX #imm - Compare X Register, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CpxImmPlaceholder() => throw new NotImplementedException("CPX #imm not implemented");

    /// <summary>
    /// BEQ rel - Branch if Equal (Z=1).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BeqRelPlaceholder() => throw new NotImplementedException("BEQ rel not implemented");

    #endregion
}
