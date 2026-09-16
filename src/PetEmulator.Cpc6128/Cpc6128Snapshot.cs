using PetEmulator.Cpc;
using PetEmulator.CpcFdc;

namespace PetEmulator.Cpc6128;

public sealed class Cpc6128Snapshot : CpcMachineSnapshot
{
    public Cpc6128MemorySnapshot Memory { get; set; } = null!;
    public Cpc6128PortsSnapshot Ports { get; set; } = null!;
    public I8272Snapshot Fdc { get; set; } = null!;
}
