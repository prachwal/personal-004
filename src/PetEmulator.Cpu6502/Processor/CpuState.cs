namespace Cpu6502;

/// <summary>
/// Klasa reprezentująca stan procesora 6502 (rejestry).
/// </summary>
public class CpuState
{
    /// <summary>
    /// Accumulator - główny rejestr arytmetyczny.
    /// </summary>
    public byte A { get; set; }

    /// <summary>
    /// Rejestr indeksowy X.
    /// </summary>
    public byte X { get; set; }

    /// <summary>
    /// Rejestr indeksowy Y.
    /// </summary>
    public byte Y { get; set; }

    /// <summary>
    /// Program Counter - wskaźnik bieżącej instrukcji (16-bit).
    /// </summary>
    public ushort PC { get; set; }

    /// <summary>
    /// Stack Pointer - wskaźnik stosu.
    /// </summary>
    public byte SP { get; set; }

    /// <summary>
    /// Processor Status Register - flagi procesora.
    /// </summary>
    public byte P { get; set; }

    /// <summary>
    /// Licznik cykli zegara.
    /// </summary>
    public ulong Cycle { get; set; }

    /// <summary>
    /// Instruction Register - przechowuje opcode przesunięty o 3 oraz licznik cykli.
    /// </summary>
    public byte IR { get; set; }

    /// <summary>
    /// Sygnalizuje rozpoczęcie nowej instrukcji.
    /// </summary>
    public bool Sync { get; set; }

    /// <summary>
    /// Wskazuje, czy procesor jest zatrzymany (np. przez KIL/JAM).
    /// </summary>
    public bool Halted { get; set; }
}
