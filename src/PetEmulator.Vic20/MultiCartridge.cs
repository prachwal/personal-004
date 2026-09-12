using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20;

/// <summary>
/// Groups independent VIC-20 expansion devices into one attachable unit. Resource conflicts are
/// validated once for the complete group; bus decoding remains owned by the existing registry.
/// </summary>
public sealed class MultiCartridge : IVic20ExpansionDevice
{
    private readonly IReadOnlyList<IVic20ExpansionDevice> _devices;

    public MultiCartridge(IEnumerable<IVic20ExpansionDevice> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);
        _devices = devices.ToArray();
        if (_devices.Count == 0)
            throw new ArgumentException("A multi-cartridge must contain at least one device.", nameof(devices));

        if (_devices.Any(device => device is null))
            throw new ArgumentException("A multi-cartridge cannot contain a null device.", nameof(devices));

        Resources = [.. _devices.SelectMany(device => device.Resources)];
        Vic20CartridgeResourceValidator.ThrowIfConflicting(Resources);
    }

    public IReadOnlyList<IVic20ExpansionDevice> Devices => _devices;

    public IReadOnlyList<Vic20CartridgeResource> Resources { get; }

    public bool Irq => _devices.Any(device => device.Irq);

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
