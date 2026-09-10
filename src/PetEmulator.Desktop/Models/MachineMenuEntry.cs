using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Models;

/// <summary>One selectable entry in the machine menu.</summary>
public sealed record MachineMenuEntry(string Label, Func<IMachineViewModel> Create);
