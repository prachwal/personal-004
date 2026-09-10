using PetEmulator.Core;

namespace PetEmulator.Vic20.Chips;

/// <summary>
/// Minimal MOS 6560/6561 VIC (Video Interface Chip) - the VIC-20's video/audio chip, 16
/// memory-mapped registers. Ported (rewritten, not copied - different bus/device contracts) from
/// a reference implementation (see docs/vic20-migration-plan.md step 2), NTSC-only for v1: no
/// audio oscillators (nothing in "boot to BASIC READY + render text" needs them - a documented
/// gap, not a guess), no PAL timing (<see cref="TotalScanlines"/>/<see cref="CyclesPerLine"/> are
/// NTSC constants only).
/// </summary>
public sealed class Vic6560 : IMemoryMappedDevice
{
    public const int RegisterCount = 16;

    // NTSC video timing (real VIC-I hardware constants, not implementation-specific).
    public const double Phi2Ntsc = 1_431_8181.0 / 14.0;
    public const int TotalScanlines = 261;
    public const int CyclesPerLine = 65;

    private readonly ushort _baseAddress;
    private readonly byte[] _registers = new byte[RegisterCount];
    private int _rasterCounter;
    private long _lineCycles;

    public Vic6560(string name = "VIC", ushort baseAddress = 0)
    {
        Name = name;
        _baseAddress = baseAddress;
    }

    public string Name { get; }

    public uint Length => RegisterCount;

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
    /// final boot-time value - see docs/vic20-migration-plan.md's color-RAM bug note), real
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
    // on blue instead of the real blue-on-white) - see docs/vic20-rendering-fixes.md.
    public bool ReverseMode => (_registers[0x0F] & 0x08) == 0;
    public byte BorderColor => (byte)(_registers[0x0F] & 0x07);

    public byte Read(ushort address)
    {
        var offset = (address - _baseAddress) & 0x0F;
        return offset switch
        {
            0x04 => (byte)(_rasterCounter & 0xFF),
            0x03 => (byte)((_registers[0x03] & 0x7F) | ((_rasterCounter >> 1) & 0x80)),
            _ => _registers[offset],
        };
    }

    public void Write(ushort address, byte value)
    {
        var offset = (address - _baseAddress) & 0x0F;
        _registers[offset] = value;
    }

    public void Reset()
    {
        Array.Clear(_registers);
        _rasterCounter = 0;
        _lineCycles = 0;
    }

    /// <summary>Advances the raster line counter by real elapsed CPU cycles (not once-per-call
    /// like the reference chip's frame-stepped <c>Update()</c> - matches this repo's
    /// <see cref="IDevice.Tick"/> contract, called once per <see cref="PetEmulator.Pet.PetMachine.StepInstruction"/>-equivalent
    /// with the exact cycle count that instruction took).</summary>
    public void Tick(ulong cycles)
    {
        _lineCycles += (long)cycles;
        while (_lineCycles >= CyclesPerLine)
        {
            _lineCycles -= CyclesPerLine;
            _rasterCounter++;
            if (_rasterCounter >= TotalScanlines)
                _rasterCounter = 0;
        }
    }
}
