namespace PetEmulator.Core;

/// <summary>Minimal lifecycle contract shared by emulated processors.</summary>
public interface IProcessor
{
    bool Halted { get; }

    ulong CycleCount { get; }

    ulong InstructionCount { get; }

    void Reset();

    void StepInstruction();

    void SetIRQ(bool active);

    void SetNMI(bool active);
}
