using PetEmulator.Core;

namespace PetEmulator.Cpu6502;

/// <summary>
/// Klasa reprezentująca stan procesora 6502 (rejestry).
/// </summary>
public class CpuState : PetEmulator.Core.CpuState
{
    internal Cpu6502? Owner { get; set; }

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

    public override IReadOnlyDictionary<string, ulong> GetRegisters() => new Dictionary<string, ulong>
    {
        ["PC"] = PC,
        ["A"] = A,
        ["X"] = X,
        ["Y"] = Y,
        ["SP"] = SP,
        ["P"] = P,
        ["Cycle"] = Cycle,
        ["IR"] = IR,
        ["Sync"] = Sync ? 1UL : 0UL,
    };

    public override CpuStateSnapshot CaptureSnapshot()
        => new(GetRegisters(), Halted);

    public override void RestoreSnapshot(CpuStateSnapshot snapshot)
    {
        A = (byte)snapshot.Registers["A"];
        X = (byte)snapshot.Registers["X"];
        Y = (byte)snapshot.Registers["Y"];
        PC = (ushort)snapshot.Registers["PC"];
        SP = (byte)snapshot.Registers["SP"];
        P = (byte)snapshot.Registers["P"];
        if (snapshot.Registers.TryGetValue("Cycle", out var cycle))
            Cycle = cycle;
        if (snapshot.Registers.TryGetValue("IR", out var ir))
            IR = (byte)ir;
        if (snapshot.Registers.TryGetValue("Sync", out var sync))
            Sync = sync != 0;
        Halted = snapshot.Halted;
    }
}
