namespace PetEmulator.Core;

/// <summary>Captures and restores the complete state of a concrete machine.</summary>
public interface IMachineStateStore<TSnapshot> where TSnapshot : IMachineSnapshot
{
    TSnapshot CaptureState();
    void RestoreState(TSnapshot state);
}
