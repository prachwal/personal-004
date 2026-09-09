namespace PetEmulator.Pet.Ieee488;

/// <summary>
/// A device a <see cref="PetIeeeBus"/> can address as a listener or talker (e.g.
/// <see cref="CbmDos.PetIeeeDiskDrive"/>).
/// </summary>
public interface IIeeeDevice
{
    int PrimaryAddress { get; }
    bool DataAvailable { get; }
    void OpenForRead(byte secondaryAddr);
    void OpenForWrite(byte secondaryAddr);
    void Close();
    void Write(byte data);
    bool TryRead(out byte data);
}
