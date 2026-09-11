using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20;

/// <summary>Owns mounted expansion devices and validates their address resources atomically.</summary>
internal sealed class Vic20ExpansionDeviceRegistry
{
    private readonly IReadOnlyList<Vic20CartridgeResource> _reservedResources;
    private readonly List<Vic20Cartridge> _devices = [];

    public Vic20ExpansionDeviceRegistry(IReadOnlyList<Vic20CartridgeResource> reservedResources)
    {
        _reservedResources = reservedResources;
    }

    public IReadOnlyList<Vic20Cartridge> Devices => _devices;

    public void Insert(Vic20Cartridge device)
    {
        ArgumentNullException.ThrowIfNull(device);
        Vic20CartridgeResourceValidator.ThrowIfConflicting(
            _reservedResources.Concat(_devices.SelectMany(item => item.Resources))
                .Concat(device.Resources));
        _devices.Add(device);
    }

    public void EjectAll() => _devices.Clear();

    public void Eject(Vic20Cartridge device) => _devices.Remove(device);

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
