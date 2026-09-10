using PetEmulator.Chips;
using PetEmulator.Desktop.Resources;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed class Mt6520DebugSession : ChipDebugSessionBase
{
    public Mt6520DebugSession()
        : base("MT 6520 PIA", ChipDescriptions.Get("Mt6520"), CreateDefinition()) { }

    private static ChipDebugDefinition CreateDefinition()
    {
        var chip = new MT6520("Chip Tester PIA");
        return new ChipDebugDefinition(
            () => chip.Tick(1), chip.Reset,
            name => chip.Read((ushort)(name switch { "ORA" => 0, "CRA" => 1, "ORB" => 2, "CRB" => 3, _ => throw new ArgumentException($"Unknown register: {name}") })),
            (name, value) => chip.Write((ushort)(name switch { "ORA" => 0, "CRA" => 1, "ORB" => 2, "CRB" => 3, _ => throw new ArgumentException($"Unknown register: {name}") }), value),
            [("ORA", () => chip.ORA), ("CRA", () => chip.Read(1)), ("ORB", () => chip.ORB), ("CRB", () => chip.Read(3))],
            [("CA1", () => chip.CA1, value => chip.CA1 = value), ("CA2", () => chip.CA2, value => chip.CA2 = value),
             ("CB1", () => chip.CB1, value => chip.CB1 = value), ("CB2", () => chip.CB2, value => chip.CB2 = value),
             ("IRQ", () => chip.IRQ, null)]);
    }
}
