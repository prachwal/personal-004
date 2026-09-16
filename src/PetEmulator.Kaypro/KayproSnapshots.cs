using PetEmulator.Chips;
using PetEmulator.Core;

namespace PetEmulator.Kaypro;

public sealed class KayproVideoSnapshot
{
    public byte[] Memory { get; set; } = [];
}

public sealed class KayproFdcSnapshot
{
    public FD1791Snapshot Controller { get; set; } = null!;
    public byte SystemPortValue { get; set; }
    public int? SelectedDrive { get; set; }
    public bool WaitAsserted { get; set; }
    public int WaitWatchdogRemaining { get; set; }
}

public sealed class KayproSnapshot : MachineSnapshot
{
    public byte[] Ram { get; set; } = [];
    public KayproVideoSnapshot Video { get; set; } = null!;
    public KayproFdcSnapshot Fdc { get; set; } = null!;
    public Z80SioState Sio { get; set; } = null!;
    public Z80PioState PioG { get; set; } = null!;
    public Z80PioState PioS { get; set; } = null!;
    public Z80PioInterruptChainState PioInterruptChain { get; set; } = null!;
    public ulong FdcNmiPulseCount { get; set; }
    public bool FdcNmiPulseActive { get; set; }
}
