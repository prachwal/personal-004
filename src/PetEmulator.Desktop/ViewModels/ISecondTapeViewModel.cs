namespace PetEmulator.Desktop.ViewModels;

/// <summary>Capability for machines with a second, independently loadable tape deck
/// (currently only PET's cassette #2).</summary>
public interface ISecondTapeViewModel
{
    void LoadTape2(string path);
}
