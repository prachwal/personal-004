using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Desktop.Infrastructure;
using System.Diagnostics;
using PetEmulator.Desktop.Models;
using PetEmulator.Desktop.Services;
using PetEmulator.Desktop.Views;
using PetEmulator.Pet;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Vic20;
using PetEmulator.Trs80;
using PetEmulator.Cpc464;

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
    private int _tickErrorCount;

    private static readonly ILogger ShellLog = EmulatorLogging.CreateLogger("Shell");
    private static readonly ILogger TapeLog = EmulatorLogging.CreateLogger("Tape");
    private static readonly ILogger DiskLog = EmulatorLogging.CreateLogger("Disk");
    private static readonly ILogger VicLog = EmulatorLogging.CreateLogger("VIC20");
    private static readonly ILogger TickLog = EmulatorLogging.CreateLogger("Tick");

    [ObservableProperty]
    private IShellModule _currentModule;

    /// <summary>Last failure detail, also shown in the side panel - previously
    /// machine-switch and media errors only went to <c>Console.Error</c> (invisible in a
    /// WinExe with no console) or were swallowed by the command dispatcher entirely.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _lastError = string.Empty;

    /// <summary>Whether <see cref="LastError"/> currently holds a failure - drives the ERROR
    /// block visibility in the side panel so it doesn't occupy space when empty.</summary>
    public bool HasError => !string.IsNullOrEmpty(LastError);

    /// <summary>Whether the left status panel is expanded (false = collapsed to a narrow strip
    /// with just the expand button).</summary>
    [ObservableProperty]
    private bool _isStatusPanelExpanded = true;

    /// <summary>Opens the session log file in the associated viewer (see
    /// <see cref="EmulatorLogging.LogFilePath"/>) - the file path itself is no longer pasted
    /// into the UI, this button is its only surface.</summary>
    [RelayCommand]
    private void OpenLog()
    {
        try
        {
            ShellLog.LogInformation("Opening session log: {LogFile}.", EmulatorLogging.LogFilePath);
            Process.Start(new ProcessStartInfo(EmulatorLogging.LogFilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ReportError(ShellLog, "Failed to open the session log.", ex);
        }
    }

    public MainWindowViewModel(IFilePickerService filePicker)
    {
        _filePicker = filePicker;
        _romsRoot = RomsRootLocator.Find();
        ShellLog.LogInformation("MainWindowViewModel starting with ROMs root {RomsRoot}.", _romsRoot);

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
              new ModuleMenuEntry("Amstrad CPC464", () => new Cpc464MachineViewModel(_romsRoot)),
              new ModuleMenuEntry("Amstrad CPC6128", () => new Cpc6128MachineViewModel(_romsRoot)),
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
        try
        {
            _currentModule = initialCreate();
            ShellLog.LogInformation( $"Initial module: {_currentModule.GetType().Name} ({_currentModule.WindowTitle}).");
        }
        catch (Exception ex)
        {
            ReportError(ShellLog, "Failed to create the initial machine module.", ex);
            throw;
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += (_, _) =>
        {
            if (CurrentModule is not IMachineViewModel machine)
                return;

            try
            {
                machine.Tick();
                _tickErrorCount = 0;
            }
            catch (Exception ex)
            {
                // A throwing render loop used to die silently (or kill the dispatcher with no
                // trace in a WinExe). Log the first failure in full, then throttle to every
                // 50th so a permanently broken machine does not flood the log at 50 Hz.
                _tickErrorCount++;
                if (_tickErrorCount == 1 || _tickErrorCount % 50 == 0)
                    ReportError(TickLog, $"Tick failed {_tickErrorCount}x for {machine.GetType().Name}.", ex);
            }
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
                VicLog.LogInformation( "Program profile cleared (empty).");
                return;
            }

            VicLog.LogInformation( $"Loading program profile '{profile.Name}'.");
            current.LoadProgramProfile(profile);
            current.ProgramProfileSelector.ErrorMessage = null;
            VicLog.LogInformation( $"Program profile '{profile.Name}' loaded.");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException or InvalidOperationException)
        {
            VicLog.LogError(ex, $"Failed to load program profile '{profile.Name}'.");
            current.ProgramProfileSelector.ErrorMessage = $"{ex.Message} (see {EmulatorLogging.LogFilePath})";
        }
    }


    /// <summary>Raised by <see cref="ExitCommand"/> - the View closes the window.</summary>
    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void Reset()
    {
        if (CurrentModule is not IMachineViewModel machine)
            return;

        try
        {
            ShellLog.LogDebug( $"Resetting {machine.GetType().Name}.");
            machine.Reset();
        }
        catch (Exception ex)
        {
            ReportError(ShellLog, $"Reset failed for {machine.GetType().Name}.", ex);
        }
    }

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ToggleStatusPanel() => IsStatusPanelExpanded = !IsStatusPanelExpanded;

    [RelayCommand]
    private void SwitchMachine(ModuleMenuEntry entry)
    {
        var create = entry.Create;
        if (create is null)
            return;

        ShellLog.LogInformation( $"Switching machine to '{entry.Label}'.");
        IShellModule next;
        try
        {
            next = create();
            ShellLog.LogInformation( $"Switched to {next.GetType().Name} ({next.WindowTitle}).");
        }
        catch (Exception ex)
        {
            // Previously this exception vanished inside the RelayCommand dispatch with no
            // error anywhere - e.g. PlatformNotSupportedException from the missing Windows
            // audio backend left the old machine on screen and nothing in any log.
            ReportError(ShellLog, $"Failed to switch machine to '{entry.Label}'.", ex);
            return;
        }

        var old = CurrentModule;
        CurrentModule = next;
        LastError = string.Empty;
        try
        {
            old.Dispose();
        }
        catch (Exception ex)
        {
            ShellLog.LogWarning(ex, "Disposing {Module} failed.", old.GetType().Name);
        }
    }

    /// <summary>Loads a VICE-style .tap file into the current machine's datasette, if it has one -
    /// supported machine. The file-picker dialog itself is Avalonia-specific glue that lives in
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
            ReportError(TapeLog, "Load tape dialog failed.", ex);
        }
    }

    /// <summary>Second-deck loader for machines with two tape transports (PET cassette #2) -
    /// see <see cref="ISecondTapeViewModel"/>.</summary>
    [RelayCommand]
    private async Task LoadTape2()
    {
        try
        {
            var path = await _filePicker.PickTapeToOpenAsync();
            if (path is not null)
                LoadTape2(path);
        }
        catch (Exception ex)
        {
            ReportError(TapeLog, "Load tape #2 dialog failed.", ex);
        }
    }

    public void LoadTape(string path)
    {
        if (CurrentModule is not ITapeViewModel tape)
        {
            TapeLog.LogWarning( $"Current module {CurrentModule.GetType().Name} has no tape device; ignoring '{path}'.");
            return;
        }

        try
        {
            TapeLog.LogInformation( $"Loading tape '{path}' ({DescribeFile(path)}) into {CurrentModule.GetType().Name}.");
            tape.LoadTape(path);
            TapeLog.LogInformation( $"Tape loaded: '{path}'.");
        }
        catch (Exception ex)
        {
            ReportError(TapeLog, $"Failed to load tape '{path}'.", ex);
        }
    }

    public void LoadTape2(string path)
    {
        if (CurrentModule is not ISecondTapeViewModel second)
        {
            TapeLog.LogWarning("Current module {Module} has no second tape deck; ignoring '{Path}'.",
                CurrentModule.GetType().Name, path);
            return;
        }

        try
        {
            TapeLog.LogInformation("Loading tape #2 '{Path}' ({Size}) into {Module}.",
                path, DescribeFile(path), CurrentModule.GetType().Name);
            second.LoadTape2(path);
            TapeLog.LogInformation("Tape #2 loaded: '{Path}'.", path);
        }
        catch (Exception ex)
        {
            ReportError(TapeLog, $"Failed to load tape #2 '{path}'.", ex);
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
            ReportError(DiskLog, "Load disk dialog failed.", ex);
        }
    }

    public void LoadDisk(string path)
    {
        if (CurrentModule is not IDiskDriveViewModel disk)
        {
            DiskLog.LogWarning( $"Current module {CurrentModule.GetType().Name} has no disk drive; ignoring '{path}'.");
            return;
        }

        try
        {
            DiskLog.LogInformation( $"Loading disk '{path}' ({DescribeFile(path)}) into {CurrentModule.GetType().Name}.");
            disk.LoadDisk(path);
            DiskLog.LogInformation( $"Disk loaded: '{path}'.");
        }
        catch (Exception ex)
        {
            ReportError(DiskLog, $"Failed to load disk '{path}'.", ex);
        }
    }

    /// <summary>Creates a fresh, formatted, writable D64 at <paramref name="path"/> and mounts it,
    /// if the current machine has a disk drive. See <see cref="PetMachineViewModel.NewDisk"/>.
    /// </summary>
    public void NewDisk(string path)
    {
        if (CurrentModule is not INewDiskViewModel disk)
        {
            DiskLog.LogWarning( $"Current module {CurrentModule.GetType().Name} cannot create disks; ignoring '{path}'.");
            return;
        }

        try
        {
            DiskLog.LogInformation( $"Creating new disk '{path}' in {CurrentModule.GetType().Name}.");
            disk.NewDisk(path);
            DiskLog.LogInformation( $"New disk created: '{path}'.");
        }
        catch (Exception ex)
        {
            ReportError(DiskLog, $"Failed to create disk '{path}'.", ex);
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
            ReportError(DiskLog, "New disk dialog failed.", ex);
        }
    }

    /// <summary>Puts a fresh, empty, writable tape in the current machine's datasette, if it has
    /// one - VIC-20 only for now (real SAVE support - see docs/vic20/tape.md; PET has no SAVE
    /// emulation yet). No file dialog needed (nothing to pick a path for yet), so this is a plain
    /// command, not glue through <see cref="MainWindow"/>'s code-behind like <see cref="LoadTape"/>.</summary>
    [RelayCommand]
    private void NewTape()
    {
        if (CurrentModule is not INewTapeViewModel tape)
        {
            TapeLog.LogWarning( $"Current module {CurrentModule.GetType().Name} cannot create tapes.");
            return;
        }

        try
        {
            TapeLog.LogInformation( $"Creating new blank tape in {CurrentModule.GetType().Name}.");
            tape.NewTape();
        }
        catch (Exception ex)
        {
            ReportError(TapeLog, "Failed to create a new blank tape.", ex);
        }
    }

    public void HandleKey(Key key, HostKeyEventKind kind)
    {
        if (CurrentModule is IMachineViewModel machine)
            machine.HandleKey(key, kind);
    }

    public void Dispose()
    {
        _timer.Stop();
        ShellLog.LogInformation( $"Disposing {CurrentModule.GetType().Name}.");
        CurrentModule.Dispose();
    }

    /// <summary>Logs <paramref name="exception"/> in full and surfaces a one-line summary in
    /// the side panel (<see cref="LastError"/>) - the file log always has the complete chain.</summary>
    private void ReportError(ILogger log, string message, Exception exception)
    {
        log.LogError(exception, "{Message}", message);
        LastError = $"{message} {exception.GetType().Name}: {exception.Message} (see {EmulatorLogging.LogFilePath})";
    }

    private static string DescribeFile(string path)
    {
        try
        {
            return File.Exists(path) ? $"{new FileInfo(path).Length} B" : "(file does not exist)";
        }
        catch (Exception ex)
        {
            return $"(stat failed: {ex.Message})";
        }
    }
}
