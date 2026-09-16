using PetEmulator.Chips;
using PetEmulator.Core;
using PetEmulator.Cpc464;
using PetEmulator.CpcFdc;

namespace PetEmulator.Cpc6128;

public sealed class Cpc6128Snapshot
{
    public int Version { get; set; } = 1;
    public CpuDebugSnapshot Cpu { get; set; } = null!;
    public byte[] PhysicalRam { get; set; } = [];
    public byte UpperRomNumber { get; set; }
    public ulong FrameCount { get; set; }
    public int DeviceCycleRemainder { get; set; }
    public ulong FrameCycles { get; set; }
    public Cpc464GateArraySnapshot GateArray { get; set; } = null!;
    public MT6545Snapshot Crtc { get; set; } = null!;
    public Ay38910Snapshot Ay { get; set; } = null!;
    public Cpc464KeyboardSnapshot Keyboard { get; set; } = null!;
    public Cpc464CassetteSnapshot Cassette { get; set; } = null!;
    public Cpc6128PortsSnapshot Ports { get; set; } = null!;
    public I8272Snapshot Fdc { get; set; } = null!;
}
