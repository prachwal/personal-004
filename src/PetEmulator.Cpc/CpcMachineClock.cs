using PetEmulator.Core;

namespace PetEmulator.Cpc;

/// <summary>Advances CPC devices at the hardware clock ratio shared by CPC models.</summary>
public sealed class CpcMachineClock
{
    private readonly MachineClock _clock;

    public CpcMachineClock(CpcGateArray gateArray, CpcCassette cassette)
    {
        ArgumentNullException.ThrowIfNull(gateArray);
        ArgumentNullException.ThrowIfNull(cassette);
        _clock = new MachineClock(4, () =>
        {
            gateArray.Tick();
            cassette.Tick();
        });
    }

    public int DeviceCycleRemainder => _clock.DeviceCycleRemainder;

    public void Tick(int cpuCycles) => _clock.Tick(cpuCycles);

    public void Restore(int deviceCycleRemainder) => _clock.Restore(deviceCycleRemainder);
}
