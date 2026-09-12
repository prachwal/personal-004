namespace PetEmulator.Chips;

/// <summary>Initial state and circular-register geometry of a MOS 6702 variant.</summary>
public sealed record MOS6702Variant
{
    public MOS6702Variant(byte initialValue, IReadOnlyList<int> shiftRegisterLengths)
    {
        ArgumentNullException.ThrowIfNull(shiftRegisterLengths);
        if (shiftRegisterLengths.Count != 8 || shiftRegisterLengths.Any(length => length is < 1 or > 8))
            throw new ArgumentException("A MOS 6702 variant needs eight shift lengths in the range 1..8.", nameof(shiftRegisterLengths));

        InitialValue = initialValue;
        ShiftRegisterLengths = [.. shiftRegisterLengths];
    }

    public byte InitialValue { get; }

    public IReadOnlyList<int> ShiftRegisterLengths { get; }

    public static MOS6702Variant Standard { get; } =
        new(0xD6, [6, 3, 7, 8, 1, 3, 5, 2]);
}
