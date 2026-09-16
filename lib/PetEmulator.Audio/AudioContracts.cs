namespace PetEmulator.Audio;

/// <summary>
/// Generates audio from an emulated sound device (a SID/AY chip, a machine's simple CB2/PIA
/// beeper, ...). Ported from personal-002's <c>Abstraction.Audio.IAudioSource</c> - trimmed to
/// just this repo's actual consumer shape (a host sink pulling rendered frames on its own thread);
/// personal-002's sibling contracts (<c>IBitAudioSource</c>, <c>IDmaAudioSource</c>,
/// <c>IAudioMixer</c>) had no real implementer there either and aren't carried over here - add
/// back only once an actual chip needs that shape (YAGNI).
/// </summary>
public interface IAudioSource
{
    /// <summary>The format this source renders in - fixed for the source's lifetime.</summary>
    AudioFormat Format { get; }

    /// <summary>Renders up to <paramref name="destination"/>'s length and returns the count of
    /// frames actually written. A source that has nothing left to render (a one-shot sample, not
    /// a free-running chip) returns 0, which the host sink treats as "stop".</summary>
    int Render(Span<AudioFrame> destination);
}

/// <summary>Emulated machine audio endpoint. Machines without physical audio expose a null device.</summary>
public interface IAudioDevice
{
    IAudioSource? Source { get; }
}

/// <summary>Audio endpoint for a machine that has no modeled physical output.</summary>
public sealed class NullAudioDevice : IAudioDevice
{
    public IAudioSource? Source => null;
}

/// <summary>Audio endpoint backed by one emulated audio source.</summary>
public sealed class AudioDevice(IAudioSource source) : IAudioDevice
{
    public IAudioSource Source { get; } = source ?? throw new ArgumentNullException(nameof(source));
}

/// <summary>
/// The platform seam: plays an <see cref="IAudioSource"/> on the host's actual speakers. A
/// consumer (a machine's ViewModel, the CLI) codes against this interface, never against a
/// concrete backend - swapping WSL's <see cref="PulseAudioSink"/> for a native Windows backend
/// (WASAPI, say) later means adding one new class here and changing <see cref="AudioOutputFactory"/>,
/// nothing at any call site.
/// </summary>
public interface IAudioOutput : IDisposable
{
    /// <summary>Whether a background thread is actively pulling frames and writing them to the
    /// host device right now.</summary>
    bool IsPlaying { get; }

    /// <summary>Set when playback couldn't start or failed mid-stream - an implementation never
    /// throws for a missing/unreachable host audio server, so a caller without one (CI, a
    /// platform with no backend wired up yet) keeps running with audio silently disabled instead
    /// of crashing. Cleared on the next successful <see cref="Start"/>.</summary>
    string? LastError { get; }

    /// <summary>Starts pulling frames from <paramref name="source"/> on a dedicated background
    /// thread. Stops and restarts if already playing another source.</summary>
    void Start(IAudioSource source);

    /// <summary>Stops playback and releases the host device. Safe to call when not playing.</summary>
    void Stop();
}

/// <summary>Describes a PCM stream: how many samples per second and how many channels each frame
/// carries. Samples are always 32-bit float (matches every host audio backend this repo has
/// evidence for - PulseAudio's <c>PA_SAMPLE_FLOAT32LE</c>, SDL3's <c>SDL_AUDIO_F32</c>); add a
/// sample-type field only if a backend without float32 support actually shows up.</summary>
public readonly record struct AudioFormat(uint SampleRate, int Channels);

/// <summary>One PCM frame. <see cref="Right"/> is ignored by a mono <see cref="AudioFormat"/>
/// (<c>Channels == 1</c>) - a source still fills both fields uniformly rather than exposing two
/// frame shapes, so callers don't need to branch on channel count.</summary>
public readonly record struct AudioFrame(float Left, float Right);
