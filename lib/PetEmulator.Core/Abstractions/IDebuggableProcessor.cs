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

    /// <summary>Names, from <see cref="GetRegisters"/>, of registers that are 8 bits wide (a debug
    /// trace should format them as 2 hex digits) - every other key is 16-bit (4 hex digits). Empty
    /// by default: a CPU family only needs to override this when it actually has a mix of 8-bit and
    /// 16-bit registers whose values can't be told apart by width alone (e.g. Z80's HL vs its own
    /// 8-bit H/L halves - a 16-bit register that happens to hold a small value must still print
    /// 4 digits, so formatting can't be inferred from the value like it was before this existed).</summary>
    IReadOnlyCollection<string> EightBitRegisterNames => [];
}
