namespace PetEmulator.Core;

/// <summary>Advances one or more devices at a fixed CPU-to-device clock ratio.</summary>
public sealed class MachineClock(int deviceCyclesPerCpuCycle, Action tickDevice)
{
    private readonly int _deviceCyclesPerCpuCycle = deviceCyclesPerCpuCycle > 0
        ? deviceCyclesPerCpuCycle
        : throw new ArgumentOutOfRangeException(nameof(deviceCyclesPerCpuCycle));
    private readonly Action _tickDevice = tickDevice ?? throw new ArgumentNullException(nameof(tickDevice));
    private int _deviceCycleRemainder;

    public int DeviceCycleRemainder => _deviceCycleRemainder;

    public void Tick(int cpuCycles)
    {
        if (cpuCycles < 0)
            throw new ArgumentOutOfRangeException(nameof(cpuCycles));

        _deviceCycleRemainder += cpuCycles;
        while (_deviceCycleRemainder >= _deviceCyclesPerCpuCycle)
        {
            _deviceCycleRemainder -= _deviceCyclesPerCpuCycle;
            _tickDevice();
        }
    }

    public void Restore(int deviceCycleRemainder)
    {
        if (deviceCycleRemainder is < 0 || deviceCycleRemainder >= _deviceCyclesPerCpuCycle)
            throw new ArgumentOutOfRangeException(nameof(deviceCycleRemainder));
        _deviceCycleRemainder = deviceCycleRemainder;
    }
}
