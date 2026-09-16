using PetEmulator.Chips;
using PetEmulator.Core;

namespace PetEmulator.Trs80;

public sealed class Trs80MemorySnapshot
{
    public byte[] Ram { get; set; } = [];
}

public sealed class Trs80KeyboardSnapshot
{
    public byte[] Rows { get; set; } = [];
}

public sealed class Trs80CassettePulseSnapshot
{
    public int DurationTStates { get; set; }
    public byte Level { get; set; }
}

public sealed class Trs80CassetteSnapshot
{
    public byte[] Tape { get; set; } = [];
    public int ByteIndex { get; set; }
    public int BitIndex { get; set; }
    public int SegmentIndex { get; set; }
    public int Remaining { get; set; }
    public bool PulsePending { get; set; }
    public bool Started { get; set; }
    public bool Recording { get; set; }
    public int RecordDuration { get; set; }
    public byte RecordLevel { get; set; }
    public bool MotorOn { get; set; }
    public Trs80CassettePulseSnapshot[] RecordedPulses { get; set; } = [];
}

public sealed class Trs80FdcSnapshot
{
    public FD1791Snapshot Controller { get; set; } = null!;
    public int? SelectedDrive { get; set; }
}

public sealed class Trs80Snapshot : MachineSnapshot
{
    public Trs80MemorySnapshot Memory { get; set; } = null!;
    public Trs80KeyboardSnapshot Keyboard { get; set; } = null!;
    public Trs80FdcSnapshot? Fdc { get; set; }
    public Trs80CassetteSnapshot? Cassette { get; set; }
}
