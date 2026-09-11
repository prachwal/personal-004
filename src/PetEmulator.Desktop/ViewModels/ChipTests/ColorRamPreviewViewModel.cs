using PetEmulator.Chips;
using PetEmulator.Vic20.Display;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed class ColorRamPreviewViewModel : ObservableObject
{
    private readonly MOS2114 _chip;

    public ColorRamPreviewViewModel(MOS2114 chip) => _chip = chip;

    public IReadOnlyList<ColorSwatch> Swatches => Enumerable.Range(0, 32)
        .Select(index => new ColorSwatch(index, _chip.Read((ushort)index), $"#{Vic20Palette.ToArgb(_chip.Read((ushort)index)):X8}"))
        .ToArray();

    public void Refresh() => OnPropertyChanged(nameof(Swatches));
}

public sealed record ColorSwatch(int Index, byte Value, string Color);
