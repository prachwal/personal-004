using PetEmulator.Desktop.Infrastructure;
using PetEmulator.Vic20.Display;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed class VicPreviewViewModel : IChipVisualViewModel
{
    private readonly Vic20RasterDisplay _display;
    private readonly FlatMemoryBus _memory;

    public VicPreviewViewModel(Vic20RasterDisplay display, FlatMemoryBus memory)
    {
        _display = display;
        _memory = memory;
        FrameBuffer = new uint[display.PixelWidth * display.PixelHeight];
    }

    public int PixelWidth => _display.PixelWidth;
    public int PixelHeight => _display.PixelHeight;
    public (int Width, int Height) PixelAspect => (5, 6);
    public uint[] FrameBuffer { get; }
    public event EventHandler? FrameReady;

    public void Refresh()
    {
        _display.Render(FrameBuffer);
        FrameReady?.Invoke(this, EventArgs.Empty);
    }
}
