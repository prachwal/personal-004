namespace PetEmulator.Chips;

/// <summary>
/// WD FD1793 variant: the FD1791 command/transfer core with a true data bus.
/// </summary>
public sealed class FD1793 : FD1791
{
    public FD1793(
        IFD1791DiskImage? disk = null,
        int dataByteTStates = DefaultDataByteTStates,
        int seekTStates = DefaultSeekTStates,
        int sectorTStates = DefaultSectorTStates,
        long indexPeriodTStates = DefaultIndexPeriodTStates,
        long indexPulseWidthTStates = DefaultIndexPulseWidthTStates)
        : base(disk, dataByteTStates, seekTStates, sectorTStates, indexPeriodTStates, indexPulseWidthTStates)
    {
    }

    public override Fd179xDataBusMode DataBusMode => Fd179xDataBusMode.True;
}
