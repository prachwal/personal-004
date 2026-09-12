namespace PetEmulator.CpuZ80.Timing;

public interface IEmulationClock
{
    long TStates { get; }
    bool IsRunning { get; }
    bool IsPaused { get; }
    void Advance(int tStates);
    void Start();
    void Pause();
    void ResumeEmulation();
    void StopEmulation();
    void Reset();
}
