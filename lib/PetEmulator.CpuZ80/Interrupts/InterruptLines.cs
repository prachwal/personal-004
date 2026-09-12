namespace PetEmulator.CpuZ80.Interrupts;

public sealed class InterruptLines : IInterruptLines
{
    public bool IntAsserted { get; private set; }
    public bool NmiAsserted { get; private set; }
    public bool WaitAsserted { get; private set; }

    public void SetInt(bool asserted) => IntAsserted = asserted;

    public void SetNmi(bool asserted) => NmiAsserted = asserted;

    public void SetWait(bool asserted) => WaitAsserted = asserted;

    public void Clear()
    {
        IntAsserted = false;
        NmiAsserted = false;
        WaitAsserted = false;
    }
}
