using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Desktop.Services;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class CartridgeMountViewModel : ObservableObject
{
    private readonly Vic20MachineViewModel _machine;
    private readonly IFilePickerService _filePicker;

    [ObservableProperty]
    private string _cartridgeName;

    [ObservableProperty]
    private bool _canEject;

    [ObservableProperty]
    private IReadOnlyList<CartridgeResourceRow> _resources = [];

    [ObservableProperty]
    private IReadOnlyList<CartridgeMountRow> _loadedCartridges = [];

    [ObservableProperty]
    private string? _selectedPath;

    [ObservableProperty]
    private string? _selectedPluginPath;

    [ObservableProperty]
    private string? _selectedImagePath;

    [ObservableProperty]
    private string? _errorMessage;

    public CartridgeMountViewModel(Vic20MachineViewModel machine, IFilePickerService filePicker)
    {
        _machine = machine;
        _filePicker = filePicker;
        _cartridgeName = machine.CartridgeName;
        Refresh();
    }

    [RelayCommand]
    private async Task Browse()
    {
        var path = await _filePicker.PickCartridgeToOpenAsync();
        if (path is not null)
        {
            SelectedPath = path;
            ErrorMessage = null;
        }
    }

    [RelayCommand]
    private async Task BrowsePlugin()
    {
        var path = await _filePicker.PickCartridgePluginToOpenAsync();
        if (path is not null)
        {
            SelectedPluginPath = path;
            ErrorMessage = null;
        }
    }

    [RelayCommand]
    private async Task BrowseImage()
    {
        var path = await _filePicker.PickCartridgeToOpenAsync();
        if (path is not null)
        {
            SelectedImagePath = path;
            ErrorMessage = null;
        }
    }

    [RelayCommand]
    private void Load()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
            return;

        try
        {
            _machine.LoadCartridge(SelectedPath);
            SelectedPath = null;
            ErrorMessage = null;
            Refresh();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void LoadPlugin()
    {
        if (string.IsNullOrWhiteSpace(SelectedPluginPath)
            || string.IsNullOrWhiteSpace(SelectedImagePath))
            return;

        try
        {
            _machine.LoadCartridgePlugin(SelectedPluginPath, SelectedImagePath);
            SelectedPluginPath = null;
            SelectedImagePath = null;
            ErrorMessage = null;
            Refresh();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void Refresh()
    {
        CartridgeName = _machine.CartridgeName;
        CanEject = _machine.CanEjectCartridge;
        Resources = _machine.GetCartridgeResources();
        LoadedCartridges = _machine.LoadedCartridges
            .Select(row => new CartridgeMountRow(row.Name, row.Path, row.Resources, EjectCommand))
            .ToArray();
    }

    [RelayCommand]
    private void Eject(string path)
    {
        _machine.EjectCartridge(path);
        Refresh();
    }
}

public sealed record CartridgeMountRow(
    string Name,
    string Path,
    IReadOnlyList<CartridgeResourceRow> Resources,
    IRelayCommand EjectCommand);
