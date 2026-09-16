using Avalonia.Input;
using PetEmulator.Desktop.Input;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>Keyboard input capability shared by every desktop machine module.</summary>
public interface IKeyboardViewModel
{
    void HandleKey(Key key, HostKeyEventKind kind);
}
