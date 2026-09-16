namespace PetEmulator.Cpc464;

public sealed class Cpc464GateArraySnapshot
{
    public CpcDisplayMode Mode { get; set; } public bool LowerRomEnabled { get; set; } public bool UpperRomEnabled { get; set; }
    public byte RamConfiguration { get; set; } public bool InterruptPending { get; set; } public byte[] Inks { get; set; } = [];
    public byte SelectedPen { get; set; } public CpcDisplayMode RequestedMode { get; set; } public bool PreviousHSync { get; set; }
    public int HsyncCount { get; set; } public byte[] Pixels { get; set; } = []; public int Width { get; set; } public int Height { get; set; }
}
public sealed class Cpc464KeyboardSnapshot { public bool[] Keys { get; set; } = []; }
public sealed class Cpc464CassetteSnapshot
{
    public byte[] Tape { get; set; } = []; public int ByteIndex { get; set; } public int BitIndex { get; set; } public int Remaining { get; set; }
    public bool PlaybackHigh { get; set; } public bool Started { get; set; } public int[] PulseTicks { get; set; } = []; public int PulseIndex { get; set; }
    public bool PulsePlayback { get; set; } public List<CassettePulseSnapshot> Recorded { get; set; } = []; public int RecordTicks { get; set; }
    public bool RecordLevel { get; set; } public bool Recording { get; set; } public bool MotorOn { get; set; } public bool Signal { get; set; }
}
public sealed class CassettePulseSnapshot { public int Ticks { get; set; } public bool Level { get; set; } }
