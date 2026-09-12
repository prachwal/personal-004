using PetEmulator.CpuZ80.Interrupts;

namespace PetEmulator.CpuZ80.Tests;

public sealed class InterruptTests
{
    [Fact]
    public void InterruptLinesExposeLevelStateAndCanBeCleared()
    {
        var lines = new InterruptLines();

        lines.SetInt(true);
        lines.SetNmi(true);
        Assert.True(lines.IntAsserted);
        Assert.True(lines.NmiAsserted);

        lines.Clear();

        Assert.False(lines.IntAsserted);
        Assert.False(lines.NmiAsserted);
    }
}
