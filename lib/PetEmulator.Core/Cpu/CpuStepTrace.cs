namespace PetEmulator.Core;

/// <summary>Diagnostic record for one processor lifecycle step.</summary>
public sealed record CpuStepTrace(
    CpuDebugSnapshot Before,
    CpuDebugSnapshot After,
    OpcodeKey? Opcode,
    string? Mnemonic,
    CpuStepResult Result);
