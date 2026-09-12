namespace PetEmulator.Core;

/// <summary>Neutral metadata and typed execution handler for one opcode.</summary>
public sealed record OpcodeDefinition<TState>(
    OpcodeKey Key,
    string Mnemonic,
    byte Length,
    byte BaseCycles,
    string AddressingMode,
    Func<TState, CpuExecutionContext, CpuStepResult> Execute,
    bool HasPageCrossPenalty = false,
    Action<TState, CpuExecutionContext, byte>? ExecuteCycle = null);
