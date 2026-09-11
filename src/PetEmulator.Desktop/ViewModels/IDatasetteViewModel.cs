using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace PetEmulator.Desktop.ViewModels;

public interface IDatasetteViewModel
{
    IBrush TapeIconBrush { get; }

    IRelayCommand PlayTapeCommand { get; }

    IRelayCommand StopTapeCommand { get; }

    IRelayCommand EjectTapeCommand { get; }
}
