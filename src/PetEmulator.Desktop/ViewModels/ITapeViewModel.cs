namespace PetEmulator.Desktop.ViewModels;

/// <summary>Capability for machines that can accept a tape image. It intentionally does not
/// imply physical transport controls: machines such as the TRS-80 expose only their cassette
/// port and let firmware control the recorder.</summary>
public interface ITapeViewModel
{
    void LoadTape(string path);
}
