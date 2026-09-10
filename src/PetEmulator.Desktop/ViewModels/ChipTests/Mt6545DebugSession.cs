using PetEmulator.Chips;
using PetEmulator.Desktop.Infrastructure;
using PetEmulator.Pet;
using PetEmulator.Pet.Display;
using PetEmulator.Pet.Fonts;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed class Mt6545DebugSession : ChipDebugSessionBase
{
    private readonly CrtcPreviewViewModel _preview;

    public Mt6545DebugSession(string romsRoot)
        : this(CreateState(romsRoot)) { }

    private Mt6545DebugSession(CrtcState state)
        : base("MT 6545 CRTC", state.Definition)
    {
        _preview = state.Preview;
        _preview.Refresh();
    }

    public override object? Visual => _preview;

    protected override void AfterRefresh() => _preview?.Refresh();

    private static CrtcState CreateState(string romsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);
        var profile = PetProfileCatalog.Cbm4032;
        var memory = new FlatMemoryBus(ushort.MaxValue + 1);
        var chip = new MT6545("Chip Tester CRTC");
        var font = new PetCharacterRomLoader().Load(Path.Combine(
            romsRoot, "pet", profile.RomDirectory, profile.CharacterRomPath));
        var preview = new CrtcPreviewViewModel(
            new PetRasterDisplay(profile, memory, font), memory, chip, 0x8000);

        chip.Reset();
        WriteRegister(chip, 0, 49);
        WriteRegister(chip, 1, 40);
        WriteRegister(chip, 4, 25);
        WriteRegister(chip, 6, 25);
        WriteRegister(chip, 9, 7);
        WriteRegister(chip, 12, 0x20);
        WriteRegister(chip, 13, 0x00);
        WriteRegister(chip, 14, 0x20);
        WriteRegister(chip, 15, 0x00);
        FillPattern(memory, 0x2000, "CHIP TESTER CRTC");
        FillPattern(memory, 0x2400, "POKE R12/R13 TO MOVE");

        return new CrtcState(new ChipDebugDefinition(
            () => chip.Tick(1), chip.Reset,
            name => { chip.Write(0, ParseRegister(name)); return chip.Read(1); },
            (name, value) => { chip.Write(0, ParseRegister(name)); chip.Write(1, value); },
            [("R0", () => ReadRegister(chip, 0)), ("R1", () => ReadRegister(chip, 1)),
             ("R2", () => ReadRegister(chip, 2)), ("R3", () => ReadRegister(chip, 3)),
             ("R4", () => ReadRegister(chip, 4)), ("R5", () => ReadRegister(chip, 5)),
             ("R6", () => ReadRegister(chip, 6)), ("R7", () => ReadRegister(chip, 7))],
             [("HSync", () => chip.HSync, null), ("VSync", () => chip.VSync, null),
             ("DisplayEnable", () => chip.DisplayEnable, null), ("VerticalBlanking", () => chip.VerticalBlanking, null)]), preview);
    }

    private static void WriteRegister(MT6545 chip, byte register, byte value)
    {
        chip.Write(0, register);
        chip.Write(1, value);
    }

    private static void FillPattern(FlatMemoryBus memory, ushort address, string text)
    {
        const int columns = 40;
        var row = 12;
        var start = (columns - text.Length) / 2;
        for (var i = 0; i < text.Length; i++)
            memory.Write((ushort)(address + row * columns + start + i), ToPetScreenCode(text[i]));
    }

    private static byte ToPetScreenCode(char value) => value is >= 'A' and <= 'Z'
        ? (byte)(value - 'A' + 1)
        : (byte)0x20;

    private static byte ParseRegister(string name) => Convert.ToByte(name.TrimStart('R'), 16);

    private static byte ReadRegister(MT6545 chip, byte register)
    {
        chip.Write(0, register);
        return chip.Read(1);
    }

    private sealed record CrtcState(ChipDebugDefinition Definition, CrtcPreviewViewModel Preview);
}
