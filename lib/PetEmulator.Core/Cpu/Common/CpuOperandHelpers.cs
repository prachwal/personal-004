namespace PetEmulator.Core;

/// <summary>Shared register and condition decoding for 8080-compatible CPUs.</summary>
public static class CpuOperandHelpers
{
    public static byte ReadRegister(
        int index,
        byte a,
        byte b,
        byte c,
        byte d,
        byte e,
        byte h,
        byte l,
        ushort memoryAddress,
        Func<ushort, byte> readMemory)
    {
        ArgumentNullException.ThrowIfNull(readMemory);

        return index switch
        {
            0 => b,
            1 => c,
            2 => d,
            3 => e,
            4 => h,
            5 => l,
            6 => readMemory(memoryAddress),
            7 => a,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }

    public static void WriteRegister(
        int index,
        byte value,
        Action<byte> setA,
        Action<byte> setB,
        Action<byte> setC,
        Action<byte> setD,
        Action<byte> setE,
        Action<byte> setH,
        Action<byte> setL,
        ushort memoryAddress,
        Action<ushort, byte> writeMemory)
    {
        ArgumentNullException.ThrowIfNull(setA);
        ArgumentNullException.ThrowIfNull(setB);
        ArgumentNullException.ThrowIfNull(setC);
        ArgumentNullException.ThrowIfNull(setD);
        ArgumentNullException.ThrowIfNull(setE);
        ArgumentNullException.ThrowIfNull(setH);
        ArgumentNullException.ThrowIfNull(setL);
        ArgumentNullException.ThrowIfNull(writeMemory);

        switch (index)
        {
            case 0: setB(value); break;
            case 1: setC(value); break;
            case 2: setD(value); break;
            case 3: setE(value); break;
            case 4: setH(value); break;
            case 5: setL(value); break;
            case 6: writeMemory(memoryAddress, value); break;
            case 7: setA(value); break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public static ushort ReadPair(int index, ushort bc, ushort de, ushort hl, ushort sp)
        => index switch
        {
            0 => bc,
            1 => de,
            2 => hl,
            3 => sp,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

    public static void WritePair(
        int index,
        ushort value,
        Action<ushort> setBc,
        Action<ushort> setDe,
        Action<ushort> setHl,
        Action<ushort> setSp)
    {
        ArgumentNullException.ThrowIfNull(setBc);
        ArgumentNullException.ThrowIfNull(setDe);
        ArgumentNullException.ThrowIfNull(setHl);
        ArgumentNullException.ThrowIfNull(setSp);

        switch (index)
        {
            case 0: setBc(value); break;
            case 1: setDe(value); break;
            case 2: setHl(value); break;
            case 3: setSp(value); break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public static bool EvaluateCondition(int condition, bool zero, bool carry, bool parity, bool sign)
        => condition switch
        {
            0 => !zero,
            1 => zero,
            2 => !carry,
            3 => carry,
            4 => !parity,
            5 => parity,
            6 => !sign,
            7 => sign,
            _ => throw new ArgumentOutOfRangeException(nameof(condition)),
        };
}
