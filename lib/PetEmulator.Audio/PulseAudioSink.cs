using System.Runtime.InteropServices;

namespace PetEmulator.Audio;

/// <summary>
/// Streams an <see cref="IAudioSource"/> to the host speakers via PulseAudio's simple API
/// (<c>libpulse-simple</c>) on a dedicated background thread.
///
/// Chosen over SDL3's audio API for this repo's WSLg-only host, per prior-art gathered from
/// personal-002/personal-003/proc-vibe-001 (see docs/audio-wsl-backend.md for the full writeup):
/// SDL3's default driver probe silently picks pipewire on WSL (the distro ships a pipewire
/// *client* lib with no pipewire server actually running), so a naive <c>SDL_Init</c> opens a
/// device that plays into nothing - personal-002's Sdl3AudioSink only works around this with a
/// native (not managed - .NET's own SetEnvironmentVariable does not reach SDL3's native getenv)
/// <c>setenv("SDL_AUDIO_DRIVER", "pulseaudio")</c> forced before init. Talking to
/// <c>libpulse-simple</c> directly skips that landmine entirely: WSLg's own audio server *is*
/// PulseAudio (<c>pactl list short sinks</c> reports a <c>RDPSink</c>/<c>RDPSink.monitor</c>
/// pair), confirmed working end-to-end by personal-003's MSX AY audio investigation.
///
/// Buffer sizing (~1s target length/prebuffer, ~0.25s minimum request) is not arbitrary - a
/// thinner buffer produced audible crackle over the WSLg/RDP transport even though the
/// PulseAudio-side stream itself was clean (that investigation isolated this by capturing
/// <c>RDPSink.monitor</c> - after PulseAudio, before the Windows endpoint - independently of what
/// was actually heard). Reused unchanged from that fix.
/// </summary>
public sealed class PulseAudioSink : IDisposable
{
    private Thread? _thread;
    private volatile bool _stop;

    public bool IsPlaying { get; private set; }

    /// <summary>Set when playback couldn't start or failed mid-stream - never thrown, so a caller
    /// without a real PulseAudio server (CI, a non-WSL/non-Linux dev box) keeps running with audio
    /// silently disabled instead of crashing.</summary>
    public string? LastError { get; private set; }

    public void Start(IAudioSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Stop();
        _stop = false;
        LastError = null;
        _thread = new Thread(() => Run(source)) { IsBackground = true, Name = "PulseAudioSink" };
        _thread.Start();
    }

    public void Stop()
    {
        _stop = true;
        _thread?.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        IsPlaying = false;
    }

    public void Dispose() => Stop();

    private void Run(IAudioSource source)
    {
        var format = source.Format;
        var stream = IntPtr.Zero;
        try
        {
            var spec = new Pulse.SampleSpec(Pulse.Float32Le, format.SampleRate, (byte)format.Channels);
            var targetBytes = (uint)(format.SampleRate * format.Channels * sizeof(float));
            var buffer = new Pulse.BufferAttributes(
                maxlength: uint.MaxValue,
                tlength: targetBytes,
                prebuf: targetBytes,
                minreq: targetBytes / 4,
                fragsize: uint.MaxValue);
            var error = 0;
            stream = Pulse.New(
                Environment.GetEnvironmentVariable("PULSE_SERVER"),
                "PetEmulator", Pulse.Playback, IntPtr.Zero, "PetEmulator.Audio", ref spec,
                IntPtr.Zero, ref buffer, ref error);
            if (stream == IntPtr.Zero)
            {
                Fail($"PulseAudio initialization failed ({error})");
                return;
            }

            var frames = new AudioFrame[4096];
            var bytes = new byte[frames.Length * format.Channels * sizeof(float)];
            IsPlaying = true;
            while (!_stop)
            {
                var produced = source.Render(frames);
                if (produced == 0)
                    break;

                var byteLength = Interleave(frames.AsSpan(0, produced), format.Channels, bytes);
                if (Pulse.Write(stream, bytes, (uint)byteLength, ref error) != 0)
                {
                    Fail($"PulseAudio write failed ({error})");
                    return;
                }
            }

            if (!_stop)
                Pulse.Drain(stream, ref error);
        }
        catch (DllNotFoundException ex) { Fail($"PulseAudio backend unavailable: {ex.Message}"); }
        catch (EntryPointNotFoundException ex) { Fail($"PulseAudio backend unavailable: {ex.Message}"); }
        finally
        {
            if (stream != IntPtr.Zero)
                Pulse.Free(stream);
            IsPlaying = false;
        }
    }

    private void Fail(string message)
    {
        LastError = message;
        Console.Error.WriteLine($"Audio disabled: {message}. Emulation continues.");
    }

    /// <summary>Packs frames into a host byte buffer, least-significant-byte-first float32 per
    /// channel - a mono format (<paramref name="channels"/> == 1) writes only <see cref="AudioFrame.Left"/>
    /// per frame. Pure and allocation-free (writes into the caller's buffer) so it's testable
    /// without a real PulseAudio server.</summary>
    internal static int Interleave(ReadOnlySpan<AudioFrame> frames, int channels, byte[] destination)
    {
        var offset = 0;
        foreach (var frame in frames)
        {
            MemoryMarshal.Write(destination.AsSpan(offset), frame.Left);
            offset += sizeof(float);
            if (channels > 1)
            {
                MemoryMarshal.Write(destination.AsSpan(offset), frame.Right);
                offset += sizeof(float);
            }
        }

        return offset;
    }

    private static class Pulse
    {
        internal const uint Playback = 1;
        internal const uint Float32Le = 5;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SampleSpec(uint format, uint rate, byte channels)
        {
            internal uint Format = format;
            internal uint Rate = rate;
            internal byte Channels = channels;
            private readonly byte _padding1 = 0;
            private readonly byte _padding2 = 0;
            private readonly byte _padding3 = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BufferAttributes(uint maxlength, uint tlength, uint prebuf, uint minreq, uint fragsize)
        {
            internal uint MaxLength = maxlength;
            internal uint TargetLength = tlength;
            internal uint PreBuffer = prebuf;
            internal uint MinimumRequest = minreq;
            internal uint FragmentSize = fragsize;
        }

        [DllImport("libpulse-simple.so.0", EntryPoint = "pa_simple_new", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern IntPtr New(
            string? server,
            string name,
            uint direction, IntPtr device,
            string streamName,
            ref SampleSpec spec, IntPtr channelMap, ref BufferAttributes bufferAttributes, ref int error);

        [DllImport("libpulse-simple.so.0", EntryPoint = "pa_simple_write", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Write(IntPtr connection, byte[] data, uint bytes, ref int error);

        [DllImport("libpulse-simple.so.0", EntryPoint = "pa_simple_drain", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Drain(IntPtr connection, ref int error);

        [DllImport("libpulse-simple.so.0", EntryPoint = "pa_simple_free", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Free(IntPtr connection);
    }
}
