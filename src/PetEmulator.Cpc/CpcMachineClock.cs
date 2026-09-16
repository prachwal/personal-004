namespace PetEmulator.Cpc;

/// <summary>Advances CPC devices at the hardware clock ratio shared by CPC models.</summary>
public sealed class CpcMachineClock(CpcGateArray gateArray, CpcCassette cassette)
{
    private int _deviceCycleRemainder;

    public int DeviceCycleRemainder => _deviceCycleRemainder;

    public void Tick(int cpuCycles)
    {
        _deviceCycleRemainder += cpuCycles;
        while (_deviceCycleRemainder >= 4)
        {
            _deviceCycleRemainder -= 4;
            gateArray.Tick();
            cassette.Tick();
        }
    }

    public void Restore(int deviceCycleRemainder)
    {
        if (deviceCycleRemainder is < 0 or >= 4)
            throw new ArgumentOutOfRangeException(nameof(deviceCycleRemainder));
        _deviceCycleRemainder = deviceCycleRemainder;
    }
}
