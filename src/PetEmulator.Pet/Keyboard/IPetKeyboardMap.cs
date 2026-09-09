namespace PetEmulator.Pet.Keyboard;

/// <summary>One PET matrix cell action produced by translating a host key event.</summary>
public readonly record struct MatrixAction(int Row, int Column, bool Pressed);

public enum HostKeyEventKind { Press, Release }

/// <summary>
/// Translates one physical host-keyboard key event into the ordered sequence of PET matrix
/// actions it produces. Most keys are a plain 1:1 (one host key -> one matrix cell), but some
/// real PET keyboards force other matrix cells as a side effect - see each variant's '+' key
/// for the ROM-verified reason. Implementations are allowed to be stateful (the CBM 8032
/// Shift/'+' interaction needs it) - use one instance per keyboard/session, not a shared one.
/// </summary>
public interface IPetKeyboardMap
{
    string Id { get; }

    IReadOnlyList<MatrixAction> Translate(string hostKey, HostKeyEventKind kind);
}
