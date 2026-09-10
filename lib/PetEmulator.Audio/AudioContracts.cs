namespace PetEmulator.Audio;

/// <summary>
/// Generates audio from an emulated sound device (a SID/AY chip, a machine's simple CB2/PIA
/// beeper, ...). Ported from personal-002's <c>Abstraction.Audio.IAudioSource</c> - trimmed to
/// just this repo's actual consumer shape (a host sink pulling rendered frames on its own thread);
/// personal-002's sibling contracts (<c>IAudioOutput</c>, <c>IBitAudioSource</c>,
/// <c>IDmaAudioSource</c>, <c>IAudioMixer</c>) had no real implementer there either and aren't
/// carried over here - add back only once an actual chip needs that shape (YAGNI).
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

/// <summary>Describes a PCM stream: how many samples per second and how many channels each frame
/// carries. Samples are always 32-bit float (matches every host audio backend this repo has
/// evidence for - PulseAudio's <c>PA_SAMPLE_FLOAT32LE</c>, SDL3's <c>SDL_AUDIO_F32</c>); add a
/// sample-type field only if a backend without float32 support actually shows up.</summary>
public readonly record struct AudioFormat(uint SampleRate, int Channels);

/// <summary>One PCM frame. <see cref="Right"/> is ignored by a mono <see cref="AudioFormat"/>
/// (<c>Channels == 1</c>) - a source still fills both fields uniformly rather than exposing two
/// frame shapes, so callers don't need to branch on channel count.</summary>
public readonly record struct AudioFrame(float Left, float Right);
