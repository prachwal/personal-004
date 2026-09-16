namespace PetEmulator.Cpc;

/// <summary>Common CPC keyboard matrix state. Machine-specific keyboards may derive from this.</summary>
public class CpcKeyboardSnapshot { public bool[] Keys { get; set; } = []; }
public sealed class CpcCassetteSnapshot
{
    public byte[] Tape { get; set; } = []; public int ByteIndex { get; set; } public int BitIndex { get; set; } public int Remaining { get; set; }
    public bool PlaybackHigh { get; set; } public bool Started { get; set; } public int[] PulseTicks { get; set; } = []; public int PulseIndex { get; set; }
    public bool PulsePlayback { get; set; } public List<CassettePulseSnapshot> Recorded { get; set; } = []; public int RecordTicks { get; set; }
    public bool RecordLevel { get; set; } public bool Recording { get; set; } public bool MotorOn { get; set; } public bool Signal { get; set; }
}
public sealed class CassettePulseSnapshot { public int Ticks { get; set; } public bool Level { get; set; } }
