using PetEmulator.Chips;
using PetEmulator.Core;
using PetEmulator.Desktop.Infrastructure;
using PetEmulator.Pet.Display;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public interface IChipVisualViewModel
{
    int PixelWidth { get; }
    int PixelHeight { get; }
    (int Width, int Height) PixelAspect { get; }
    uint[] FrameBuffer { get; }
    event EventHandler? FrameReady;
}

public sealed class CrtcPreviewViewModel : IChipVisualViewModel
{
    private readonly PetRasterDisplay _display;
    private readonly FlatMemoryBus _memory;
    private readonly MT6545 _crtc;
    private readonly ushort _displayAddress;

    public CrtcPreviewViewModel(PetRasterDisplay display, FlatMemoryBus memory, MT6545 crtc, ushort displayAddress)
    {
        _display = display;
        _memory = memory;
        _crtc = crtc;
        _displayAddress = displayAddress;
        FrameBuffer = new uint[display.PixelWidth * display.PixelHeight];
    }

    public int PixelWidth => _display.PixelWidth;
    public int PixelHeight => _display.PixelHeight;
    public (int Width, int Height) PixelAspect => (5, 6);
    public uint[] FrameBuffer { get; }
    public event EventHandler? FrameReady;

    public void Refresh()
    {
        var source = _crtc.DisplayStartAddress;
        for (var i = 0; i < 1_000; i++)
            _memory.Write((ushort)(_displayAddress + i), _memory.Read((ushort)(source + i)));
        _display.Render(FrameBuffer);
        FrameReady?.Invoke(this, EventArgs.Empty);
    }
}
