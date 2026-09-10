using PetEmulator.Chips;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed class Mos2114DebugSession : ChipDebugSessionBase
{
    private readonly ColorRamPreviewViewModel _preview;

    public Mos2114DebugSession()
        : this(CreateState()) { }

    private Mos2114DebugSession(Mos2114State state)
        : base("MOS 2114 Color RAM", state.Definition)
    {
        _preview = state.Preview;
        _preview.Refresh();
    }

    public override object? Visual => _preview;
    protected override void AfterRefresh() => _preview?.Refresh();

    private static Mos2114State CreateState()
    {
        var chip = new MOS2114("Chip Tester Color RAM");
        return new Mos2114State(new ChipDebugDefinition(
            () => { }, chip.Reset, _ => chip.Read(0), (_, value) => chip.Write(0, value),
            [("DATA", () => chip.Read(0))], []), new ColorRamPreviewViewModel(chip));
    }

    private sealed record Mos2114State(ChipDebugDefinition Definition, ColorRamPreviewViewModel Preview);
}
