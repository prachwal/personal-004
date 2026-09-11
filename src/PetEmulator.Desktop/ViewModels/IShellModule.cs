namespace PetEmulator.Desktop.ViewModels;

public interface IShellModule : IDisposable
{
    string WindowTitle { get; }

    string StatusText { get; }
}
