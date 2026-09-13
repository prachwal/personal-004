namespace PetEmulator.Chips;

public readonly record struct Z80SioBusSignals(
    bool ChipEnabled,
    bool IoRequest,
    bool Read,
    bool M1,
    bool ControlSelect,
    bool ChannelB)
{
    /// <summary>Compatibility alias for callers that model bus cycles generically.</summary>
    public bool MachineCycle => M1;
}

/// <summary>Maps active-high Z80 bus signals to the four SIO port functions.</summary>
public sealed class Z80SioBusAdapter(Z80Sio sio)
{
    public bool TryRead(Z80SioBusSignals signals, out byte value)
    {
        value = 0xFF;
        if (!IsIoAccess(signals) || !signals.Read)
            return false;
        value = sio.ReadPort(ToPort(signals));
        return true;
    }

    public bool TryWrite(Z80SioBusSignals signals, byte value)
    {
        if (!IsIoAccess(signals) || signals.Read)
            return false;
        sio.WritePort(ToPort(signals), value);
        return true;
    }

    public bool TryAcknowledgeInterrupt(Z80SioBusSignals signals, out byte vector)
    {
        vector = 0;
        if (!signals.ChipEnabled || !signals.IoRequest || !signals.M1)
            return false;
        return sio.TryAcknowledgeInterrupt(out vector);
    }

    public void Reset(bool resetAsserted)
    {
        if (resetAsserted)
            sio.Reset();
    }

    private static bool IsIoAccess(Z80SioBusSignals signals) =>
        signals.ChipEnabled && signals.IoRequest && !signals.M1;

    private static ushort ToPort(Z80SioBusSignals signals) =>
        (ushort)((signals.ChannelB ? 1 : 0) + (signals.ControlSelect ? 2 : 0));
}
