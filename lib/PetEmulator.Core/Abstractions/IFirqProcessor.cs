namespace PetEmulator.Core;

/// <summary>Optional fast-interrupt capability of CPU families such as MC6809.</summary>
public interface IFirqProcessor
{
    void SetFIRQ(bool active);
}
