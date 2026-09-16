using PetEmulator.Audio;
using PetEmulator.Core;

namespace PetEmulator.Chips;

/// <summary>
/// Minimal MOS 6560/6561 VIC (Video Interface Chip) - the VIC-20's video/audio chip, 16
/// memory-mapped registers. Ported (rewritten, not copied - different bus/device contracts) from
/// a reference implementation (see docs/vic20/migration-plan.md step 2). The model supports the
/// NTSC 6560 and PAL 6561 timing profiles; both variants share the register and audio behavior.
/// </summary>
public sealed class MOS6560 : IMemoryMappedDevice, IAudioSource
{
    public const int RegisterCount = 16;

    // NTSC video timing (real VIC-I hardware constants, not implementation-specific).
    public const double Phi2Ntsc = 1_431_8181.0 / 14.0;
    public const int TotalScanlines = 261;
    public const int CyclesPerLine = 65;
    public const double Phi2Pal = 17_734_472.0 / 16.0;
    public const int PalTotalScanlines = 312;
    public const int PalCyclesPerLine = 71;

    private readonly ushort _baseAddress;
    private readonly MOS6560Standard _standard;
    private readonly byte[] _registers = new byte[RegisterCount];
    private int _rasterCounter;
    private long _lineCycles;
    private readonly double[] _audioPhases = new double[4];
    private ushort _noiseLfsr = 0xFFFF;

    public MOS6560(string name = "VIC", ushort baseAddress = 0, MOS6560Standard standard = MOS6560Standard.Ntsc)
    {
        Name = name;
        _baseAddress = baseAddress;
        _standard = standard;
    }

    public string Name { get; }

    public MOS6560Standard Standard => _standard;

    public double Phi2 => _standard == MOS6560Standard.Pal ? Phi2Pal : Phi2Ntsc;

    public int TimingTotalScanlines => _standard == MOS6560Standard.Pal ? PalTotalScanlines : TotalScanlines;

    public int TimingCyclesPerLine => _standard == MOS6560Standard.Pal ? PalCyclesPerLine : CyclesPerLine;

    public uint Length => RegisterCount;

    public double SampleRate { get; set; } = 44_100;

    public AudioFormat Format => new((uint)Math.Round(SampleRate), 2);

    public bool Oscillator1Enabled => (_registers[0x0A] & 0x80) != 0;
    public bool Oscillator2Enabled => (_registers[0x0B] & 0x80) != 0;
    public bool Oscillator3Enabled => (_registers[0x0C] & 0x80) != 0;
    public bool NoiseEnabled => (_registers[0x0D] & 0x80) != 0;

    public double Oscillator1Frequency => CalculateFrequency(_registers[0x0A], 256);
    public double Oscillator2Frequency => CalculateFrequency(_registers[0x0B], 128);
    public double Oscillator3Frequency => CalculateFrequency(_registers[0x0C], 64);
    public double NoiseFrequency => CalculateFrequency(_registers[0x0D], 32);

    public byte Volume => (byte)(_registers[0x0E] & 0x0F);

    /// <summary>Latched light-pen X coordinate supplied by the external control port.</summary>
    public byte LightPenX { get; private set; }

    /// <summary>Latched light-pen Y coordinate supplied by the external control port.</summary>
    public byte LightPenY { get; private set; }

    /// <summary>Digitized paddle X value supplied by the external control port.</summary>
    public byte PaddleX { get; private set; }

    /// <summary>Digitized paddle Y value supplied by the external control port.</summary>
    public byte PaddleY { get; private set; }

    /// <summary>Supplies the two VIC-20 paddle positions read through $9008/$9009.</summary>
    public void SetPaddlePosition(byte x, byte y)
    {
        PaddleX = x;
        PaddleY = y;
    }

    /// <summary>Latches a light-pen position as if the external light-pen strobe occurred.</summary>
    public void StrobeLightPen(byte x, byte y)
    {
        LightPenX = x;
        LightPenY = y;
    }

    /// <summary>14-bit VIC-internal address translated to a CPU-bus address: A15 = NOT A13
    /// (real VIC-I address-line inversion quirk - matches how the chip's own screen/char-matrix
    /// base registers must be interpreted to find real RAM/ROM content).</summary>
    public static ushort ToCpuAddress(int vicAddress)
    {
        var low = vicAddress & 0x1FFF;
        if ((vicAddress & 0x2000) == 0)
            low |= 0x8000;
        return (ushort)low;
    }

    // Backed by _rasterCounter (kept current every Tick()), not _registers[0x03]/[0x04] directly -
    // those only get the live value on a real bus Read() (see Read()'s offset 0x03/0x04 cases).
    // A caller reading Raster straight off the chip (tests, a future renderer) needs the same
    // live value without going through a bus access.
    public int Raster => _rasterCounter;
    public int Columns => _registers[0x02] & 0x7F;
    public int Rows => (_registers[0x03] >> 1) & 0x3F;
    public bool DoubleHeightChars => (_registers[0x03] & 0x01) != 0;
    public int ScreenMatrixBase => ((_registers[0x05] & 0xF0) << 6) | ((_registers[0x02] & 0x80) << 2);
    public int CharMatrixBase => (_registers[0x05] & 0x0F) << 10;
    public int ScreenAddr => ToCpuAddress(ScreenMatrixBase);
    public int CharAddr => ToCpuAddress(CharMatrixBase);

    /// <summary>Offset into the fixed $9400-$97FF color-RAM window for this screen's cell 0 -
    /// real hardware fetches color RAM using the SAME low 10 bits of the video matrix address as
    /// the screen fetch (color RAM only has 10 useful address lines; the video matrix's high bits
    /// select which 512-byte-aligned page it lives in, but its low 10 bits still land somewhere
    /// within color RAM's own window and must be added to <c>ColorRamStart</c> - reading straight
    /// from offset 0 is wrong whenever the low 10 bits aren't already 0). Confirmed empirically
    /// against the real KERNAL: with <see cref="ScreenMatrixBase"/>=$3E00 (this repo's real,
    /// final boot-time value - see docs/vic20/migration-plan.md's color-RAM bug note), real
    /// KERNAL writes to color RAM land at $9600-$97F9 (offset $200), exactly
    /// <c>ScreenMatrixBase &amp; 0x3FF</c> = $3E00 &amp; 0x3FF = $200.</summary>
    public int ColorMatrixOffset => ScreenMatrixBase & 0x3FF;
    public byte AuxColor => (byte)((_registers[0x0E] >> 4) & 0x0F);
    public byte ScreenColor => (byte)((_registers[0x0F] >> 4) & 0x0F);
    // Bit 3's default power-on/KERNAL-init value is 1, and 1 means "colors in their normal
    // places" (ink = color RAM, paper = screen color, no swap) - 0 is what actually reverses the
    // field. Confirmed empirically: the real KERNAL boot writes $1B to $900F (bit 3 set), and a
    // real VIC-20's boot screen is blue text on a white background - which is only what this
    // repo's RenderChar produces if "bit set" means false (no swap). Was inverted (`!= 0` read as
    // "reverse"), which silently swapped ink/paper for the entire default boot screen (white text
    // on blue instead of the real blue-on-white) - see docs/vic20/rendering-fixes.md.
    public bool ReverseMode => (_registers[0x0F] & 0x08) == 0;
    public byte BorderColor => (byte)(_registers[0x0F] & 0x07);

    public byte Read(ushort address)
    {
        var offset = (address - _baseAddress) & 0x0F;
        return offset switch
        {
            0x04 => (byte)(_rasterCounter & 0xFF),
            0x03 => (byte)((_registers[0x03] & 0x7F) | ((_rasterCounter >> 1) & 0x80)),
            0x06 => LightPenX,
            0x07 => LightPenY,
            0x08 => PaddleX,
            0x09 => PaddleY,
            _ => _registers[offset],
        };
    }

    public void Write(ushort address, byte value)
    {
        var offset = (address - _baseAddress) & 0x0F;
        if (offset is 0x06 or 0x07 or 0x08 or 0x09)
            return; // light-pen and paddle registers are external-input readbacks

        _registers[offset] = value;
    }

    public void Reset()
    {
        Array.Clear(_registers);
        _rasterCounter = 0;
        _lineCycles = 0;
        Array.Clear(_audioPhases);
        _noiseLfsr = 0xFFFF;
        LightPenX = LightPenY = PaddleX = PaddleY = 0;
    }

    public MOS6560Snapshot CaptureState() => new()
    {
        Standard = _standard, SampleRate = SampleRate,
        Registers = _registers.ToArray(), RasterCounter = _rasterCounter, LineCycles = _lineCycles,
        AudioPhases = _audioPhases.ToArray(), NoiseLfsr = _noiseLfsr,
        LightPenX = LightPenX, LightPenY = LightPenY, PaddleX = PaddleX, PaddleY = PaddleY
    };

    public void RestoreState(MOS6560Snapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Standard != _standard)
            throw new InvalidDataException($"MOS6560 standard mismatch: snapshot is {state.Standard}, machine is {_standard}.");
        if (state.Registers.Length != RegisterCount || state.AudioPhases.Length != 4)
            throw new ArgumentException("Invalid MOS6560 snapshot dimensions.", nameof(state));
        state.Registers.CopyTo(_registers, 0);
        state.AudioPhases.CopyTo(_audioPhases, 0);
        _rasterCounter = state.RasterCounter; _lineCycles = state.LineCycles; _noiseLfsr = state.NoiseLfsr;
        SampleRate = state.SampleRate;
        LightPenX = state.LightPenX; LightPenY = state.LightPenY;
        PaddleX = state.PaddleX; PaddleY = state.PaddleY;
    }

    public int Render(Span<AudioFrame> destination)
    {
        var dt = 1.0 / SampleRate;
        var volume = Volume / 15.0;

        for (var i = 0; i < destination.Length; i++)
        {
            var sample = 0.0;
            var generators = 0;
            sample += RenderSquare(Oscillator1Enabled, Oscillator1Frequency, 0, dt, ref generators);
            sample += RenderSquare(Oscillator2Enabled, Oscillator2Frequency, 1, dt, ref generators);
            sample += RenderSquare(Oscillator3Enabled, Oscillator3Frequency, 2, dt, ref generators);
            sample += RenderNoise(dt, ref generators);
            var value = generators == 0 ? 0f : (float)(sample / generators * volume);
            destination[i] = new AudioFrame(value, value);
        }

        return destination.Length;
    }

    private double CalculateFrequency(byte register, int divider) =>
        // VIC frequency registers use the complete 8-bit value while enabled.
        // Bit 7 is the enable bit, but it also participates in the divider; masking
        // it out turns values such as $F0 into a ~28 Hz rumble instead of ~266 Hz.
        Phi2 / divider / (255 - register + 1);

    private double RenderSquare(bool enabled, double frequency, int phaseIndex, double dt, ref int generators)
    {
        if (!enabled)
            return 0;

        _audioPhases[phaseIndex] = AdvancePhase(_audioPhases[phaseIndex], frequency, dt);
        generators++;
        return _audioPhases[phaseIndex] < 0.5 ? 1 : -1;
    }

    private double RenderNoise(double dt, ref int generators)
    {
        if (!NoiseEnabled)
            return 0;

        var previous = _audioPhases[3];
        _audioPhases[3] = AdvancePhase(previous, NoiseFrequency, dt);
        if (_audioPhases[3] < previous)
        {
            var feedback = (ushort)(((_noiseLfsr >> 14) ^ (_noiseLfsr >> 13)) & 1);
            _noiseLfsr = (ushort)((_noiseLfsr << 1) | feedback);
            if (_noiseLfsr == 0)
                _noiseLfsr = 0xFFFF;
        }

        generators++;
        return (_noiseLfsr & 1) == 0 ? 1 : -1;
    }

    private static double AdvancePhase(double phase, double frequency, double dt)
    {
        phase += frequency * dt;
        return phase - Math.Floor(phase);
    }

    /// <summary>Advances the raster line counter by real elapsed CPU cycles (not once-per-call
    /// like the reference chip's frame-stepped <c>Update()</c> - matches this repo's
    /// <see cref="IDevice.Tick"/> contract, called once per <see cref="PetEmulator.Pet.PetMachine.StepInstruction"/>-equivalent
    /// with the exact cycle count that instruction took).</summary>
    public void Tick(ulong cycles)
    {
        _lineCycles += (long)cycles;
        while (_lineCycles >= TimingCyclesPerLine)
        {
            _lineCycles -= TimingCyclesPerLine;
            _rasterCounter++;
            if (_rasterCounter >= TimingTotalScanlines)
                _rasterCounter = 0;
        }
    }
}

public enum MOS6560Standard
{
    Ntsc,
    Pal
}
