namespace PetEmulator.Chips;

public enum Z80SioFrameMode
{
    Async,
    Sync8,
    Sync16,
    ExternalSync,
    Sdlc,
}

public readonly record struct Z80SioAsyncFormat(
    int DataBits,
    bool ParityEnabled,
    bool EvenParity,
    double StopBits)
{
    public static Z80SioAsyncFormat EightNOne => new(8, false, false, 1);
}

public readonly record struct Z80SioAsyncDecodeResult(
    bool Success,
    byte Value,
    bool ParityError,
    bool FramingError,
    int ConsumedBits);

public readonly record struct Z80SioSdlcDecodeResult(
    bool Success,
    IReadOnlyList<byte> Payload,
    bool CrcValid,
    bool Aborted,
    bool EndOfFrame);
