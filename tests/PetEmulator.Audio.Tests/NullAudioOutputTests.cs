using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Audio.Tests;

public sealed class NullAudioOutputTests
{
    [Test]
    public void Start_WithSource_LeavesPlaybackDisabledWithoutAnError()
    {
        using var output = new NullAudioOutput();

        output.Start(new SilentSource());

        output.IsPlaying.Should().BeFalse();
        output.LastError.Should().BeNull();
    }

    [Test]
    public void Start_WithNullSource_ThrowsArgumentNullException()
    {
        using var output = new NullAudioOutput();

        var act = () => output.Start(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void StopAndDispose_AreSafeBeforeAndAfterStart()
    {
        using var output = new NullAudioOutput();

        output.Stop();
        output.Start(new SilentSource());
        output.Stop();
        output.Dispose();

        output.IsPlaying.Should().BeFalse();
    }

    private sealed class SilentSource : IAudioSource
    {
        public AudioFormat Format => new(44100, 2);

        public int Render(Span<AudioFrame> destination) => 0;
    }
}
