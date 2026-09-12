using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace PetEmulator.Desktop.ViewModels;

/// <summary>Bindable state for the VIC-20 cartridge indicator.</summary>
public interface ICartridgeViewModel
{
    bool CartridgeLoaded { get; }

    string CartridgeName { get; }

    IBrush CartridgeIconBrush { get; }

    bool CanEjectCartridge { get; }

    IReadOnlyList<CartridgeResourceRow> CartridgeResources { get; }

    IRelayCommand EjectCartridgeCommand { get; }
}

public sealed record CartridgeResourceRow(string Name, string Range, string Kind, string Access)
{
    public string Summary => $"{Name}  {Range}  {Kind}  {Access}";
}

public sealed record LoadedCartridgeRow(string Name, string Path, IReadOnlyList<CartridgeResourceRow> Resources);
