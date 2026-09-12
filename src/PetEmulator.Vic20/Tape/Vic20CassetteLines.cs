using PetEmulator.Chips;

namespace PetEmulator.Vic20.Tape;

/// <summary>
/// Physical VIC-20 cassette-port lines as seen by the two VIAs. This class deliberately knows
/// nothing about tape pulses or KERNAL SAVE/LOAD; it only translates deck state and line
/// transitions to the real VIA pins.
/// </summary>
public sealed class Vic20CassetteLines
{
    private readonly MOS6522 _via1;
    private readonly MOS6522 _via2;

    public Vic20CassetteLines(MOS6522 via1, MOS6522 via2)
    {
        _via1 = via1 ?? throw new ArgumentNullException(nameof(via1));
        _via2 = via2 ?? throw new ArgumentNullException(nameof(via2));
        SetPlaySense(false);
    }

    /// <summary>VIA1 CA2, active-low cassette motor control.</summary>
    public bool MotorOn => (_via1.PCR & 0x0E) >= 0x08 && !_via1.CA2Output;

    /// <summary>VIA1 PA6, active-low PLAY/sense input.</summary>
    public bool Sense => (_via1.PortAInput & 0x40) == 0;

    /// <summary>VIA2 PB3, the physical cassette WRITE output level.</summary>
    public bool WriteLevel => (_via2.PortBOutput & 0x08) != 0;

    /// <summary>Updates the physical PLAY/sense switch without disturbing other VIA1 inputs.</summary>
    public void SetPlaySense(bool pressed)
    {
        _via1.PortAInput = (byte)(pressed
            ? (_via1.PortAInput & ~0x40)
            : (_via1.PortAInput | 0x40));
    }

    /// <summary>Produces one guaranteed read transition on VIA2 CA1.</summary>
    public void PulseRead()
    {
        _via2.CA1 = true;
        _via2.CA1 = false;
        _via2.CA1 = true;
    }
}
