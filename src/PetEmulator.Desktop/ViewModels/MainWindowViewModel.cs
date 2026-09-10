using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Desktop.Infrastructure;
using PetEmulator.Desktop.Models;
using PetEmulator.Desktop.Services;
using PetEmulator.Desktop.Views;
using PetEmulator.Pet;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Vic20;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>
/// The Desktop shell: owns the currently-running machine (<see cref="CurrentModule"/>, one
/// <see cref="IShellModule"/> at a time) and the render-loop timer that ticks it. Switching
/// machine (<see cref="SwitchMachineCommand"/>) disposes the old one and replaces
/// <see cref="CurrentModule"/> wholesale with a freshly-constructed instance - MainWindow.axaml's
/// <c>DataTemplate</c>s pick which composite screen+device-bar View to show purely from the new
/// instance's concrete type ("podmiana komponentu MVVM", not an if/else).
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IFilePickerService _filePicker;
    private readonly string _romsRoot;
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private IShellModule _currentModule;

    public MainWindowViewModel(IFilePickerService filePicker)
    {
        _filePicker = filePicker;
        _romsRoot = RomsRootLocator.Find();

        ModuleChoices =
        [
             .. PetProfileCatalog.All.Select(profile =>
                 new ModuleMenuEntry(profile.Name, () => new PetMachineViewModel(profile, Path.Combine(_romsRoot, "pet")))),
               .. Vic20ExpansionPresetCatalog.All.Select(preset =>
                   new ModuleMenuEntry(preset.Label, () => new Vic20MachineViewModel(
                       Path.Combine(_romsRoot, "vic20"), preset.Preset))),
              new ModuleMenuEntry("Chip Tester", () => new ChipTesterViewModel(_romsRoot)),
              new ModuleMenuEntry("Media Tester", () => new MediaTesterViewModel(_filePicker)),
              new ModuleMenuEntry("Font / Glyph Viewer", () => new FontViewerViewModel(_romsRoot)),
              new ModuleMenuEntry("CPU Opcode Stepper", () => new OpcodeStepperViewModel()),
              new ModuleMenuEntry("Keyboard Matrix", () => new KeyboardMatrixViewModel()),
         ];

        _currentModule = ModuleChoices[0].Create();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += (_, _) =>
        {
            if (CurrentModule is IMachineViewModel machine)
                machine.Tick();
        };
        _timer.Start();
    }

    /// <summary>Every selectable machine configuration, for the Machine menu.</summary>
    public IReadOnlyList<ModuleMenuEntry> ModuleChoices { get; }

    /// <summary>Raised by <see cref="ExitCommand"/> - the View closes the window.</summary>
    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void Reset()
    {
        if (CurrentModule is IMachineViewModel machine)
            machine.Reset();
    }

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void SwitchMachine(ModuleMenuEntry entry)
    {
        var old = CurrentModule;
        CurrentModule = entry.Create();
        old.Dispose();
    }

    /// <summary>Loads a VICE-style .tap file into the current machine's datasette, if it has one -
    /// both <see cref="PetMachineViewModel"/> and <see cref="Vic20MachineViewModel"/> do (see
    /// docs/vic20-tape.md). The file-picker dialog itself is Avalonia-specific glue that lives in
    /// <see cref="MainWindow"/>'s code-behind (needs a <c>TopLevel</c>), which calls straight
    /// through to this.</summary>
    [RelayCommand]
    private async Task LoadTape()
    {
        try
        {
            var path = await _filePicker.PickTapeToOpenAsync();
            if (path is not null)
                LoadTape(path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Load tape failed: {ex.Message}");
        }
    }

    public void LoadTape(string path)
    {
        switch (CurrentModule)
        {
            case PetMachineViewModel pet: pet.LoadTape(path); break;
            case Vic20MachineViewModel vic20: vic20.LoadTape(path); break;
        }
    }

    /// <inheritdoc cref="LoadTape"/>
    [RelayCommand]
    private async Task LoadDisk()
    {
        try
        {
            var path = await _filePicker.PickDiskToOpenAsync();
            if (path is not null)
                LoadDisk(path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Load disk failed: {ex.Message}");
        }
    }

    public void LoadDisk(string path)
    {
        switch (CurrentModule)
        {
            case PetMachineViewModel pet: pet.LoadDisk(path); break;
            case Vic20MachineViewModel vic20: vic20.LoadDisk(path); break;
        }
    }

    /// <summary>Creates a fresh, formatted, writable D64 at <paramref name="path"/> and mounts it,
    /// if the current machine has a disk drive. See <see cref="PetMachineViewModel.NewDisk"/>.
    /// </summary>
    public void NewDisk(string path)
    {
        switch (CurrentModule)
        {
            case PetMachineViewModel pet: pet.NewDisk(path); break;
            case Vic20MachineViewModel vic20: vic20.NewDisk(path); break;
        }
    }

    [RelayCommand]
    private async Task NewDisk()
    {
        try
        {
            var path = await _filePicker.PickDiskToSaveAsync();
            if (path is not null)
                NewDisk(path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"New disk failed: {ex.Message}");
        }
    }

    /// <summary>Puts a fresh, empty, writable tape in the current machine's datasette, if it has
    /// one - VIC-20 only for now (real SAVE support - see docs/vic20-tape.md; PET has no SAVE
    /// emulation yet). No file dialog needed (nothing to pick a path for yet), so this is a plain
    /// command, not glue through <see cref="MainWindow"/>'s code-behind like <see cref="LoadTape"/>.</summary>
    [RelayCommand]
    private void NewTape()
    {
        if (CurrentModule is Vic20MachineViewModel vic20)
            vic20.NewTape();
    }

    public void HandleKey(Key key, HostKeyEventKind kind)
    {
        if (CurrentModule is IMachineViewModel machine)
            machine.HandleKey(key, kind);
    }

    public void Dispose()
    {
        _timer.Stop();
        CurrentModule.Dispose();
    }
}
