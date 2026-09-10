namespace PetEmulator.Core.Keyboard;

/// <summary>A physical keyboard key position in a machine keyboard matrix.</summary>
public readonly record struct KeyboardMatrixPosition(int Row, int Column);

/// <summary>
/// Base contract for mapping every AT keyboard key to a machine matrix.
/// A machine overrides only keys it physically implements; unsupported keys remain explicit nulls.
/// </summary>
public abstract class AtKeyboardMapping
{
    public static IReadOnlyList<AtKeyboardKey> AllKeys { get; } = Enum.GetValues<AtKeyboardKey>();

    public KeyboardMatrixPosition? Translate(AtKeyboardKey key) => Map(key);

    protected abstract KeyboardMatrixPosition? Map(AtKeyboardKey key);
}
