// <copyright file="Cpu6502.Bcd.cs" company="6502 Emulator">
// Copyright © 2026 6502 Emulator. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region BCD Arithmetic Operations

    /// <summary>
    /// ExecuteAdc with BCD (Decimal Mode)
    /// ADC = A + M + C in BCD
    /// </summary>
    /// <param name="operand">Operand from memory.</param>
    private void ExecuteAdcBcd(byte operand)
    {
        byte oldA = _a;
        byte al = (byte)(_a & 0x0F);      // Low nibble of A
        byte ml = (byte)(operand & 0x0F);  // Low nibble of operand
        byte ah = (byte)(_a >> 4);        // High nibble of A
        byte mh = (byte)(operand >> 4);   // High nibble of operand
        bool c = (_p & FlagC) != 0;       // Carry flag

        // Binary sum
        ushort sum = (ushort)(_a + operand + (c ? 1 : 0));
        ushort adjusted = sum;
        bool carry = false;

        // BCD correction must use the full adjusted sum, not a wrapped byte.
        if ((al + ml + (c ? 1 : 0)) > 9)
            adjusted += 6;
        if (adjusted > 0x99)
        {
            adjusted += 0x60;
            carry = true;
        }

        byte result = (byte)(adjusted & 0xFF);

        // Overflow must be derived from the pre-adjusted binary sum.
        bool overflow = ((oldA ^ (byte)sum) & (operand ^ (byte)sum) & 0x80) != 0;

        _a = result;

        // Set flags based on BCD-corrected result (NMOS 6502 behavior)
        SetFlag(FlagN, (result & 0x80) != 0);
        SetFlag(FlagV, overflow);
        SetFlag(FlagZ, result == 0);
        SetFlag(FlagC, carry);
    }

    /// <summary>
    /// ExecuteSbc with BCD (Decimal Mode)
    /// SBC = A - M - ~C in BCD
    /// </summary>
    /// <param name="operand">Operand from memory.</param>
    private void ExecuteSbcBcd(byte operand)
    {
        byte oldA = _a;
        bool c = (_p & FlagC) != 0;  // Carry flag (inverted for borrow)

        // Binary subtraction via complement
        ushort sum = (ushort)(_a + (byte)(~operand) + (c ? 1 : 0));
        byte result = (byte)(sum & 0xFF);
        bool carry = sum > 0xFF;

        // BCD correction for subtraction
        byte al = (byte)(_a & 0x0F);
        byte ml = (byte)(operand & 0x0F);

        // Check if low nibble borrow is needed
        int lowDiff = al - ml - (c ? 0 : 1);
        if (lowDiff < 0 || lowDiff > 9)
        {
            result -= 6;
        }

        // Check if high nibble borrow is needed
        if (!carry)
        {
            result -= 0x60;
        }

        // Overflow must be derived from the binary sum before decimal correction.
        byte notOperand = (byte)~operand;
        bool overflow = ((oldA ^ (byte)sum) & (notOperand ^ (byte)sum) & 0x80) != 0;

        _a = result;

        // Set flags based on BCD-corrected result
        SetFlag(FlagN, (result & 0x80) != 0);
        SetFlag(FlagV, overflow);
        SetFlag(FlagZ, result == 0);
        SetFlag(FlagC, carry);
    }

    #endregion
}
