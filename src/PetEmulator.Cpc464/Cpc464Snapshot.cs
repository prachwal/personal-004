using PetEmulator.Core;
using PetEmulator.Cpc;

namespace PetEmulator.Cpc464;

public sealed class Cpc464MemorySnapshot
{
    public byte[] Ram { get; set; } = [];
}

public sealed class Cpc464PortsSnapshot
{
    public byte PortA { get; set; }
    public byte PortB { get; set; }
    public byte PortC { get; set; }
    public byte PpiControl { get; set; }
}

public sealed class Cpc464Snapshot : CpcMachineSnapshot
{
    public Cpc464MemorySnapshot Memory { get; set; } = null!;
    public Cpc464PortsSnapshot Ports { get; set; } = null!;
}
