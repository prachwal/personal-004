using PetEmulator.Chips;

namespace PetEmulator.Kaypro;

/// <summary>
/// Kaypro system-latch wiring around the generic FD1793.
/// The selected 81-149C ROM uses the board latch values 01=A and 02=B;
/// WAIT is an adapter line and is deliberately not part of the generic
/// FD179x state machine.
/// </summary>
public sealed class KayproFdcWiring
{
    private const int WaitWatchdogTStates = 1_817;
    private int _waitWatchdogRemaining;

    public KayproFdcWiring(FD1793 controller)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public FD1793 Controller { get; }
    public byte SystemPortValue { get; private set; } = 0x80;
    public int? SelectedDrive { get; private set; }
    public bool NormalCharacterSetSelected => (SystemPortValue & 0x40) == 0;
    public bool WaitAsserted { get; private set; }
    public bool InterruptRequested => Controller.IntrqAsserted || Controller.DrqAsserted;

    public void WriteSystemPort(byte value)
    {
        SystemPortValue = value;

        if ((value & 0x03) == 0x01)
        {
            SelectedDrive = 0;
            Controller.DriveSelect = 0x01;
        }
        else if ((value & 0x03) == 0x02)
        {
            SelectedDrive = 1;
            Controller.DriveSelect = 0x02;
        }
        else
        {
            SelectedDrive = null;
            Controller.DriveSelect = 0x00;
        }

        Controller.Side = (byte)((value & 0x04) != 0 ? 1 : 0);
    }

    public void BeginWait()
    {
        WaitAsserted = true;
        _waitWatchdogRemaining = WaitWatchdogTStates;
    }

    public void Tick(int tStates)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tStates);
        Controller.Tick(tStates);

        if (!WaitAsserted)
            return;

        _waitWatchdogRemaining -= tStates;
        if (_waitWatchdogRemaining <= 0 || InterruptRequested)
            WaitAsserted = false;
    }

    public void Reset()
    {
        Controller.Reset();
        SystemPortValue = 0x80;
        SelectedDrive = null;
        WaitAsserted = false;
        _waitWatchdogRemaining = 0;
        Controller.Side = 0;
    }
}
