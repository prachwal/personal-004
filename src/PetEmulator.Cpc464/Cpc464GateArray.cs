using PetEmulator.Chips;

namespace PetEmulator.Cpc464;

public enum CpcDisplayMode : byte { Mode0, Mode1, Mode2 }

public sealed class Cpc464GateArray
{
    private readonly MT6545 _crtc;
    private readonly Func<ushort, byte> _readRam;
    private readonly byte[] _inks = new byte[17];
    private byte _selectedPen;
    private CpcDisplayMode _requestedMode;
    private bool _previousHSync;
    private int _hsyncCount;

    public Cpc464GateArray(MT6545 crtc, Func<ushort, byte> readRam)
    {
        _crtc = crtc;
        _readRam = readRam;
        Reset();
    }

    public CpcDisplayMode Mode { get; private set; }
    public bool LowerRomEnabled { get; private set; }
    public bool UpperRomEnabled { get; private set; }
    public byte RamConfiguration { get; private set; }
    public bool InterruptPending { get; private set; }
    public byte[] Pixels { get; private set; } = [];
    public int Width { get; private set; }
    public int Height { get; private set; }
    public byte GetInk(byte index) => _inks[index & 0x0F];

    public void Reset()
    {
        Array.Clear(_inks);
        _selectedPen = 0;
        _requestedMode = Mode = CpcDisplayMode.Mode1;
        LowerRomEnabled = UpperRomEnabled = true;
        RamConfiguration = 0;
        InterruptPending = false;
        _previousHSync = false;
        _hsyncCount = 0;
        Pixels = [];
        Width = Height = 0;
    }

    public void WritePort(ushort port, byte value)
    {
        if ((port & 0xFF00) != 0x7F00) return;
        switch (value >> 6)
        {
            case 0: _selectedPen = (byte)(value & 0x1F); break;
            case 1: _inks[(_selectedPen & 0x10) == 0x10 ? 16 : _selectedPen & 0x0F] = (byte)(value & 0x1F); break;
            case 2:
                _requestedMode = (CpcDisplayMode)Math.Min(value & 3, 2);
                LowerRomEnabled = (value & 4) == 0;
                UpperRomEnabled = (value & 8) == 0;
                if ((value & 0x10) != 0) _hsyncCount = 0;
                break;
            case 3:
                RamConfiguration = (byte)(value & 0x07);
                break;
        }
    }

    public void Tick()
    {
        _crtc.Tick();
        if (_crtc.HSync && !_previousHSync)
        {
            Mode = _requestedMode;
            if (++_hsyncCount == 52) { _hsyncCount = 0; InterruptPending = true; }
        }
        _previousHSync = _crtc.HSync;
    }

    public void AcknowledgeInterrupt() => InterruptPending = false;

    /// <summary>The Amstrad monitor's physical resolution regardless of mode: Mode 0/2's
    /// narrower/wider logical pixels are resampled onto this fixed canvas rather than changing the
    /// framebuffer size the machine/desktop layer already assume.</summary>
    private const int Columns = 40;
    private const int ScanLines = 8;

    public void RenderFrame()
    {
        Width = 320;
        Height = 200;
        if (Pixels.Length != Width * Height) Pixels = new byte[Width * Height];
        var pixelsPerByte = PixelsPerByte(Mode);
        var nativeWidth = Columns * 2 * pixelsPerByte;
        var native = new byte[nativeWidth];
        var start = _crtc.DisplayStartAddress;
        for (var y = 0; y < Height; y++)
        {
            var rowStart = (ushort)((start + (y / ScanLines) * Columns) & 0x3FFF);
            var raster = y & 7;
            for (var column = 0; column < Columns; column++)
            {
                var ma = (ushort)((rowStart + column) & 0x3FFF);
                var target = column * 2 * pixelsPerByte;
                DecodeByte(_readRam(VideoAddress(ma, raster, 0)), pixelsPerByte, native.AsSpan(target, pixelsPerByte));
                DecodeByte(_readRam(VideoAddress(ma, raster, 1)), pixelsPerByte, native.AsSpan(target + pixelsPerByte, pixelsPerByte));
            }
            for (var x = 0; x < Width; x++)
                Pixels[y * Width + x] = native[x * nativeWidth / Width];
        }
    }

    /// <summary>A CRTC MA address covers a pair of video-RAM bytes; RA selects one of eight
    /// interleaved 2 KB scanline blocks (the CPC's native display layout).</summary>
    private static ushort VideoAddress(ushort ma, int raster, int byteInPair) =>
        (ushort)((((ma & 0x3000) << 2) + ((ma & 0x03FF) << 1) + ((raster & 7) << 11) + byteInPair) & 0xFFFF);

    private static int PixelsPerByte(CpcDisplayMode mode) => mode switch
    {
        CpcDisplayMode.Mode0 => 2,
        CpcDisplayMode.Mode1 => 4,
        _ => 8,
    };

    /// <summary>Un-interleaves one video byte into its mode-dependent ink indices (Mode 0: 4 bits/
    /// pixel from bits 7,3,5,1 then 6,2,4,0; Mode 1: 2 bits/pixel from bits 7,3 then 6,2 ...;
    /// Mode 2: 1 bit/pixel, MSB first) - the CPC's documented per-mode bit order.</summary>
    private static void DecodeByte(byte value, int pixelsPerByte, Span<byte> destination)
    {
        for (var pixel = 0; pixel < pixelsPerByte; pixel++)
        {
            destination[pixel] = pixelsPerByte switch
            {
                2 => (byte)(((value >> (7 - pixel)) & 1) | (((value >> (3 - pixel)) & 1) << 1) |
                             (((value >> (5 - pixel)) & 1) << 2) | (((value >> (1 - pixel)) & 1) << 3)),
                4 => (byte)(((value >> (7 - pixel)) & 1) | (((value >> (3 - pixel)) & 1) << 1)),
                _ => (byte)((value >> (7 - pixel)) & 1),
            };
        }
    }

    /// <summary>The Gate Array's 32 fixed hardware colours (5-bit pen code to RGB), packed as
    /// 0xAARRGGBB. Ports written by <c>OUT</c> select among these; software never defines new
    /// colours. Source: CPC hardware colour table (also used by the personal-002 reference port).</summary>
    public static readonly uint[] HardwareColors =
    [
        0xFF6C7D6E, 0xFF6D7D6E, 0xFF6CF300, 0xFF6DF3F3,
        0xFF6C0200, 0xFF6802F0, 0xFF687800, 0xFF6C7DF3,
        0xFF6802F3, 0xFF6CF3F3, 0xFF0DF3F3, 0xFFF9F3FF,
        0xFF0605F3, 0xFFF402F3, 0xFF0D7DF3, 0xFFF980FA,
        0xFF680200, 0xFF6CF302, 0xFF01F002, 0xFFF2F30F,
        0xFF010200, 0xFFF4020C, 0xFF017802, 0xFFF47B0C,
        0xFF680269, 0xFF6CF371, 0xFF04F571, 0xFFF4F371,
        0xFF01026C, 0xFFF2026C, 0xFF017B6E, 0xFFF67B6E,
    ];

    public byte GetInkColorIndex(byte pixelValue) => (byte)(GetInk(pixelValue) & 0x1F);
}
