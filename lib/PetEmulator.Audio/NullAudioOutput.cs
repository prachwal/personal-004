namespace PetEmulator.Audio;

/// <summary>
/// No-op audio backend for tests and hosts where audio output is intentionally disabled.
/// It accepts sources without starting a worker thread or touching a host audio API.
/// </summary>
public sealed class NullAudioOutput : IAudioOutput
{
    public bool IsPlaying => false;

    public string? LastError => null;

    public void Start(IAudioSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
    }

    public void Stop()
    {
    }

    public void Dispose()
    {
    }
}
