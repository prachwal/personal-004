namespace PetEmulator.Desktop.ViewModels;

/// <summary>Capability for machines that can create a new writable disk image.</summary>
public interface INewDiskViewModel
{
    void NewDisk(string path);
}
