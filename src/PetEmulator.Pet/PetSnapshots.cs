using PetEmulator.Chips;
using PetEmulator.Core;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet;

public sealed class PetMemorySnapshot
{
    public byte[] Ram { get; set; } = [];
    public byte[]? ExpansionRam { get; set; }
    public byte ExpansionControl { get; set; }
}

public sealed class PetKeyboardSnapshot
{
    public byte[] PressedColumns { get; set; } = [];
}

public sealed class PetDatasetteSnapshot
{
    public int[] PulseCycles { get; set; } = [];
    public int PulseIndex { get; set; }
    public int CyclesUntilNextEdge { get; set; }
    public bool PlayPressed { get; set; }
    public bool LastMotorOn { get; set; }
    public string? TapeName { get; set; }
}

public sealed class PetDatasette2Snapshot
{
    public int[] PulseCycles { get; set; } = [];
    public bool PlayPressed { get; set; }
    public string? TapeName { get; set; }
}

public sealed class PetUserPortSnapshot
{
    public byte Input { get; set; }
    public bool HandshakeInput { get; set; }
    public byte Output { get; set; }
    public byte Direction { get; set; }
    public bool HandshakeOutput { get; set; }
}

/// <summary>State of a non-SuperPET machine. SuperPET processor and expansion-board state is
/// intentionally not represented; capture and restore reject that profile instead of silently
/// producing an incomplete snapshot.</summary>
public sealed class PetSnapshot : MachineSnapshot
{
    public string ProfileId { get; set; } = string.Empty;
    public string ExpansionRomManifestFingerprint { get; set; } = string.Empty;
    public PetMemorySnapshot Memory { get; set; } = null!;
    public MT6520Snapshot Pia1 { get; set; } = null!;
    public MT6520Snapshot Pia2 { get; set; } = null!;
    public MOS6522Snapshot Via { get; set; } = null!;
    public MT6545Snapshot? Crtc { get; set; }
    public PetKeyboardSnapshot Keyboard { get; set; } = null!;
    public PetDatasetteSnapshot Datasette { get; set; } = null!;
    public PetDatasette2Snapshot Datasette2 { get; set; } = null!;
    public PetUserPortSnapshot UserPort { get; set; } = null!;
    public byte KeyboardSelectedRow { get; set; }
    public bool DiskActivityPending { get; set; }
    public long IeeeByteCount { get; set; }
    public int Pia1Cb1Phase { get; set; }
}
