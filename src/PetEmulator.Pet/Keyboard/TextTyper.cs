namespace PetEmulator.Pet.Keyboard;

/// <summary>
/// Types a whole string into a running <see cref="PetMachine"/> through a real
/// <see cref="IPetKeyboardMap"/>, one character at a time - press, hold for
/// <paramref name="holdInstructions"/>-worth of real execution, release, gap, next character.
/// This is the same per-key instruction budget personal-001's own passing keyboard integration
/// tests use (5,500), not the much larger holds this repo's earlier ad-hoc diagnostics used.
/// </summary>
public static class TextTyper
{
    public static void Type(
        PetMachine machine,
        IPetKeyboardMap keyboardMap,
        string text,
        ulong holdInstructions = 5_500,
        ulong gapInstructions = 5_500)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(keyboardMap);
        ArgumentNullException.ThrowIfNull(text);

        foreach (var ch in text)
        {
            var hostKey = ToHostKey(ch);
            if (hostKey is null)
                continue;

            Apply(machine, keyboardMap.Translate(hostKey, HostKeyEventKind.Press));
            machine.Run(holdInstructions);
            Apply(machine, keyboardMap.Translate(hostKey, HostKeyEventKind.Release));
            machine.Run(gapInstructions);
        }
    }

    /// <summary>Maps one character to the host-key vocabulary <see cref="IPetKeyboardMap"/>
    /// implementations expect. Only what the PET graphics keyboard can actually type (no
    /// lowercase - PET screen codes 1-26 are uppercase-only); unmapped characters are skipped.</summary>
    public static string? ToHostKey(char ch) => ch switch
    {
        >= 'A' and <= 'Z' => "Key" + ch,
        >= '0' and <= '9' => "Digit" + ch,
        ' ' => "Space",
        '\n' or '\r' => "Enter",
        '+' => "Equal",
        '"' => "Quote",
        ',' => "Comma",
        '.' => "Period",
        '/' => "Slash",
        ';' => "Semicolon",
        ':' => "Colon",
        '-' => "Minus",
        _ => null
    };

    private static void Apply(PetMachine machine, IReadOnlyList<MatrixAction> actions)
    {
        foreach (var action in actions)
        {
            if (action.Pressed)
                machine.Keyboard.Press(action.Row, action.Column);
            else
                machine.Keyboard.Release(action.Row, action.Column);
        }
    }
}
