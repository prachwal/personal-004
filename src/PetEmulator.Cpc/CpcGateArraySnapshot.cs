namespace PetEmulator.Cpc;

public sealed class CpcGateArraySnapshot
{
    public CpcDisplayMode Mode { get; set; }
    public bool LowerRomEnabled { get; set; }
    public bool UpperRomEnabled { get; set; }
    public byte RamConfiguration { get; set; }
    public bool InterruptPending { get; set; }
    public byte[] Inks { get; set; } = [];
    public byte SelectedPen { get; set; }
    public CpcDisplayMode RequestedMode { get; set; }
    public bool PreviousHSync { get; set; }
    public int HsyncCount { get; set; }
    public byte[] Pixels { get; set; } = [];
    public int Width { get; set; }
    public int Height { get; set; }
}
