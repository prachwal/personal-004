namespace PetEmulator.Audio;

/// <summary>
/// Picks the right <see cref="IAudioOutput"/> for the host this process is running on. The only
/// place in this project that knows a concrete backend exists - every consumer (a machine's
/// ViewModel, the CLI) calls <see cref="CreateDefault"/> and talks to the result purely through
/// <see cref="IAudioOutput"/>, so adding a Windows backend later is "write the class, add one
/// branch here" with zero changes anywhere else.
/// </summary>
public static class AudioOutputFactory
{
    public static IAudioOutput CreateNull() => new NullAudioOutput();

    /// <summary>Creates the default backend for the current OS. Linux (WSL included - this
    /// repo's only verified host, see docs/desktop/audio-wsl-backend.md) gets <see cref="PulseAudioSink"/>.
    /// No other platform has a backend implemented yet - <see cref="PulseAudioSink"/> itself never
    /// throws for a missing server, but there is nothing to even attempt one on Windows/macOS
    /// today, so this throws early and by name rather than returning a sink that would silently
    /// do nothing.</summary>
    /// <exception cref="PlatformNotSupportedException">No <see cref="IAudioOutput"/> exists yet
    /// for the current OS - add one (e.g. a WASAPI-backed sink for Windows) and a branch here.</exception>
    public static IAudioOutput CreateDefault() => OperatingSystem.IsLinux()
        ? new PulseAudioSink()
        : throw new PlatformNotSupportedException(
            $"No IAudioOutput backend implemented for {Environment.OSVersion.Platform} yet - " +
            "add one (e.g. WASAPI for Windows) and a branch in AudioOutputFactory.CreateDefault.");
}
