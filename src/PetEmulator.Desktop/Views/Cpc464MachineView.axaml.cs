using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Desktop.Views.Controls;

namespace PetEmulator.Desktop.Views;

public partial class Cpc464MachineView : UserControl
{
    private static readonly ILogger Log = EmulatorLogging.CreateLogger("View");
    private Cpc464MachineViewModel? _wired;
    public Cpc464MachineView() { InitializeComponent(); DataContextChanged += (_, _) => Rewire(); }
    private void Rewire() { if (_wired is not null) _wired.FrameReady -= OnFrameReady; _wired = DataContext as Cpc464MachineViewModel; if (_wired is null) return; _wired.FrameReady += OnFrameReady; Screen.SetSize(_wired.PixelWidth, _wired.PixelHeight, 1, 1); Screen.UpdateFrame(_wired.FrameBuffer); Screen.Focus(); Log.LogInformation("Cpc464MachineView wired to {Title} ({Width}x{Height}).", _wired.WindowTitle, _wired.PixelWidth, _wired.PixelHeight); var machine = _wired; OnScreenKeyboard.Configure(CpcKeyboardLayoutFactory.Build(), machine.SendKeyboardSignal); }
    private void OnFrameReady(object? sender, EventArgs e) => Screen.UpdateFrame(_wired!.FrameBuffer);
}
