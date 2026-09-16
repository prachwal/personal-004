namespace PetEmulator.Cpc6128;

public sealed class Cpc6128MemorySnapshot
{
    public byte[] PhysicalRam { get; set; } = [];
    public byte UpperRomNumber { get; set; }
}

public sealed class Cpc6128PortsSnapshot
{
    public byte PortA { get; set; }
    public byte PortB { get; set; }
    public byte PortC { get; set; }
    public byte PpiControl { get; set; }
}
