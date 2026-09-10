using PetEmulator.Chips;

namespace PetEmulator.Pet.Ieee488;

/// <summary>
/// Binds a <see cref="PetIeeeBus"/> to PIA2 (DIO on Port A/B, NDAC on CA2, DAV on CB2) and VIA
/// Port B (ATN out on bit 2; NDAC/NRFD/DAV in on bits 0/6/7). Drives the PIA's actual CA2/CB2
/// edge-tracked output lines, matching how a real PIA2 would present the handshake to software.
///
/// Call <see cref="Tick"/> once per CPU cycle (same pattern as <c>Tape.PetDatasette</c>) so the
/// bus's talker-side byte prefetch delay advances and VIA Port B stays in sync even when nothing
/// else touches the bus this cycle.
///
/// NOTE (cross-dependency): <see cref="MT6520"/> and <see cref="MOS6522"/> are ported into
/// PetEmulator.Chips by a parallel effort; this file assumes the same member names as
/// personal-001's Emulator.Chips.MT6520/MOS6522 (PortAInput, PortBWritten, Ca2OutputChanged,
/// Cb2OutputChanged, PortBInput, DDRB). If that port lands with a different shape, this file will
/// need a small follow-up fix.
/// </summary>
public sealed class PetIeeeBusBinding
{
    private readonly PetIeeeBus _bus;
    private readonly MOS6522 _via;
    private bool _lastCb2;

    public PetIeeeBusBinding(MT6520 pia2, MOS6522 via, PetIeeeBus bus)
    {
        ArgumentNullException.ThrowIfNull(pia2);
        _via = via ?? throw new ArgumentNullException(nameof(via));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));

        // KERNAL's ACPTR reads DIO via LDA on PIA2 Port A; each read consumes the cached talker
        // byte and advances the listener-ready handshake (OnDioRead, not a passive peek).
        pia2.PortAInput = () => (byte)(_bus.OnDioRead() ^ 0xFF);
        pia2.PortBWritten = value =>
        {
            _bus.OnDioWrite((byte)(value ^ 0xFF));
            SyncViaPortBInput();
        };
        // CA2 output = NDAC out: true means accepted/released, false means active - see
        // PetIeeeBus.SetNdacAccepted's own doc comment for the polarity this expects.
        pia2.Ca2OutputChanged = value =>
        {
            _bus.SetNdacAccepted(value);
            SyncViaPortBInput();
        };
        // CB2 output = DAV out, active low. A false->true transition (DAV asserted->deasserted)
        // means this byte's handshake is done.
        pia2.Cb2OutputChanged = value =>
        {
            _bus.SetDAVState(!value);
            if (value && !_lastCb2)
                _bus.CompleteHandshake();
            _lastCb2 = value;
            SyncViaPortBInput();
        };

        _via.PortBWritten = value =>
        {
            if ((_via.DDRB & 0x04) != 0)
                _bus.OnATNWrite((value & 0x04) == 0);
            SyncViaPortBInput();
        };

        SyncViaPortBInput();
    }

    /// <summary>Advances the bus's device-side timing and refreshes VIA Port B's input bits.</summary>
    public void Tick()
    {
        _bus.Tick();
        SyncViaPortBInput();
    }

    public void Reset()
    {
        _bus.Reset();
        _lastCb2 = false;
        SyncViaPortBInput();
    }

    private void SyncViaPortBInput() => _via.PortBInput = _bus.GetViaPortBInput();
}
