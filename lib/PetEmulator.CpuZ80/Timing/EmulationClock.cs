namespace PetEmulator.CpuZ80.Timing;

public sealed class EmulationClock : IEmulationClock
{
    public long TStates { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsPaused { get; private set; }

    public void Advance(int tStates)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tStates);

        if (IsRunning && !IsPaused)
            TStates += tStates;
    }

    public void Start()
    {
        IsRunning = true;
        IsPaused = false;
    }

    public void Pause()
    {
        if (IsRunning)
            IsPaused = true;
    }

    public void ResumeEmulation()
    {
        if (IsRunning)
            IsPaused = false;
    }

    public void StopEmulation()
    {
        IsRunning = false;
        IsPaused = false;
    }

    public void Reset()
    {
        TStates = 0;
        IsRunning = false;
        IsPaused = false;
    }
}
