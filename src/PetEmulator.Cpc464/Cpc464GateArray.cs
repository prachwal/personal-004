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

    public void RenderFrame()
    {
        Width = 320;
        Height = 200;
        Pixels = new byte[Width * Height];
        var start = _crtc.DisplayStartAddress;
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var ma = (ushort)((start + (y / 8) * 40 + x / 8) & 0x3FFF);
            var address = (ushort)((((ma & 0x3000) << 2) + ((ma & 0x03FF) << 1) + ((y & 7) << 11) + (x / 8 & 1)) & 0xFFFF);
            var value = _readRam(address);
            Pixels[y * Width + x] = (byte)(((value >> (7 - (x & 7))) & 1) | (((value >> (3 - (x & 7))) & 1) << 1));
        }
    }
}
