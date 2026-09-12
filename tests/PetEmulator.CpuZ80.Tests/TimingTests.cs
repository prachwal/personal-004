using PetEmulator.CpuZ80.Timing;

namespace PetEmulator.CpuZ80.Tests;

public sealed class TimingTests
{
    [Fact]
    public void ClockAdvancesOnlyWhileRunningAndNotPaused()
    {
        var clock = new EmulationClock();

        clock.Advance(10);
        clock.Start();
        clock.Advance(20);
        clock.Pause();
        clock.Advance(30);
        clock.ResumeEmulation();
        clock.Advance(40);

        Assert.Equal(60, clock.TStates);
    }

    [Fact]
    public void ClockResetClearsState()
    {
        var clock = new EmulationClock();
        clock.Start();
        clock.Advance(10);

        clock.Reset();

        Assert.Equal(0, clock.TStates);
        Assert.False(clock.IsRunning);
        Assert.False(clock.IsPaused);
    }
}
