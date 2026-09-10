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

namespace PetEmulator.Desktop.ViewModels;

/// <summary>
/// The Desktop shell: owns the currently-running machine (<see cref="CurrentMachine"/>, one
/// <see cref="IMachineViewModel"/> at a time) and the render-loop timer that ticks it. Switching
/// machine (<see cref="SwitchMachineCommand"/>) disposes the old one and replaces
/// <see cref="CurrentMachine"/> wholesale with a freshly-constructed instance - MainWindow.axaml's
/// <c>DataTemplate</c>s pick which composite screen+device-bar View to show purely from the new
/// instance's concrete type ("podmiana komponentu MVVM", not an if/else).
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IFilePickerService _filePicker;
    private readonly string _romsRoot;
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private IMachineViewModel _currentMachine;

    public MainWindowViewModel(IFilePickerService filePicker)
    {
        _filePicker = filePicker;
        _romsRoot = RomsRootLocator.Find();

        MachineChoices =
        [
            .. PetProfileCatalog.All.Select(profile =>
                new MachineMenuEntry(profile.Name, () => new PetMachineViewModel(profile, Path.Combine(_romsRoot, "pet")))),
            new MachineMenuEntry("VIC-20", () => new Vic20MachineViewModel(Path.Combine(_romsRoot, "vic20"))),
        ];

        _currentMachine = MachineChoices[0].Create();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += (_, _) => CurrentMachine.Tick();
        _timer.Start();
    }

    /// <summary>Every selectable machine configuration, for the Machine menu.</summary>
    public IReadOnlyList<MachineMenuEntry> MachineChoices { get; }

    /// <summary>Raised by <see cref="ExitCommand"/> - the View closes the window.</summary>
    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void Reset() => CurrentMachine.Reset();

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void SwitchMachine(MachineMenuEntry entry)
    {
        var old = CurrentMachine;
        CurrentMachine = entry.Create();
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
        switch (CurrentMachine)
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
        if (CurrentMachine is PetMachineViewModel pet)
            pet.LoadDisk(path);
    }

    /// <summary>Creates a fresh, formatted, writable D64 at <paramref name="path"/> and mounts it,
    /// if the current machine has a disk drive - PET only (VIC-20 has none in this repo). See
    /// <see cref="PetMachineViewModel.NewDisk"/>.</summary>
    public void NewDisk(string path)
    {
        if (CurrentMachine is PetMachineViewModel pet)
            pet.NewDisk(path);
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
        if (CurrentMachine is Vic20MachineViewModel vic20)
            vic20.NewTape();
    }

    public void HandleKey(Key key, HostKeyEventKind kind) => CurrentMachine.HandleKey(key, kind);

    public void Dispose()
    {
        _timer.Stop();
        CurrentMachine.Dispose();
    }
}
