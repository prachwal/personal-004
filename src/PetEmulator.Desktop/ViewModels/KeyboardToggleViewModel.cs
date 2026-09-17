using CommunityToolkit.Mvvm.ComponentModel;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>Single source of truth for a machine's optional on-screen keyboard visibility.
/// A machine without one exposes <c>null</c> through
/// <see cref="IMachineViewModel.KeyboardToggle"/> instead - <c>MachineHeaderBar</c> then renders
/// an empty slot (no disabled button, no per-machine if/else in XAML).</summary>
public sealed partial class KeyboardToggleViewModel : ObservableObject
{
    /// <summary>Whether the on-screen keyboard overlay is shown. Off by default - it's an
    /// optional aid for clicking keys with a mouse, not the primary input path (a real keyboard
    /// still works via <c>HandleKey</c>), so it shouldn't occupy screen space unasked.</summary>
    [ObservableProperty]
    private bool _isVisible;
}
