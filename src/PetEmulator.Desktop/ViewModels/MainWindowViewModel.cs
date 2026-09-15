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
using PetEmulator.Trs80;

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

        SuperPetChoices =
        [
            new ModuleMenuEntry("6502", () => new PetMachineViewModel(PetProfileCatalog.SuperPet6502, Path.Combine(_romsRoot, "pet"))),
            new ModuleMenuEntry("6809", () => new PetMachineViewModel(PetProfileCatalog.SuperPet6809, Path.Combine(_romsRoot, "pet")))
        ];

        ModuleChoices =
        [
              new ModuleMenuEntry("PET 20xx", null, CreateProfileChoices(PetProfileCatalog.Pet2001_8, PetProfileCatalog.Pet2001_32)),
              new ModuleMenuEntry("CBM 30xx", null, CreateProfileChoices(PetProfileCatalog.Cbm3008, PetProfileCatalog.Cbm3016, PetProfileCatalog.Cbm3032)),
              new ModuleMenuEntry("CBM 40xx", null, CreateProfileChoices(
                  PetProfileCatalog.Cbm4008Crtc40N60,
                  PetProfileCatalog.Cbm4016Crtc40N60,
                  PetProfileCatalog.Cbm4032,
                  PetProfileCatalog.Cbm4032Crtc40N50,
                  PetProfileCatalog.Cbm4032Crtc40B50,
                  PetProfileCatalog.Cbm4032Crtc40B60)),
              new ModuleMenuEntry("CBM 80xx", null, CreateProfileChoices(
                  PetProfileCatalog.Cbm8032,
                  PetProfileCatalog.Cbm8032Crtc80B50,
                  PetProfileCatalog.Cbm8016Converted80N50,
                  PetProfileCatalog.Converted80NUnknown)),
              new ModuleMenuEntry("SuperPET", null, SuperPetChoices),
              new ModuleMenuEntry("VIC-20", CreateVic20),
              new ModuleMenuEntry("Kaypro II", () => new KayproMachineViewModel(_romsRoot)),
              new ModuleMenuEntry("TRS-80 Model I", () => new Trs80MachineViewModel(_romsRoot)),
         ];

        ToolChoices =
        [
              new ModuleMenuEntry("Chip Tester", () => new ChipTesterViewModel(_romsRoot)),
              new ModuleMenuEntry("Media Tester", () => new MediaTesterViewModel(_filePicker)),
              new ModuleMenuEntry("Font / Glyph Viewer", () => new FontViewerViewModel(_romsRoot)),
              new ModuleMenuEntry("CPU Opcode Stepper", () => new OpcodeStepperViewModel()),
              new ModuleMenuEntry("Keyboard Matrix", () => new KeyboardMatrixViewModel()),
         ];

        var initialCreate = ModuleChoices
            .SelectMany(entry => entry.Children ?? Array.Empty<ModuleMenuEntry>())
            .FirstOrDefault(entry => entry.Create is not null)?.Create
            ?? throw new InvalidOperationException("The first machine menu entry must be selectable.");
        _currentModule = initialCreate();

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

    /// <summary>Developer and media tools, kept separate from emulated machine profiles.</summary>
    public IReadOnlyList<ModuleMenuEntry> ToolChoices { get; }

    /// <summary>The two processor modes behind the physical SuperPET switch.</summary>
    public IReadOnlyList<ModuleMenuEntry> SuperPetChoices { get; }

    private IReadOnlyList<ModuleMenuEntry> CreateProfileChoices(params PetProfile[] profiles) =>
        profiles.Select(profile =>
            new ModuleMenuEntry(profile.Name, () => new PetMachineViewModel(profile, Path.Combine(_romsRoot, "pet"))))
        .ToArray();

    private Vic20MachineViewModel CreateVic20()
    {
        var machine = new Vic20MachineViewModel(Path.Combine(_romsRoot, "vic20"));
        machine.ProgramProfileSelector.ProfileSelected += (_, profile) =>
            LoadVic20ProgramProfile(machine, profile);
        return machine;
    }

    private void LoadVic20ProgramProfile(Vic20MachineViewModel current, Vic20ProgramProfile profile)
    {
        if (!ReferenceEquals(CurrentModule, current))
            return;

        try
        {
            if (profile.IsEmpty)
            {
                current.EjectAllCartridges();
                current.ProgramProfileSelector.ErrorMessage = null;
                return;
            }

            current.LoadProgramProfile(profile);
            current.ProgramProfileSelector.ErrorMessage = null;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException or InvalidOperationException)
        {
            current.ProgramProfileSelector.ErrorMessage = ex.Message;
        }
    }


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
        var create = entry.Create;
        if (create is null)
            return;

        var old = CurrentModule;
        CurrentModule = create();
        old.Dispose();
    }

    /// <summary>Loads a VICE-style .tap file into the current machine's datasette, if it has one -
    /// both <see cref="PetMachineViewModel"/> and <see cref="Vic20MachineViewModel"/> do (see
    /// docs/vic20/tape.md). The file-picker dialog itself is Avalonia-specific glue that lives in
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
            case Trs80MachineViewModel trs80: trs80.LoadTape(path); break;
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
            case KayproMachineViewModel kaypro: kaypro.LoadDisk(path); break;
            case Trs80MachineViewModel trs80: trs80.LoadDisk(path); break;
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
    /// one - VIC-20 only for now (real SAVE support - see docs/vic20/tape.md; PET has no SAVE
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
