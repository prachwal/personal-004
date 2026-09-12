namespace PetEmulator.Vic20.Keyboard;

/// <summary>Types a whole string into a running <see cref="Vic20Machine"/> through
/// <see cref="Vic20HostKeyMap"/>, one character at a time - press, hold, release, gap. Mirrors
/// PetEmulator.Pet.Keyboard.TextTyper's shape (same reason it isn't shared: tightly bound to a
/// specific machine's Keyboard property type - see docs/vic20/migration-plan.md step 6).</summary>
public static class Vic20TextTyper
{
    public static void Type(Vic20Machine machine, string text, ulong holdInstructions = 5_500, ulong gapInstructions = 5_500)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(text);

        foreach (var ch in text)
        {
            // The VIC-20 keyboard has no dedicated quote or dollar keys: they are SHIFT+2 and
            // SHIFT+4 respectively. The real KERNAL identifies SHIFT at matrix row 4, column 6.
            var shifted = ch is '"' or '$';
            var key = Vic20HostKeyMap.Find(ch switch { '"' => '2', '$' => '4', _ => ch });
            if (key is not { } cell)
                continue;

            if (shifted)
            {
                machine.Keyboard.Press(4, 6);
                machine.Run(holdInstructions);
            }
            machine.Keyboard.Press(cell.Row, cell.Col);
            machine.Run(holdInstructions);
            machine.Keyboard.Release(cell.Row, cell.Col);
            if (shifted)
                machine.Keyboard.Release(4, 6);
            machine.Run(gapInstructions);
        }
    }
}
