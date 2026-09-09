namespace PetEmulator.Vic20.Keyboard;

/// <summary>Translates a host-key-string event (see PetEmulator.Desktop.KeyMapping - Avalonia
/// Key -> "KeyA"/"Digit5"/"Space"/"Enter"/... - already machine-agnostic despite living in the
/// PET-originated Desktop project) into one <see cref="Vic20KeyboardMatrix"/> cell. Mirrors the
/// shape of PetEmulator.Pet.Keyboard.IPetKeyboardMap's Translate for live GUI key handling
/// (distinct from <see cref="Vic20HostKeyMap"/>, which is char-based for scripted typing only -
/// see that class's doc comment for why they aren't merged).</summary>
public sealed class Vic20KeyboardMap
{
    public (int Row, int Col)? Translate(string hostKey) => hostKey switch
    {
        // Delegates to Vic20HostKeyMap.Find (single source of truth for these two cells - see
        // its own doc comment for how Enter/Space were verified) instead of duplicating the
        // (row, col) literals here.
        "Enter" => Vic20HostKeyMap.Find('\n'),
        "Space" => Vic20HostKeyMap.Find(' '),
        _ when hostKey.StartsWith("Key", StringComparison.Ordinal) && hostKey.Length == 4 =>
            Vic20HostKeyMap.Find(hostKey[3]),
        _ when hostKey.StartsWith("Digit", StringComparison.Ordinal) && hostKey.Length == 6 =>
            Vic20HostKeyMap.Find(hostKey[5]),
        _ => null,
    };
}
