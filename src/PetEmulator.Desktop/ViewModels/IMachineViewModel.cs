using Avalonia.Input;
using PetEmulator.Core;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Desktop.Views.Controls;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>
/// The contract one running machine (PET, VIC-20, ...) exposes to the Desktop shell -
/// <see cref="MainWindowViewModel"/> holds exactly one of these at a time
/// (<see cref="MainWindowViewModel.CurrentMachine"/>), and the View picks which composite
/// screen+device-bar UserControl to show purely by the concrete implementation's type
/// (Avalonia <c>DataTemplate DataType</c> matching in MainWindow.axaml) - "podmiana komponentu
/// MVVM" the user asked for, not an if/else in code-behind.
///
/// Three regions per the design brief: the screen (<see cref="FrameBuffer"/>/
/// <see cref="PixelWidth"/>/<see cref="PixelHeight"/>/<see cref="PixelAspect"/>, rendered by a
/// shared <see cref="PetScreenControl"/> - already machine-agnostic despite its name, just an
/// ARGB8888 blitter), the device icon bar (<see cref="Devices"/>, rendered by a shared
/// <c>DeviceStatusBar</c> UserControl), and <see cref="Extra"/> - a reserved, deliberately unused
/// extension point for a third per-machine element (the user picked "empty slot for the future"
/// over a concrete use during design; both PetMachineViewModel and Vic20MachineViewModel return
/// null today).
/// </summary>
public interface IMachineViewModel : IDisposable
{
    string WindowTitle { get; }

    string StatusText { get; }

    int PixelWidth { get; }

    int PixelHeight { get; }

    /// <summary>Physical width:height ratio of one source pixel - see
    /// <see cref="PetEmulator.Pet.PetProfile.PixelAspect"/>. 1:1 (square) for machines that don't
    /// need correction.</summary>
    (int Width, int Height) PixelAspect { get; }

    /// <summary>ARGB8888 (0xAARRGGBB), row-major, length == <see cref="PixelWidth"/> *
    /// <see cref="PixelHeight"/>. Not a bindable property (a View reacts to <see cref="FrameReady"/>
    /// instead) - mutating the array in place wouldn't raise per-element change notifications
    /// anyway, and re-wrapping it every frame would allocate for no benefit.</summary>
    uint[] FrameBuffer { get; }

    /// <summary>Every attached peripheral worth a status icon - empty for a machine with none
    /// modeled yet (VIC-20 v1). See <see cref="IDeviceStatus"/>.</summary>
    IReadOnlyList<IDeviceStatus> Devices { get; }

    /// <summary>Reserved, unused extension point - see this interface's own doc comment.</summary>
    object? Extra { get; }

    /// <summary>Raised after each render tick, once <see cref="FrameBuffer"/> holds the new frame.</summary>
    event EventHandler? FrameReady;

    /// <summary>Raised when geometry (<see cref="PixelWidth"/>/<see cref="PixelHeight"/>/
    /// <see cref="PixelAspect"/>) changes - the View reacts by resizing its screen control before
    /// the next <see cref="FrameReady"/>.</summary>
    event EventHandler? GeometryChanged;

    /// <summary>Advances the machine and its display by one render tick (called from the shell's
    /// timer - see <see cref="MainWindowViewModel"/>).</summary>
    void Tick();

    /// <summary>Resets the running machine to its power-on state. Plain method (not a
    /// <c>[RelayCommand]</c>-generated <c>ResetCommand</c>) so it stays resolvable through this
    /// interface under Avalonia's compiled bindings - the shell's own Reset command
    /// (<see cref="MainWindowViewModel"/>) delegates to it generically regardless of which
    /// concrete machine is current.</summary>
    void Reset();

    /// <summary>Translates one Avalonia key event into matrix presses/releases on the running
    /// machine's keyboard.</summary>
    void HandleKey(Key key, HostKeyEventKind kind);
}
