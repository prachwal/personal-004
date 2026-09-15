namespace PetEmulator.Cpc464;

public sealed class Cpc464Cassette
{
    public bool MotorOn { get; private set; }
    public bool Signal { get; set; }
    public void Reset() => MotorOn = false;
    public void SetMotor(bool enabled) => MotorOn = enabled;
    public bool ReadSignal() => Signal;
    public void Tick() { }
}
