using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Audio;
using PetEmulator.Vic20.Tests.Roms;

namespace PetEmulator.Vic20.Tests;

public sealed class Vic20AudioTests
{
    [Test]
    public void VicRegisters_MapAudioControlsThroughTheMemoryBus()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));

        machine.Memory.Write(0x900A, 0x81);
        machine.Memory.Write(0x900B, 0x82);
        machine.Memory.Write(0x900C, 0x83);
        machine.Memory.Write(0x900D, 0x84);
        machine.Memory.Write(0x900E, 0x0D);

        machine.Vic.Oscillator1Enabled.Should().BeTrue();
        machine.Vic.Oscillator2Enabled.Should().BeTrue();
        machine.Vic.Oscillator3Enabled.Should().BeTrue();
        machine.Vic.NoiseEnabled.Should().BeTrue();
        machine.Vic.Volume.Should().Be(0x0D);
        machine.Vic.Oscillator1Frequency.Should().BeGreaterThan(0);
        machine.Vic.Oscillator2Frequency.Should().BeGreaterThan(0);
        machine.Vic.Oscillator3Frequency.Should().BeGreaterThan(0);
        machine.Vic.NoiseFrequency.Should().BeGreaterThan(0);
    }

    [Test]
    public void Reset_ClearsVicAudioRegisters()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
        machine.Memory.Write(0x900A, 0xFF);
        machine.Memory.Write(0x900E, 0x0F);

        machine.Reset();

        machine.Vic.Oscillator1Enabled.Should().BeFalse();
        machine.Vic.NoiseEnabled.Should().BeFalse();
        machine.Vic.Volume.Should().Be(0);
    }

    [Test]
    public void AudioRegisters_ProduceSamplesThroughTheMachineBus()
    {
        var machine = new Vic20Machine(RomLocator.Directory("kernal.bin"));
        machine.Memory.Write(0x900A, 0x80 | 0x40);
        machine.Memory.Write(0x900E, 0x0F);

        var frames = new AudioFrame[64];
        machine.Vic.Render(frames).Should().Be(frames.Length);

        frames.Should().Contain(frame => frame.Left != 0 || frame.Right != 0);
    }
}
