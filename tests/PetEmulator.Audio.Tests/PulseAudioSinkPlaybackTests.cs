using FluentAssertions;
using NUnit.Framework;

namespace PetEmulator.Audio.Tests;

/// <summary>A 440 Hz sine wave, free-running forever - enough to drive
/// <see cref="PulseAudioSink"/> for a real playback smoke test.</summary>
internal sealed class SineWaveSource(uint sampleRate = 44100, int channels = 1) : IAudioSource
{
    private double _phase;

    public AudioFormat Format { get; } = new(sampleRate, channels);

    public int Render(Span<AudioFrame> destination)
    {
        var step = 440.0 * 2 * Math.PI / Format.SampleRate;
        for (var i = 0; i < destination.Length; i++)
        {
            var sample = (float)(Math.Sin(_phase) * 0.2);
            destination[i] = new AudioFrame(sample, sample);
            _phase += step;
        }
        return destination.Length;
    }
}

/// <summary>Actually plays audio against this machine's real PulseAudio/WSLg server - not a pure
/// unit test. Excluded from the default run (<c>dotnet test</c> with no filter never hits a real
/// audio device in CI or a headless box); run explicitly with:
/// <c>dotnet test --filter "FullyQualifiedName~PulseAudioSinkPlaybackTests"</c> on a box that has
/// one, per docs/audio-wsl-backend.md.</summary>
[Explicit("Plays real audio through the host's PulseAudio/WSLg server - run manually to verify the backend actually works, not on every CI run.")]
public sealed class PulseAudioSinkPlaybackTests
{
    [Test]
    public void Start_PlaysASineWaveThroughTheRealHostBackend_WithNoError()
    {
        var source = new SineWaveSource();
        using var output = AudioOutputFactory.CreateDefault();

        output.Start(source);
        Thread.Sleep(3000);
        var wasPlaying = output.IsPlaying;
        var error = output.LastError;
        output.Stop();

        error.Should().BeNull();
        wasPlaying.Should().BeTrue();
    }
}
