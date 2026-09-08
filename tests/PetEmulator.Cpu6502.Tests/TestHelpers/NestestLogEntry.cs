namespace Cpu6502.Tests.TestHelpers;

/// <summary>
/// Reprezentuje pojedynczą linię logu nestest.
/// Format: C000  4C F5 C5  JMP $C5F5  A:00 X:00 Y:00 P:24 SP:FD PPU:0,0 CYC:7
/// </summary>
public readonly struct NestestLogEntry
{
    /// <summary>Licznik programu.</summary>
    public ushort PC { get; }

    /// <summary>Bajty opkodu instrukcji.</summary>
    public byte[] OpcodeBytes { get; }

    /// <summary>Reprezentacja tekstowa instrukcji.</summary>
    public string Instruction { get; }

    /// <summary>Wartość rejestru A.</summary>
    public byte A { get; }

    /// <summary>Wartość rejestru X.</summary>
    public byte X { get; }

    /// <summary>Wartość rejestru Y.</summary>
    public byte Y { get; }

    /// <summary>Wartość rejestru flag (P).</summary>
    public byte P { get; }

    /// <summary>Wartość wskaźnika stosu.</summary>
    public byte SP { get; }

    /// <summary>Liczba cykli od startu.</summary>
    public ulong Cycle { get; }

    /// <summary>
    /// Tworzy nową instancję NestestLogEntry.
    /// </summary>
    public NestestLogEntry(ushort pc, byte[] opcodeBytes, string instruction, byte a, byte x, byte y, byte p, byte sp, ulong cycle)
    {
        PC = pc;
        OpcodeBytes = opcodeBytes;
        Instruction = instruction;
        A = a;
        X = x;
        Y = y;
        P = p;
        SP = sp;
        Cycle = cycle;
    }
}
