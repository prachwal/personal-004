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

public sealed class MOS6522Snapshot
{
    public byte Orb { get; set; }
    public byte Ora { get; set; }
    public byte Ddrb { get; set; }
    public byte Ddra { get; set; }
    public ushort T1Counter { get; set; }
    public ushort T1Latch { get; set; }
    public ushort T2Counter { get; set; }
    public ushort T2Latch { get; set; }
    public byte ShiftRegister { get; set; }
    public byte Acr { get; set; }
    public byte Pcr { get; set; }
    public byte Ifr { get; set; }
    public byte Ier { get; set; }
    public byte LatchedPortA { get; set; }
    public byte LatchedPortB { get; set; }
    public bool Ca1 { get; set; }
    public bool Ca2 { get; set; }
    public bool Cb1 { get; set; }
    public bool Cb2 { get; set; }
    public bool PreviousCa1 { get; set; }
    public bool PreviousCa2 { get; set; }
    public bool PreviousCb1 { get; set; }
    public bool PreviousCb2 { get; set; }
    public bool T1Running { get; set; }
    public bool T2Running { get; set; }
    public bool T1OneShotArmed { get; set; }
    public bool T2OneShotArmed { get; set; }
    public bool T1Pb7 { get; set; }
    public int ShiftCount { get; set; }
    public bool Ca2Output { get; set; }
    public bool Cb2Output { get; set; }
    public bool Irq { get; set; }
    public byte PortAInput { get; set; }
    public byte PortBInput { get; set; }
}

public sealed class MOS6560Snapshot
{
    public MOS6560Standard Standard { get; set; }
    public double SampleRate { get; set; }
    public byte[] Registers { get; set; } = [];
    public int RasterCounter { get; set; }
    public long LineCycles { get; set; }
    public double[] AudioPhases { get; set; } = [];
    public ushort NoiseLfsr { get; set; }
    public byte LightPenX { get; set; }
    public byte LightPenY { get; set; }
    public byte PaddleX { get; set; }
    public byte PaddleY { get; set; }
}

public sealed class MOS2114Snapshot
{
    public byte[] Data { get; set; } = [];
}
