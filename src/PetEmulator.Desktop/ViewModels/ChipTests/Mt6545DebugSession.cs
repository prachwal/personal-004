using PetEmulator.Chips;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed class Mt6545DebugSession : ChipDebugSessionBase
{
    public Mt6545DebugSession()
        : base("MT 6545 CRTC", CreateDefinition()) { }

    private static ChipDebugDefinition CreateDefinition()
    {
        var chip = new MT6545("Chip Tester CRTC");
        return new ChipDebugDefinition(
            () => chip.Tick(1), chip.Reset,
            name => { chip.Write(0, ParseRegister(name)); return chip.Read(1); },
            (name, value) => { chip.Write(0, ParseRegister(name)); chip.Write(1, value); },
            [("R0", () => ReadRegister(chip, 0)), ("R1", () => ReadRegister(chip, 1)),
             ("R2", () => ReadRegister(chip, 2)), ("R3", () => ReadRegister(chip, 3)),
             ("R4", () => ReadRegister(chip, 4)), ("R5", () => ReadRegister(chip, 5)),
             ("R6", () => ReadRegister(chip, 6)), ("R7", () => ReadRegister(chip, 7))],
             [("HSync", () => chip.HSync, null), ("VSync", () => chip.VSync, null),
             ("DisplayEnable", () => chip.DisplayEnable, null), ("VerticalBlanking", () => chip.VerticalBlanking, null)]);
    }

    private static byte ParseRegister(string name) => Convert.ToByte(name.TrimStart('R'), 16);

    private static byte ReadRegister(MT6545 chip, byte register)
    {
        chip.Write(0, register);
        return chip.Read(1);
    }
}
