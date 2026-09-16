using PetEmulator.Chips;
using PetEmulator.Core;
using PetEmulator.Vic20.Keyboard;
using PetEmulator.Vic20.Tape;

namespace PetEmulator.Vic20;

public sealed class Vic20MemorySnapshot
{
    public byte[] ZeroPageRam { get; set; } = [];
    public byte[] BuiltinRam { get; set; } = [];
}

public sealed class Vic20KeyboardSnapshot
{
    public byte[] PressedColumns { get; set; } = [];
    public byte RowMask { get; set; }
}

public sealed class Vic20JoystickSnapshot
{
    public bool Up { get; set; }
    public bool Down { get; set; }
    public bool Left { get; set; }
    public bool Right { get; set; }
    public bool Fire { get; set; }
}

public sealed class Vic20UserPortSnapshot
{
    public byte Input { get; set; }
    public byte Output { get; set; }
    public byte Direction { get; set; }
}

public sealed class Vic20DatasetteSnapshot
{
    public int[] PulseCycles { get; set; } = [];
    public int PulseIndex { get; set; }
    public int CyclesUntilNextEdge { get; set; }
    public bool PlayPressed { get; set; }
    public string? TapeName { get; set; }
}

/// <summary>Complete in-memory state of an unexpanded VIC-20.
/// Cartridge and other polymorphic expansion-device state is intentionally not included in v1;
/// restoring a snapshot never silently inserts or removes an expansion device.</summary>
public sealed class Vic20Snapshot : MachineSnapshot
{
    public Vic20MemorySnapshot Memory { get; set; } = null!;
    public MOS6560Snapshot Vic { get; set; } = null!;
    public MOS6522Snapshot Via1 { get; set; } = null!;
    public MOS6522Snapshot Via2 { get; set; } = null!;
    public MOS2114Snapshot ColorRam { get; set; } = null!;
    public Vic20KeyboardSnapshot Keyboard { get; set; } = null!;
    public Vic20JoystickSnapshot Joystick { get; set; } = null!;
    public Vic20UserPortSnapshot UserPort { get; set; } = null!;
    public Vic20DatasetteSnapshot Datasette { get; set; } = null!;
    public bool DiskActivityPending { get; set; }
    public ushort LastIrqVector { get; set; }
}
