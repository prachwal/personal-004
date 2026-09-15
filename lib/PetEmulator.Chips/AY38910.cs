namespace PetEmulator.Chips;

/// <summary>Minimal register-accurate AY-3-8910 sound generator used by CPC464.</summary>
public sealed class Ay38910
{
    private readonly int[] _toneCounters = new int[3];
    private readonly bool[] _toneStates = new bool[3];
    private int _noiseCounter;
    private int _noiseShift = 1;
    private int _envelopeCounter;
    private int _envelopeStep;
    public byte[] Registers { get; } = new byte[16];
    public byte Selected { get; private set; }
    public double Clock { get; set; } = 1_000_000;
    public void Reset() { Array.Clear(Registers); Array.Clear(_toneCounters); Array.Clear(_toneStates); Selected = 0; _noiseCounter = 0; _noiseShift = 1; _envelopeCounter = _envelopeStep = 0; }
    public byte ReadSelected() => Registers[Selected];
    public void WritePort(byte port, byte value) { if ((port & 1) == 0) Selected = (byte)(value & 0x0F); else { Registers[Selected] = (byte)(value & Mask(Selected)); if (Selected == 13) _envelopeStep = (Registers[13] & 4) != 0 ? 0 : 15; } }
    public int TonePeriod(int channel) { var p = Registers[channel * 2] | (Registers[channel * 2 + 1] << 8); return p == 0 ? 1 : p; }
    public int NoisePeriod => (Registers[6] & 0x1F) == 0 ? 1 : Registers[6] & 0x1F;
    public int EnvelopePeriod => (Registers[11] | (Registers[12] << 8)) == 0 ? 1 : Registers[11] | (Registers[12] << 8);
    public int EnvelopeLevel => _envelopeStep;
    public int NoiseShift => _noiseShift;
    public void Tick()
    {
        for (var channel = 0; channel < 3; channel++) if (++_toneCounters[channel] >= TonePeriod(channel)) { _toneCounters[channel] = 0; _toneStates[channel] = !_toneStates[channel]; }
        if (++_noiseCounter >= NoisePeriod * 2) { _noiseCounter = 0; var feedback = (_noiseShift ^ (_noiseShift >> 3)) & 1; _noiseShift = (_noiseShift >> 1) | (feedback << 16); }
        if (++_envelopeCounter >= EnvelopePeriod * 32) { _envelopeCounter = 0; if (_envelopeStep > 0) _envelopeStep--; }
    }
    private static byte Mask(int register) => register is 1 or 3 or 5 or 13 ? (byte)0x0F : register == 6 ? (byte)0x1F : register is 8 or 9 or 10 ? (byte)0x1F : (byte)0xFF;
}
