using PetEmulator.Chips;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed class Mos2114DebugSession : ChipDebugSessionBase
{
    public Mos2114DebugSession()
        : base("MOS 2114 Color RAM", CreateDefinition()) { }

    private static ChipDebugDefinition CreateDefinition()
    {
        var chip = new MOS2114("Chip Tester Color RAM");
        return new ChipDebugDefinition(
            () => { }, chip.Reset, _ => chip.Read(0), (_, value) => chip.Write(0, value),
            [("DATA", () => chip.Read(0))], []);
    }
}
