namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Właściwości publiczne

    /// <summary>
    /// Zwraca aktualny stan rejestru statusu (P).
    /// </summary>
    public byte Status => _p;

    /// <summary>
    /// Processor Status Register - rejestr flag.
    /// </summary>
    public byte P
    {
        get => _p;
        set => _p = value;
    }

    /// <summary>
    /// Controls whether ADC/SBC honor decimal mode when D=1. MOS 6502 enables it;
    /// NES 2A03 leaves the D flag visible but performs binary arithmetic.
    /// </summary>
    public bool DecimalModeEnabled => Variant.Quirks.HasFlag(CpuQuirk.DecimalArithmetic);

    /// <summary>
    /// Controls whether the JMP indirect bug is present.
    /// NMOS 6502 has the bug: JMP ($xxFF) reads high byte from $xx00 instead of $(xx+1)00.
    /// Ricoh 2A03 (NES) does NOT have this bug.
    /// </summary>
    public bool HasJmpIndirectBug => Variant.Quirks.HasFlag(CpuQuirk.JmpIndirectPageWrap);

    /// <summary>
    /// Controls whether BCD (decimal mode) ADC/SBC operations incur an extra cycle.
    /// WDC 65C02 charges an extra cycle when D=1 for ADC/SBC.
    /// NMOS 6502 and other variants do not.
    /// </summary>
    public bool HasCmosBcdExtraCycle => Variant.Quirks.HasFlag(CpuQuirk.CmosBcdExtraCycle);

    /// <summary>
    /// Accumulator - główny rejestr arytmetyczny.
    /// </summary>
    public byte A
    {
        get => _a;
        set => _a = value;
    }

    /// <summary>
    /// Rejestr indeksowy X.
    /// </summary>
    public byte X
    {
        get => _x;
        set => _x = value;
    }

    /// <summary>
    /// Rejestr indeksowy Y.
    /// </summary>
    public byte Y
    {
        get => _y;
        set => _y = value;
    }

    /// <summary>
    /// Program Counter - wskaźnik bieżącej instrukcji.
    /// </summary>
    public ushort PC
    {
        get => _pc;
        set => _pc = value;
    }

    /// <summary>
    /// Stack Pointer - wskaźnik stosu.
    /// </summary>
    public byte SP
    {
        get => _sp;
        set => _sp = value;
    }

    /// <summary>
    /// Wskazuje, czy procesor jest zatrzymany (np. przez KIL/JAM).
    /// </summary>
    public bool Halted => _halted;

    /// <summary>
    /// Licznik wykonanych instrukcji.
    /// </summary>
    public ulong InstructionCount => _instructionCount;

    /// <summary>
    /// Licznik cykli zegara.
    /// </summary>
    public ulong CycleCount => _cycle;

    #endregion
}
