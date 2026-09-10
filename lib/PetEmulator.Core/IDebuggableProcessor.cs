namespace PetEmulator.Core;

/// <summary>
/// Optional capability for an <see cref="IProcessor"/> that exposes its architectural registers
/// by name, for debug tooling (trace lines, break-on-PC, register dumps). Not every processor
/// needs to implement this - <see cref="IProcessor"/> itself stays CPU-agnostic on purpose.
/// </summary>
public interface IDebuggableProcessor
{
    /// <summary>Current register values keyed by name (e.g. "PC", "A", "X", "Y", "SP", "P"). Names and set are CPU-specific.</summary>
    IReadOnlyDictionary<string, ulong> GetRegisters();
}
