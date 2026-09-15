namespace PetEmulator.Trs80;

/// <summary>Types a whole string into a running <see cref="Trs80Machine"/> through the real
/// <see cref="Trs80KeyboardMatrix"/> - press, hold, release, gap, next character. Mirrors
/// PetEmulator.Pet.Keyboard.TextTyper/PetEmulator.Vic20.Keyboard.Vic20TextTyper's shape (not shared
/// for the same reason those two aren't shared with each other: tightly bound to one machine's
/// keyboard type). The TRS-80 Model I keyboard has no shift combos to worry about for BASIC text -
/// every letter, digit and common punctuation mark this typer covers is its own dedicated key on
/// <see cref="Trs80Key"/>, unlike CPC464 where several punctuation characters share a cell with a
/// digit and need Shift to pick the right one.</summary>
public static class Trs80TextTyper
{
    public static void Type(Trs80Machine machine, string text, ulong holdInstructions = 10_000, ulong gapInstructions = 10_000)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(text);

        foreach (var ch in text)
        {
            var mapped = ToKey(ch);
            if (mapped is not { } key) continue;

            if (key.Shift) machine.Keyboard.SetKeyDown(Trs80Key.Shift, true);
            machine.Keyboard.SetKeyDown(key.Key, true);
            machine.Run(holdInstructions);
            machine.Keyboard.SetKeyDown(key.Key, false);
            if (key.Shift) machine.Keyboard.SetKeyDown(Trs80Key.Shift, false);
            machine.Run(gapInstructions);
        }
    }

    /// <summary>'+' is Shift+Semicolon on the real Model I keyboard (bare Semicolon is ';') -
    /// verified with a "PRINT 1&lt;key&gt;1" oracle: bare Semicolon echoes the list-separator
    /// (blank-padded "1  1"), Shift+Semicolon evaluates addition ("2").</summary>
    public static (Trs80Key Key, bool Shift)? ToKey(char ch) => ch switch
    {
        >= 'A' and <= 'Z' => ((Trs80Key)(Trs80Key.A + (ch - 'A')), false),
        >= '0' and <= '9' => ((Trs80Key)(Trs80Key.D0 + (ch - '0')), false),
        ' ' => (Trs80Key.Space, false),
        '\n' or '\r' => (Trs80Key.Enter, false),
        '@' => (Trs80Key.At, false),
        ':' => (Trs80Key.Colon, false),
        ';' => (Trs80Key.Semicolon, false),
        '+' => (Trs80Key.Semicolon, true),
        ',' => (Trs80Key.Comma, false),
        '-' => (Trs80Key.Minus, false),
        '.' => (Trs80Key.Period, false),
        '/' => (Trs80Key.Slash, false),
        _ => null,
    };
}
