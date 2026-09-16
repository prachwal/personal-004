using PetEmulator.Audio;

namespace PetEmulator.Chips;

/// <summary>
/// General Instrument AY-3-8910/8912 PSG - three tone channels, one noise generator, one envelope
/// generator, 16 registers. The 8910/8912/8913 differ only in I/O port count; the sound section
/// modelled here is identical across the family.
///
/// Register file and mixer logic ported from personal-003's <c>Retro.Devices.Chips.Ay38910Chip</c>
/// (itself ported from proc-vibe-001, 12+ regression tests there), scouted and recommended over
/// personal-002's and proc-vibe-001's own copies for being self-contained (no external interface
/// dependencies) and already wired into a production CPC464 machine at the same 1 MHz clock this
/// one runs at.
///
/// <see cref="Render"/>'s architecture is NOT ported from any donor - none of the three had a
/// working audio path (personal-003's own <c>Render</c>/<c>Sample</c> mixed two inconsistent gating
/// paths: tone-enabled channels bypassed the mixer's noise gating entirely). Instead this follows
/// <see cref="MOS6560"/>, this repo's only other <see cref="IAudioSource"/>: independent phase
/// accumulators advanced by wall-clock <c>dt</c> per rendered sample, decoupled from any CPU-tick
/// cadence. Nothing else in a CPC464 reads this chip's tone/noise/envelope phase - only real audio
/// output does - so there is no correctness reason to keep it in lock-step with the Z80/Gate Array
/// loop, and every reason not to: the two donors that DID tick this from the emulation loop (proc-
/// vibe-001, personal-002) expected Tick() called at Clock/8 Hz, but this machine's Gate Array
/// wiring already calls the equivalent hook at the full 1 MHz Gate Array rate - an 8x-too-fast bug
/// that a phase-accumulator design sidesteps rather than needing to fix at every call site.
/// </summary>
public sealed class Ay38910 : IAudioSource
{
    public byte[] Registers { get; } = new byte[16];
    public byte Selected { get; private set; }

    /// <summary>PSG clock in Hz - the Amstrad CPC runs this at 1 MHz (vs. 1.7734 MHz on a ZX
    /// Spectrum 128 or MSX); only used to convert periods to real frequencies for <see cref="Render"/>.</summary>
    public double Clock { get; set; } = 1_000_000;

    public double SampleRate { get; set; } = 44_100;

    public AudioFormat Format => new((uint)Math.Round(SampleRate), 2);

    private readonly double[] _tonePhase = new double[3];
    private double _noisePhase;
    private uint _noiseShift = 1; // never zero: an all-zero 17-bit LFSR would lock up
    private double _envPhase;
    private int _envStep;
    private bool _envRising;
    private bool _envDone;

    /// <summary>Normalized AY-3-8910 DAC curve from Matthew Westcott's 2001 hardware measurements
    /// (the same table personal-003 carries - a real published measurement, not this repo's own
    /// guess, worth keeping regardless of the donor's own Render bug).</summary>
    private static readonly float[] VolumeTable =
    [
        0.00000f, 0.01375f, 0.02046f, 0.02905f, 0.04234f, 0.06184f, 0.08472f, 0.13690f,
        0.16913f, 0.26467f, 0.35271f, 0.44994f, 0.57038f, 0.68728f, 0.84817f, 1.00000f,
    ];

    public void Reset()
    {
        Array.Clear(Registers);
        Selected = 0;
        Array.Clear(_tonePhase);
        _noisePhase = 0;
        _noiseShift = 1;
        _envPhase = 0; _envStep = 0; _envRising = false; _envDone = false;
    }

    public byte ReadSelected() => Registers[Selected];

    public void WritePort(byte port, byte value)
    {
        if ((port & 1) == 0) { Selected = (byte)(value & 0x0F); return; }
        Registers[Selected] = (byte)(value & Mask(Selected));
        // any write to R13 restarts the envelope, even writing the value already there
        if (Selected == 13) RestartEnvelope();
    }

    private static byte Mask(int register) => register switch
    {
        1 or 3 or 5 or 13 => 0x0F,
        6 or 8 or 9 or 10 => 0x1F,
        _ => 0xFF,
    };

    public int TonePeriod(int channel)
    {
        var p = Registers[channel * 2] | (Registers[channel * 2 + 1] << 8);
        return p == 0 ? 1 : p; // hardware does not stall on a zero period
    }

    public int NoisePeriod => (Registers[6] & 0x1F) == 0 ? 1 : Registers[6] & 0x1F;
    public int EnvelopePeriod => (Registers[11] | (Registers[12] << 8)) == 0
        ? 1 : Registers[11] | (Registers[12] << 8);

    public double ToneHz(int channel) => Clock / (16.0 * TonePeriod(channel));
    public double NoiseHz => Clock / (16.0 * NoisePeriod);
    public double EnvelopeHz => Clock / (256.0 * EnvelopePeriod);

    public int EnvelopeLevel => _envStep;
    public uint NoiseShift => _noiseShift;

    public Ay38910Snapshot CaptureState() => new()
    {
        Registers = Registers.ToArray(), Selected = Selected, TonePhase = _tonePhase.ToArray(), NoisePhase = _noisePhase,
        NoiseShift = _noiseShift, EnvPhase = _envPhase, EnvStep = _envStep, EnvRising = _envRising, EnvDone = _envDone
    };

    public void RestoreState(Ay38910Snapshot state)
    {
        ArgumentNullException.ThrowIfNull(state); state.Registers.AsSpan().CopyTo(Registers); Selected = state.Selected;
        state.TonePhase.AsSpan().CopyTo(_tonePhase); _noisePhase = state.NoisePhase; _noiseShift = state.NoiseShift;
        _envPhase = state.EnvPhase; _envStep = state.EnvStep; _envRising = state.EnvRising; _envDone = state.EnvDone;
    }

    private bool Hold => (Registers[13] & 1) != 0;
    private bool Alternate => (Registers[13] & 2) != 0;
    private bool Attack => (Registers[13] & 4) != 0;
    private bool Continue => (Registers[13] & 8) != 0;

    private void RestartEnvelope()
    {
        _envRising = Attack;
        _envStep = Attack ? 0 : 15;
        _envDone = false;
        _envPhase = 0;
    }

    private void AdvanceEnvelope(double dt)
    {
        if (_envDone) return;
        _envPhase += EnvelopeHz * dt;
        while (_envPhase >= 1.0)
        {
            _envPhase -= 1.0;
            if (_envRising && _envStep < 15) { _envStep++; continue; }
            if (!_envRising && _envStep > 0) { _envStep--; continue; }

            // one ramp finished
            if (!Continue) { _envDone = true; _envStep = 0; return; }
            if (Hold) { _envDone = true; _envStep = Attack ^ Alternate ? 15 : 0; return; }
            if (Alternate) _envRising = !_envRising;
            else _envStep = _envRising ? 0 : 15;
        }
    }

    /// <summary>Renders <paramref name="destination"/> from the chip's current register state.
    /// The emulated machine only ever writes registers through <see cref="WritePort"/>; this is
    /// the sole place tone/noise/envelope phase advances, driven by wall-clock <see cref="SampleRate"/>
    /// time rather than emulated CPU cycles - see this class's own doc comment for why.</summary>
    public int Render(Span<AudioFrame> destination)
    {
        var dt = 1.0 / SampleRate;
        var mixer = Registers[7];

        for (var i = 0; i < destination.Length; i++)
        {
            AdvanceEnvelope(dt);

            var previousNoisePhase = _noisePhase;
            _noisePhase += NoiseHz * dt;
            if (_noisePhase < previousNoisePhase || _noisePhase >= 1.0)
            {
                _noisePhase -= Math.Floor(_noisePhase);
                // 17-bit LFSR, taps on bits 0 and 3 (x^17 + x^14 + 1), full period 131071
                var feedback = (_noiseShift ^ (_noiseShift >> 3)) & 1;
                _noiseShift = (_noiseShift >> 1) | (feedback << 16);
            }
            var noiseBit = (_noiseShift & 1) != 0;

            var sample = 0f;
            for (var channel = 0; channel < 3; channel++)
            {
                _tonePhase[channel] += ToneHz(channel) * dt;
                _tonePhase[channel] -= Math.Floor(_tonePhase[channel]);
                var toneBit = _tonePhase[channel] < 0.5;

                // R7 bits are inverted - a set bit DISABLES that branch - and the two branches
                // (tone-gate, noise-gate) are ANDed: disabling both parks the channel at full
                // amplitude as a DC level rather than silencing it (how PCM samples play on real
                // hardware), which is why each branch is an OR against the disable bit, not a
                // plain AND of the raw generator outputs.
                var toneGate = ((mixer >> channel) & 1) != 0 || toneBit;
                var noiseGate = ((mixer >> (3 + channel)) & 1) != 0 || noiseBit;
                if (!(toneGate && noiseGate)) continue;

                // The gate is a binary pass/mute decision, not a polarity inverter: real hardware
                // sources current only while the ANDed tone/noise signal is high, so a tone-only
                // channel is a unipolar pulse train (0 or +amplitude), not a bipolar square wave -
                // toneBit already made it here via toneGate, so the level itself doesn't flip sign.
                var amp = Registers[8 + channel];
                var level = (amp & 0x10) != 0 ? _envStep : amp & 0x0F;
                sample += VolumeTable[level];
            }

            // ponytail: unipolar per-channel sum carries a DC bias (a full-volume, always-gated
            // channel averages to +amplitude, not 0) - real hardware removes this with an output
            // coupling capacitor; add a high-pass/DC-block stage here if a host backend or mixed-
            // machine-audio path ever turns out to mind a few hundred mV of offset.
            sample /= 3f;
            destination[i] = new AudioFrame(sample, sample);
        }

        return destination.Length;
    }
}
