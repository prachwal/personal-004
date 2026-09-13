namespace PetEmulator.Chips;

/// <summary>Physical polarity of the external handshake inputs.</summary>
public readonly record struct Z80PioSignalPolarity(
    bool StrobeActiveHigh = true,
    bool ReadyActiveHigh = true)
{
    public static Z80PioSignalPolarity ActiveHigh => new();
    public static Z80PioSignalPolarity ActiveLow => new(false, false);
}

/// <summary>Converts physical handshake levels to the logical PIO port contract.</summary>
public sealed class Z80PioSignalAdapter : IZ80PioPort
{
    private readonly IZ80PioPort _inner;
    private readonly Z80PioSignalPolarity _polarity;

    public Z80PioSignalAdapter(IZ80PioPort inner)
        : this(inner, Z80PioSignalPolarity.ActiveHigh)
    {
    }

    public Z80PioSignalAdapter(IZ80PioPort inner, Z80PioSignalPolarity polarity)
    {
        _inner = inner;
        _polarity = polarity;
        _inner.InputChanged += value => InputChanged?.Invoke(value);
        _inner.StrobeChanged += value => StrobeChanged?.Invoke(Decode(value, _polarity.StrobeActiveHigh));
        _inner.OutputChanged += value => OutputChanged?.Invoke(value);
        _inner.ReadyChanged += value => ReadyChanged?.Invoke(Decode(value, _polarity.ReadyActiveHigh));
    }

    public byte InputValue => _inner.InputValue;
    public bool StrobeAsserted => Decode(_inner.StrobeAsserted, _polarity.StrobeActiveHigh);
    public bool Ready => Decode(_inner.Ready, _polarity.ReadyActiveHigh);
    public event Action<byte>? InputChanged;
    public event Action<bool>? StrobeChanged;
    public event Action<byte>? OutputChanged;
    public event Action<bool>? ReadyChanged;

    public void DriveInput(byte value) => _inner.DriveInput(value);
    public void SetStrobe(bool asserted) => _inner.SetStrobe(Encode(asserted, _polarity.StrobeActiveHigh));
    public void WriteOutput(byte value) => _inner.WriteOutput(value);
    public void SetReady(bool asserted) => _inner.SetReady(Encode(asserted, _polarity.ReadyActiveHigh));

    private static bool Decode(bool physical, bool activeHigh) => activeHigh ? physical : !physical;
    private static bool Encode(bool logical, bool activeHigh) => activeHigh ? logical : !logical;
}
