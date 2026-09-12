using System.Numerics;

namespace PetEmulator.CpuZ80.Cpu;

public static class Z80Flags
{
    public const byte Carry = 0x01;
    public const byte AddSubtract = 0x02;
    public const byte HalfCarry = 0x10;
    public const byte ParityOverflow = 0x04;
    public const byte Sign = 0x80;
    public const byte Zero = 0x40;
    public const byte X = 0x08;
    public const byte Y = 0x20;

    public static bool IsSet(byte flags, byte mask) => (flags & mask) != 0;

    public static byte SignZero(byte value) => (byte)((value & Sign) | (value == 0 ? Zero : 0));

    public static byte Parity(byte value) => (byte)(BitOperations.PopCount(value) % 2 == 0 ? ParityOverflow : 0);
}
