using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Audio.Tests;

/// <summary>Covers <see cref="PulseAudioSink.Interleave"/> only - the one piece of the sink that
/// is pure and doesn't need a real PulseAudio server. The P/Invoke playback loop itself is
/// exercised manually (see docs/desktop/audio-wsl-backend.md); it has no meaningful behavior to assert
/// without an actual WSLg audio server.</summary>
public sealed class PulseAudioSinkTests
{
    [Test]
    public void Interleave_WithStereoFormat_WritesLeftThenRightPerFrame()
    {
        AudioFrame[] frames = [new(1f, -1f), new(0.5f, -0.5f)];
        var destination = new byte[frames.Length * 2 * sizeof(float)];

        var written = PulseAudioSink.Interleave(frames, channels: 2, destination);

        written.Should().Be(destination.Length);
        BitConverter.ToSingle(destination, 0).Should().Be(1f);
        BitConverter.ToSingle(destination, 4).Should().Be(-1f);
        BitConverter.ToSingle(destination, 8).Should().Be(0.5f);
        BitConverter.ToSingle(destination, 12).Should().Be(-0.5f);
    }

    [Test]
    public void Interleave_WithMonoFormat_WritesOnlyLeftPerFrame_IgnoringRight()
    {
        AudioFrame[] frames = [new(1f, 999f), new(0.5f, 999f)];
        var destination = new byte[frames.Length * sizeof(float)];

        var written = PulseAudioSink.Interleave(frames, channels: 1, destination);

        written.Should().Be(destination.Length);
        BitConverter.ToSingle(destination, 0).Should().Be(1f);
        BitConverter.ToSingle(destination, 4).Should().Be(0.5f);
    }

    [Test]
    public void Interleave_WithEmptySpan_ReturnsZero()
    {
        var written = PulseAudioSink.Interleave([], channels: 2, []);

        written.Should().Be(0);
    }
}
