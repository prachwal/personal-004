using PetEmulator.Chips;
using PetEmulator.Core;

namespace PetEmulator.Cpc;

/// <summary>Common snapshot state shared by CPC machine families.</summary>
public abstract class CpcMachineSnapshot : IMachineSnapshot
{
    public int Version { get; set; } = 1;
    public CpuDebugSnapshot Cpu { get; set; } = null!;
    public ulong FrameCount { get; set; }
    public int DeviceCycleRemainder { get; set; }
    public ulong FrameCycles { get; set; }
    public CpcGateArraySnapshot GateArray { get; set; } = null!;
    public MT6545Snapshot Crtc { get; set; } = null!;
    public Ay38910Snapshot Ay { get; set; } = null!;
    public CpcKeyboardSnapshot Keyboard { get; set; } = null!;
    public CpcCassetteSnapshot Cassette { get; set; } = null!;
}
