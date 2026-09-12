namespace PetEmulator.Core;

/// <summary>Serializable-neutral snapshot of architectural state.</summary>
public sealed record CpuStateSnapshot(
    IReadOnlyDictionary<string, ulong> Registers,
    bool Halted);
