using PetEmulator.Chips;
using PetEmulator.Audio;
using PetEmulator.Core;
using PetEmulator.Desktop.Infrastructure;
using PetEmulator.Desktop.Resources;
using PetEmulator.Vic20.Display;

namespace PetEmulator.Desktop.ViewModels.ChipTests;

public sealed class Mos6560DebugSession : ChipDebugSessionBase
{
    private readonly MOS6560 _chip;
    private readonly VicPreviewViewModel _preview;
    private readonly IAudioOutput _audioOutput;

    public Mos6560DebugSession()
        : this(CreateState()) { }

    private Mos6560DebugSession(VicState state)
        : base("MOS 6560 VIC", ChipDescriptions.Get("Mos6560"), state.Definition)
    {
        _chip = state.Chip;
        _preview = state.Preview;
        _audioOutput = AudioOutputFactory.CreateDefault();
        _audioOutput.Start(_chip);
        _preview.Refresh();
    }

    public override object? Visual => _preview;
    protected override int TimerCyclesPerTick => (int)Math.Round(MOS6560.Phi2Ntsc * 0.020);
    protected override void AfterRefresh() => _preview?.Refresh();
    public override void Dispose()
    {
        base.Dispose();
        _audioOutput.Dispose();
    }

    private static VicState CreateState()
    {
        var chip = new MOS6560("Chip Tester VIC");
        var memory = new FlatMemoryBus(ushort.MaxValue + 1);
        var preview = new VicPreviewViewModel(new Vic20RasterDisplay(memory, chip), memory);
        chip.Reset();
        chip.Write(2, 22);
        chip.Write(3, 46);
        chip.Write(5, 0x04);
        chip.Write(0x0E, 0x2F);
        chip.Write(0x0F, 0x1E);
        chip.Write(0x0A, 0xFF);
        chip.Write(0x0B, 0xFF);
        chip.Write(0x0C, 0xFF);
        chip.Write(0x0D, 0xFF);
        FillPattern(memory, chip);

        return new VicState(chip, new ChipDebugDefinition(
            () => chip.Tick(1), () => { chip.Reset(); FillPattern(memory, chip); },
            name => chip.Read(ParseRegister(name)),
            (name, value) => chip.Write(ParseRegister(name), value),
            Enumerable.Range(0, MOS6560.RegisterCount)
                .Select(index => (Name: $"R{index:X1}", Read: (Func<byte>)(() => chip.Read((ushort)index))))
                .ToArray(), []), preview);
    }

    private static void FillPattern(FlatMemoryBus memory, MOS6560 chip)
    {
        const ushort screen = 0x8000;
        const ushort characters = 0x9000;
        const ushort colors = 0x9400;
        const string message = "CHIP TESTER VIC IMAGE + SOUND";
        for (var index = 0; index < 256; index++)
            for (var row = 0; row < 8; row++)
                memory.Write((ushort)(characters + index * 8 + row), GlyphRow(index, row));

        for (var row = 0; row < chip.Rows; row++)
            for (var col = 0; col < chip.Columns; col++)
            {
                var position = row * chip.Columns + col;
                var code = position >= 2 && position - 2 < message.Length
                    ? ToVicScreenCode(message[position - 2])
                    : (byte)((row + col) % 32 + 1);
                memory.Write((ushort)(screen + position), code);
                memory.Write((ushort)(colors + position), (byte)((row + col) % 16));
            }
    }

    private static byte GlyphRow(int index, int row)
    {
        if (index == 0x20)
            return 0;
        var seed = (index * 37 + row * 19) & 0xFF;
        return (byte)(seed ^ (seed >> 3) ^ 0x18);
    }

    private static byte ToVicScreenCode(char value) => value is >= 'A' and <= 'Z'
        ? (byte)(value - 'A' + 1)
        : value == '+' ? (byte)0x2B : (byte)0x20;

    private static ushort ParseRegister(string name) => Convert.ToUInt16(name.TrimStart('R'), 16);

    private sealed record VicState(MOS6560 Chip, ChipDebugDefinition Definition, VicPreviewViewModel Preview);
}
