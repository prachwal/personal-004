namespace PetEmulator.Core;

/// <summary>Flag-neutral result of an 8-bit add or subtract operation.</summary>
public readonly record struct CpuArithmeticResult(
    byte Result,
    bool Carry,
    bool HalfCarry,
    bool Overflow);

/// <summary>Arithmetic shared by the Intel 8080-compatible subset and Z80.</summary>
public static class CpuArithmetic
{
    public static CpuArithmeticResult Add8(byte left, byte right, bool carryIn = false)
    {
        var carry = carryIn ? 1 : 0;
        var wide = left + right + carry;
        var result = (byte)wide;
        return new(
            result,
            wide > byte.MaxValue,
            ((left & 0x0F) + (right & 0x0F) + carry) > 0x0F,
            ((~(left ^ right) & (left ^ result)) & 0x80) != 0);
    }

    public static CpuArithmeticResult Subtract8(byte left, byte right, bool borrowIn = false)
    {
        var borrow = borrowIn ? 1 : 0;
        var wide = left - right - borrow;
        var result = (byte)wide;
        return new(
            result,
            wide < 0,
            ((left & 0x0F) - (right & 0x0F) - borrow) < 0,
            (((left ^ right) & (left ^ result)) & 0x80) != 0);
    }

    public static CpuArithmeticResult Increment8(byte value)
        => Add8(value, 1);

    public static CpuArithmeticResult Decrement8(byte value)
        => Subtract8(value, 1);
}
