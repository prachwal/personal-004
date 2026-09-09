using PetEmulator.Core;

namespace PetEmulator.Pet.Chips;

/// <summary>Minimal, renderer-independent Motorola 6545 CRT controller.
/// NOT modeled: the Rockwell/Hitachi 6545-specific "Update"/"Transparent" alternate memory-access
/// modes and their status-bit7 "Update Ready" flag (R8 bits 6:5 select them on some 6545 variants).
/// No PET ROM ever selects them (R8 is always written 0 by the KERNAL), and this class's
/// 18-register model has no update-address register pair to back them - left as a documented gap
/// rather than a guessed implementation. R8's interlace (bits 1:0) and skew (bits 7:4, standard
/// 6845 features) ARE modeled.</summary>
public sealed class Crtc6545 : IMemoryMappedDevice
{
    private const byte StatusVerticalRetrace = 0x20;
    private const byte StatusLightPen = 0x40;
    private static readonly byte[] RegisterMasks =
    [
        0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0x1F, 0x7F, 0x7F, 0xFF,
        0x1F, 0x7F, 0x1F, 0x3F, 0xFF, 0x3F, 0xFF, 0x3F, 0xFF
    ];

    private readonly ushort _baseAddress;
    private readonly byte[] _registers = new byte[18];
    private byte _selectedRegister;
    private ushort _horizontalCounter;
    private ushort _verticalCounter;
    private byte _rasterCounter;
    private byte _verticalAdjustCounter;
    private ushort _lineStartAddress;
    private bool _inVerticalAdjust;
    private bool _lightPenRegistered;
    private bool _cursorBlinkVisible = true;
    private byte _cursorBlinkFrames;
    private bool _interlaceField;
    private readonly bool[] _dePipe = new bool[4];
    private readonly bool[] _cursorPipe = new bool[4];

    public Crtc6545(string name = "CRTC", ushort baseAddress = 0)
    {
        Name = name;
        _baseAddress = baseAddress;
    }

    public string Name { get; }

    /// <summary>Two local bus addresses: register select/status (0) and data (1).</summary>
    public uint Length => 2;

    public byte SelectedRegister => _selectedRegister;

    public ushort MACounter { get; private set; }

    public byte RACounter => _rasterCounter;

    public ushort DisplayStartAddress => (ushort)((_registers[12] << 8) | _registers[13]);

    public ushort CursorAddress => (ushort)((_registers[14] << 8) | _registers[15]);

    public bool HSync { get; private set; }

    public bool VSync { get; private set; }

    public bool DisplayEnable { get; private set; }

    public bool VerticalBlanking { get; private set; }

    public bool CursorEnable { get; private set; }

    public bool LightPenRegistered => _lightPenRegistered;

    /// <summary>
    /// Advances the controller by one character clock. Outputs and MA/RA describe
    /// the character clock being consumed; the counters then move to the next one.
    /// </summary>
    public void Tick()
    {
        UpdateOutputs();
        AdvanceCounters();
    }

    /// <summary>Advances the controller by <paramref name="cycles"/> character clocks.</summary>
    public void Tick(ulong cycles)
    {
        for (ulong i = 0; i < cycles; i++)
            Tick();
    }

    public byte Read(ushort address)
    {
        int offset = address - _baseAddress;
        return offset switch
        {
            0 => ReadStatus(),
            1 => ReadSelectedRegister(),
            _ => throw new ArgumentOutOfRangeException(nameof(address))
        };
    }

    public void Write(ushort address, byte value)
    {
        int offset = address - _baseAddress;
        switch (offset)
        {
            case 0:
                _selectedRegister = (byte)(value & 0x1F);
                break;
            case 1:
                WriteSelectedRegister(value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(address));
        }
    }

    public void Reset()
    {
        Array.Clear(_registers);
        _selectedRegister = 0;
        _horizontalCounter = 0;
        _verticalCounter = 0;
        _rasterCounter = 0;
        _verticalAdjustCounter = 0;
        _lineStartAddress = 0;
        _inVerticalAdjust = false;
        _lightPenRegistered = false;
        _cursorBlinkVisible = true;
        _cursorBlinkFrames = 0;
        _interlaceField = false;
        Array.Clear(_dePipe);
        Array.Clear(_cursorPipe);
        MACounter = 0;
        HSync = false;
        VSync = false;
        DisplayEnable = false;
        VerticalBlanking = false;
        CursorEnable = false;
    }

    /// <summary>Captures the current MA value into the read-only R16/R17 pair.</summary>
    public void LightPenStrobe()
    {
        _registers[16] = (byte)(MACounter >> 8);
        _registers[17] = (byte)MACounter;
        _lightPenRegistered = true;
    }

    private byte ReadStatus()
        => (byte)((VerticalBlanking ? StatusVerticalRetrace : 0) |
                  (_lightPenRegistered ? StatusLightPen : 0));

    private byte ReadSelectedRegister()
    {
        if (_selectedRegister >= _registers.Length)
            return 0xFF;

        var value = _registers[_selectedRegister];
        if (_selectedRegister is 16 or 17)
            _lightPenRegistered = false;

        return value;
    }

    private void WriteSelectedRegister(byte value)
    {
        if (_selectedRegister >= 16)
            return;

        _registers[_selectedRegister] = (byte)(value & RegisterMasks[_selectedRegister]);
        if (_selectedRegister is 12 or 13 && _horizontalCounter == 0 && _verticalCounter == 0 && _rasterCounter == 0)
        {
            _lineStartAddress = DisplayStartAddress;
            MACounter = _lineStartAddress;
        }
    }

    private void UpdateOutputs()
    {
        var horizontalDisplay = _horizontalCounter < _registers[1];
        var verticalDisplay = !_inVerticalAdjust && _verticalCounter < _registers[6];
        var rawDisplayEnable = horizontalDisplay && verticalDisplay;
        VerticalBlanking = _inVerticalAdjust || _verticalCounter >= _registers[6];

        var hSyncWidth = SyncWidth((byte)(_registers[3] & 0x0F));
        var vSyncWidth = SyncWidth((byte)(_registers[3] >> 4));
        HSync = _horizontalCounter >= _registers[2] && _horizontalCounter < _registers[2] + hSyncWidth;
        VSync = !_inVerticalAdjust && _verticalCounter >= _registers[7] && _verticalCounter < _registers[7] + vSyncWidth;

        MACounter = (ushort)((_lineStartAddress + _horizontalCounter) & 0x3FFF);
        var cursorMode = (byte)((_registers[10] >> 5) & 0x03);
        var cursorRaster = _rasterCounter >= (_registers[10] & 0x1F) && _rasterCounter <= _registers[11];
        var rawCursorEnable = rawDisplayEnable && MACounter == CursorAddress && cursorRaster &&
            cursorMode != 1 && (cursorMode == 0 || _cursorBlinkVisible);

        // R8 bits 7:6 = display-enable skew, bits 5:4 = cursor skew (both 0-3 character clocks) -
        // pipeline the raw signals so the public property lags by the configured clock count,
        // matching the 6845/6545's character-generator pipeline-latency compensation.
        ShiftPipe(_dePipe, rawDisplayEnable);
        ShiftPipe(_cursorPipe, rawCursorEnable);
        DisplayEnable = _dePipe[(_registers[8] >> 6) & 0x03];
        CursorEnable = _cursorPipe[(_registers[8] >> 4) & 0x03];
    }

    private static void ShiftPipe(bool[] pipe, bool value)
    {
        for (var i = pipe.Length - 1; i > 0; i--)
            pipe[i] = pipe[i - 1];
        pipe[0] = value;
    }

    /// <summary>R8 bits 1:0 = interlace mode. Only "interlace sync and video" (0b11) is modeled:
    /// the raster counter steps by 2 instead of 1, and each frame alternates which raster line it
    /// starts on (0 or 1), doubling the apparent vertical resolution across two fields. "Interlace
    /// sync only" (0b01) is 6845-datasheet-documented to affect only VSync pulse alignment, not
    /// the raster/MA sequence this class exposes, so it is treated the same as non-interlace (0b00
    /// /0b10) here.</summary>
    private bool IsInterlaceVideoMode => (_registers[8] & 0x03) == 0x03;

    private void AdvanceCounters()
    {
        if (++_horizontalCounter <= _registers[0])
            return;

        _horizontalCounter = 0;
        if (_inVerticalAdjust)
        {
            if (++_verticalAdjustCounter >= _registers[5])
                StartFrame();
            return;
        }

        _rasterCounter += (byte)(IsInterlaceVideoMode ? 2 : 1);
        if (_rasterCounter <= _registers[9])
            return;

        _rasterCounter = 0;
        _lineStartAddress = (ushort)((_lineStartAddress + _registers[1]) & 0x3FFF);
        if (++_verticalCounter <= _registers[4])
            return;

        if (_registers[5] == 0)
            StartFrame();
        else
        {
            _inVerticalAdjust = true;
            _verticalAdjustCounter = 0;
        }
    }

    private void StartFrame()
    {
        _verticalCounter = 0;
        _rasterCounter = 0;
        if (IsInterlaceVideoMode)
        {
            _interlaceField = !_interlaceField;
            _rasterCounter = (byte)(_interlaceField ? 1 : 0);
        }
        _verticalAdjustCounter = 0;
        _inVerticalAdjust = false;
        _lineStartAddress = DisplayStartAddress;
        MACounter = _lineStartAddress;

        var cursorMode = (byte)((_registers[10] >> 5) & 0x03);
        var blinkPeriod = cursorMode == 3 ? 32 : 16;
        if (cursorMode is 2 or 3 && ++_cursorBlinkFrames >= blinkPeriod)
        {
            _cursorBlinkFrames = 0;
            _cursorBlinkVisible = !_cursorBlinkVisible;
        }
    }

    private static ushort SyncWidth(byte value) => value == 0 ? (ushort)16 : value;
}
