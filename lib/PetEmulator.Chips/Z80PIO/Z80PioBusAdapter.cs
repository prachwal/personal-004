namespace PetEmulator.Chips;

/// <summary>Logical, active-high representation of a Z80 I/O bus cycle.</summary>
public readonly record struct Z80PioBusSignals(
    bool ChipEnabled,
    bool IoRequest,
    bool Read,
    bool M1,
    bool ControlSelect,
    bool PortB)
{
    public bool MachineCycle => M1;
}

/// <summary>Adapts Z80 CPU bus control lines to a PIO without embedding a machine map.</summary>
public sealed class Z80PioBusAdapter(Z80PioDevice pio)
{
    public bool TryRead(Z80PioBusSignals signals, out byte value)
    {
        value = 0xFF;
        if (!IsIoAccess(signals) || !signals.Read)
            return false;

        value = pio.ReadPort(ToPort(signals));
        return true;
    }

    public bool TryWrite(Z80PioBusSignals signals, byte value)
    {
        if (!IsIoAccess(signals) || signals.Read)
            return false;

        pio.WritePort(ToPort(signals), value);
        return true;
    }

    public bool TryAcknowledgeInterrupt(Z80PioBusSignals signals, out byte vector)
    {
        vector = 0;
        if (!signals.ChipEnabled || !signals.IoRequest || !signals.M1)
            return false;

        return pio.TryAcknowledgeInterrupt(out vector);
    }

    public void Reset(bool resetAsserted)
    {
        if (resetAsserted)
            pio.Reset();
    }

    private static bool IsIoAccess(Z80PioBusSignals signals) =>
        signals.ChipEnabled && signals.IoRequest && !signals.M1;

    private static ushort ToPort(Z80PioBusSignals signals) =>
        (ushort)((signals.PortB ? 1 : 0) + (signals.ControlSelect ? 2 : 0));
}
