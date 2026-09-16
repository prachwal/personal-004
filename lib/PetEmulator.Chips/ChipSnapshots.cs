namespace PetEmulator.Chips;

public sealed class MT6545Snapshot
{
    public byte[] Registers { get; set; } = []; public byte SelectedRegister { get; set; } public ushort HorizontalCounter { get; set; }
    public ushort VerticalCounter { get; set; } public byte RasterCounter { get; set; } public byte VerticalAdjustCounter { get; set; }
    public ushort LineStartAddress { get; set; } public bool InVerticalAdjust { get; set; } public bool LightPenRegistered { get; set; }
    public bool CursorBlinkVisible { get; set; } public byte CursorBlinkFrames { get; set; } public bool InterlaceField { get; set; }
    public bool[] DePipe { get; set; } = []; public bool[] CursorPipe { get; set; } = []; public ushort MACounter { get; set; }
    public bool HSync { get; set; } public bool VSync { get; set; } public bool DisplayEnable { get; set; } public bool VerticalBlanking { get; set; } public bool CursorEnable { get; set; }
}
public sealed class Ay38910Snapshot
{
    public byte[] Registers { get; set; } = []; public byte Selected { get; set; } public double[] TonePhase { get; set; } = [];
    public double NoisePhase { get; set; } public uint NoiseShift { get; set; } public double EnvPhase { get; set; } public int EnvStep { get; set; }
    public bool EnvRising { get; set; } public bool EnvDone { get; set; }
}
