using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Models;

/// <summary>One selectable entry in the Desktop shell's module menu.</summary>
public sealed record ModuleMenuEntry(
    string Label,
    Func<IShellModule>? Create,
    IReadOnlyList<ModuleMenuEntry>? Children = null);
