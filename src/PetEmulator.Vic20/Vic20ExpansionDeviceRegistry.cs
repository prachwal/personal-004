using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20;

/// <summary>Owns mounted expansion devices and validates their address resources atomically.</summary>
internal sealed class Vic20ExpansionDeviceRegistry
{
    private readonly IReadOnlyList<Vic20CartridgeResource> _reservedResources;
    private readonly List<IVic20ExpansionDevice> _devices = [];

    public Vic20ExpansionDeviceRegistry(IReadOnlyList<Vic20CartridgeResource> reservedResources)
    {
        _reservedResources = reservedResources;
    }

    public IReadOnlyList<IVic20ExpansionDevice> Devices => _devices;

    public bool Irq => _devices.Any(device => device.Irq);

    public void Insert(IVic20ExpansionDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        Vic20CartridgeResourceValidator.ThrowIfConflicting(
            _reservedResources.Concat(_devices.SelectMany(item => item.Resources))
                .Concat(device.Resources));
        _devices.Add(device);
    }

    public void EjectAll() => _devices.Clear();

    public void Eject(IVic20ExpansionDevice device) => _devices.Remove(device);

    public bool TryRead(ushort address, out byte value)
    {
        foreach (var device in _devices)
            if (device.TryRead(address, out value))
                return true;

        value = 0;
        return false;
    }

    public bool TryWrite(ushort address, byte value)
    {
        foreach (var device in _devices)
            if (device.TryWrite(address, value))
                return true;

        return false;
    }

    public void Reset()
    {
        foreach (var device in _devices)
            device.Reset();
    }

    public void Tick(ulong cycles)
    {
        foreach (var device in _devices)
            device.Tick(cycles);
    }
}
